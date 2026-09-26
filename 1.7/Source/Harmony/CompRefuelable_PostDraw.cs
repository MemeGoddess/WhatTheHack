using HarmonyLib;
using RimWorld;
using Verse;

namespace WhatTheHack.Harmony
{
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.PostDraw))]
    public static class Patch_CompRefuelable_PostDraw
    {
        static bool Prefix(CompRefuelable __instance)
        {
            // Check if the parent is a Mechanoid and is dead
            if (__instance.parent is Pawn pawn && pawn.RaceProps.IsMechanoid && pawn.Dead)
            {
                return false; // Skip the PostDraw method
            }

            return true; // Allow PostDraw to run as normal
        }
    }
}
