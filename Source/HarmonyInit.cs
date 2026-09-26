using HarmonyLib;
using Verse;



[StaticConstructorOnStartup]
internal static class HarmonyInit
{
	static HarmonyInit()
	{
		//Log.Message("[WhatTheHack] Patching.");
		new Harmony("zal.whatthehack").PatchAll();
	}
}
