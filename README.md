# Harmonize

[![NuGet Version](https://img.shields.io/nuget/v/Harmonize)](https://www.nuget.org/packages/Harmonize)
[![GitHub License](https://img.shields.io/github/license/BadMagic100/Harmonize?logo=github)](https://github.com/BadMagic100/Harmonize)


Harmonize is a suite of design-time and compile-time tooling to simplify working with
[Harmony](https://github.com/pardeike/Harmony) and [HarmonyX](https://github.com/BepInEx/HarmonyX). It provides
IDE autocompletion for common [parameter injections](https://harmony.pardeike.net/articles/patching-injections.html).
It also analyzes your code for common usage mistakes to prevent errors at runtime and provides quick fixes to
remedy them where possible.

## Installing

### Prerequisites

Harmonize requires the .NET 10 SDK. This means that, depending how you build your code, you will need one or
more of the following to use Harmonize:

- Visual Studio - 2026 (18.0) or later
- JetBrains Rider - 2025.3 or later
- Visual Studio Code or `dotnet` CLI - .NET SDK 10.0.100 or later

### Package installation

Any of the methods outlined on the NuGet page should be sufficient in theory. In practice, it seems that adding
the PackageReference directly to your csproj is the most bulletproof as `dotnet add package` will sometimes give
a "the package does not contain any assembly references or content files that are compabible" error for reasons
that are not very clear to me (if you know, feel free to tell share).

## Features

Pending better documentation, here is a semi-comprehensive list of features currently supported by Harmonize.

### Analyzers

Harmonize provides the following diagnostics, most of which also come with quick fixes to remedy them:

- `HARMONIZE001` - Errors when a patch method is underspecified (for example, does not define arguments for an
  overloaded method). This will produce an error at runtime.
- `HARMONIZE002` - Warns when any piece of data from patch attributes is defined multiple times in the same scope.
  This is undefined behavior, even if the attributes appear ordered. HarmonyX has one edge case where this warning
  can be ignored, which you can read about [here](https://github.com/BepInEx/HarmonyX/wiki/Multitargeted-patches).
- `HARMONIZE003` - Warns when a patch class is not annotated with `[HarmonyPatch]`. This will cause PatchAll to
  silently ignore the patch.
- `HARMONIZE004` - Warns when the type (Prefix, Postfix, Transpiler) of a patch method cannot be determined. If
  the method is not named or annotated appropriately, this will cause PatchAll to silently ignore the patch. If
  there are multiple patch types among the name/annotations this is undefined behavior. Static helper methods
  called from within the patch class are exempted from this rule.

### Completions

> [!WARNING]
> Completions are not supported in Rider due to limitations of the JetBrains platform.

Harmonize provides the following completion providers:

- Injections - Fully-typed injections for patch parameters when typing in a parameter list of an
  unambiguously-specified patch method. The simplest way to access them is by starting with `_`. The following
  injections are currently supported
  - `__instance` for instance methods
  - parameters of the patched method
