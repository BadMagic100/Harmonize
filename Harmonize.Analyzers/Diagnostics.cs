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

    public static readonly DiagnosticDescriptor AmbiguousData = new(
        id: DiagnosticPrefix + "002",
        title: "Ambiguous data found for patch",
        messageFormat: "Merging the same data fields from multiple attributes has undefined behavior",
        category: "Harmonize.Targeting",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MissingClassPatch = new(
        id: DiagnosticPrefix + "003",
        title: "Missing class patch",
        messageFormat: "Methods annotated with HarmonyPatch should belong to a class annotated with HarmonyPatch",
        category: "Harmonize.Targeting",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor UnspecifiedPatchType = new(
        id: DiagnosticPrefix + "004",
        title: "Undefined patch type",
        messageFormat: "Patch type is undefined or ambiguous and cannot be determined",
        category: "Harmonize.Targeting",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
