using HarmonyLib;
using RimWorld;
using Verse;

namespace WhatTheHack.Harmony
{
    [HarmonyPatch]
    internal static class Need_Power_NeedInterval
    {
        private static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("WhatTheHack.Needs.Need_Power");
            return AccessTools.Method(type, "NeedInterval");
        }

        private static bool Prefix(object __instance)
        {
            // Access pawn field dynamically since type may not be directly referenceable
            var pawnField = AccessTools.Field(__instance.GetType(), "pawn");
            Pawn pawn = pawnField?.GetValue(__instance) as Pawn;

            if (pawn == null)
                return true;

            // Skip the drain logic if:            
            // 1. Pawn belongs to a non-player faction
            if ((pawn.Faction != null && pawn.Faction != Faction.OfPlayer))
            {
                return false; // skip original method
            }

            return true; // allow vanilla behavior otherwise
        }
    }
}
