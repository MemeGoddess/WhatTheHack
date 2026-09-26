using HarmonyLib;
using RimWorld;
using Verse;

namespace WhatTheHack.Harmony;

//[HarmonyPatch(typeof(Need_MechEnergy), "NeedInterval")]
//internal static class Need_MechEnergy_NeedInterval
//{
//   private static bool Prefix(Need_MechEnergy __instance)
//   {
//       return !__instance.pawn.IsHacked();
//   }
//}

[HarmonyPatch]
internal static class Need_MechEnergy_NeedInterval
{
    private static System.Reflection.MethodBase TargetMethod()
    {
        // Use reflection to find the Biotech class safely
        var type = AccessTools.TypeByName("RimWorld.Need_MechEnergy");
        return AccessTools.Method(type, "NeedInterval");
    }

    private static bool Prefix(object __instance)
    {
        // Get the pawn field dynamically
        var pawnField = AccessTools.Field(__instance.GetType(), "pawn");
        Pawn pawn = pawnField?.GetValue(__instance) as Pawn;

        if (pawn == null)
            return true; // If pawn is missing, run vanilla behavior

        // Skip the drain logic if:
        // 1. Pawn is hacked (player controlled via WTH)
        // 2. Pawn belongs to a non-player faction
        if (pawn.IsHacked() || (pawn.Faction != null && pawn.Faction != Faction.OfPlayer))
        {
            return false; // skip original method
        }

        return true; // allow vanilla behavior otherwise
    }
}