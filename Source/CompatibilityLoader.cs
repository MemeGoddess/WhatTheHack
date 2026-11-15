using RimWorld;
using Verse;

namespace WhatTheHack;

public static class CompatibilityLoader
{
    public const string Expanded = "memegoddess.wthexp";
    public static bool ExpandedInstalled = ModLister.AnyModActiveNoSuffix([Expanded]);

    public const string AdditionalModules = "memegoddess.wthexpansion";
    public static bool AdditionalModulesInstalled = ModLister.AnyModActiveNoSuffix([AdditionalModules]);
    
}