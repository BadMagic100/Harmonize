using Microsoft.CodeAnalysis;

namespace Harmonize;

public static class Diagnostics
{
    public const string DiagnosticPrefix = "HARMONIZE";

    public static readonly DiagnosticDescriptor AmbiguousTarget = new(
        id: DiagnosticPrefix + "001",
        title: "Ambiguous target found for patch",
        messageFormat: "A single unambiguous target could not be resolved for '{0}'",
        category: "Harmonize.Targeting",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
