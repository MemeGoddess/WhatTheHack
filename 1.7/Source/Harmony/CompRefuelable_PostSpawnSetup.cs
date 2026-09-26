using HarmonyLib;
using RimWorld;
using Verse;

namespace WhatTheHack.Harmony
{
    // spawns mechanoids fully fueled
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.PostSpawnSetup))]
    public static class WTH_CompRefuelable_PostSpawnSetup_Patch
    {
        public static void Postfix(CompRefuelable __instance)
        {
            if (__instance.parent is Pawn p && p.RaceProps.IsMechanoid)
            {
                // Force full fuel once at spawn
                //Log.Message($"Filling fuel for {p}");
                __instance.Refuel(__instance.Props.fuelCapacity);
            }
        }
    }

}
