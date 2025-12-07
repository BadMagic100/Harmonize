# Harmonize

[![NuGet Version](https://img.shields.io/nuget/v/Harmonize)](https://www.nuget.org/packages/Harmonize)
[![GitHub License](https://img.shields.io/github/license/BadMagic100/Harmonize?logo=github)](https://github.com/BadMagic100/Harmonize)


Harmonize is a suite of design-time and compile-time tooling to simplify working with
[Harmony](https://github.com/pardeike/Harmony) and [HarmonyX](https://github.com/BepInEx/HarmonyX). It provides
IDE autocompletion for common [parameter injections](https://harmony.pardeike.net/articles/patching-injections.html).
It also analyzes your code for common usage mistakes to prevent errors at runtime and provides quick fixes to
remedy them where possible.

To get started, simply add to your csproj:

```xml
<PackageReference Include="Harmonize" Version="1.0.1" />
