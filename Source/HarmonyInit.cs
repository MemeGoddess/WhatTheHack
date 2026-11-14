using HarmonyLib;
using Verse;
using System.Reflection;



[StaticConstructorOnStartup]
internal static class HarmonyInit
{
	static HarmonyInit()
	{
		//Log.Message("[WhatTheHack] Patching.");
		new Harmony("zal.whatthehack").PatchAll(Assembly.GetExecutingAssembly());
	}
}
