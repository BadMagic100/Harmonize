using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Harmonize.Test.Helper;

internal static class RoslynHelpers
{
    public static async Task<Document> CreateDocumentAsync(
        string code,
        MefHostServices host,
        CancellationToken cancellationToken
    )
    {
        ReferenceAssemblies refs = ReferenceAssemblies.Default.AddPackages([
            new PackageIdentity("Lib.Harmony", "2.4.2"),
        ]);
        CSharpCompilationOptions options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary
        );

        Project project = new AdhocWorkspace(host)
            .AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(options)
            .AddMetadataReferences(
                await refs.ResolveAsync(LanguageNames.CSharp, cancellationToken)
            );

        Document doc = project.AddDocument("TestDocument", code);
        return doc;
    }

    public static async Task<IMethodSymbol> GetMethodSymbolFromSourceAsync(
        string code,
        CancellationToken cancellationToken
    )
    {
        TestFileMarkupParser.GetSpan(code, out string finalCode, out TextSpan span);

        Document doc = await CreateDocumentAsync(
            finalCode,
            MefHostServices.DefaultHost,
            cancellationToken
        );

        SyntaxNode? root = await doc.GetSyntaxRootAsync(cancellationToken);
        SemanticModel? model = await doc.GetSemanticModelAsync(cancellationToken);

        Assert.NotNull(root);
        Assert.NotNull(model);

        MethodDeclarationSyntax? syntax = root.FindNode(span)
            .FirstAncestorOrSelf<MethodDeclarationSyntax>();
        Assert.NotNull(syntax);

        IMethodSymbol? symbol = model.GetDeclaredSymbol(syntax, cancellationToken);
        Assert.NotNull(symbol);

        return symbol;
    }
}
