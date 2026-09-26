using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using WhatTheHack.Buildings;

namespace WhatTheHack.Harmony
{
    [HarmonyPatch(typeof(RestUtility), "CurrentBed", typeof(Pawn))]
    internal static class RestUtility_CurrentBed
    {
        private static bool Prefix(Pawn p, ref Building_Bed __result)
        {

            if (p == null || !p.Spawned || p.Map == null)
                return true;

            // Only handle hacked pawns resting with WTH_Mechanoid_Rest
            if (p.jobs?.curJob?.def != WTH_DefOf.WTH_Mechanoid_Rest || !p.IsHacked())
                return true;

            var things = p.Position.GetThingList(p.Map);

            // Check hacking tables first
            var hackingTable = things.OfType<Building_HackingTable>()
                .FirstOrDefault(ht => p.Position == ht.GetSleepingSlotPos(Building_HackingTable.SLOTINDEX));
            if (hackingTable != null)
            {
                __result = hackingTable;
                return false;
            }

            // Check mechanoid platforms
            var platform = things.OfType<Building_BaseMechanoidPlatform>()
                .FirstOrDefault(pl => p.Position == pl.GetSleepingSlotPos(Building_BaseMechanoidPlatform.SLOTINDEX));
            if (platform != null)
            {
                __result = platform;
                return false;
            }

            return true; // fallback to original logic
        }
    }
}
