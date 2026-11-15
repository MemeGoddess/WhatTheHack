using RimWorld;
using HarmonyLib;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using WhatTheHack;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore.MechRecharge
{

    [HarmonyPatch(typeof(Caravan_NeedsTracker), "TrySatisfyPawnNeeds")]
    [HarmonyPatchCategory(Expanded)]
    public class Caravan_Patch
    {
        public static void Postfix(Pawn pawn, ref Caravan_NeedsTracker __instance)
        {
            if (!pawn.IsHacked() || !pawn.ShouldRecharge()) return;
            List<Thing> batteries = __instance.caravan.AllThings.Where((Thing t) => t.def == WTHExCore_DefOf.WTHExCore_MechBatteryCharger).ToList();
            if (batteries.NullOrEmpty()) return;
            Thing batteryUsed = batteries.MinBy(x => x.stackCount);
            int batteryToUse = 1;
            if (batteryUsed.stackCount - batteryToUse <= 0) batteryUsed.Destroy();
            else batteryUsed.stackCount -= batteryToUse;
            MechanoidRecharge_Utility.BatteryCharge(pawn);
        }

    }
}
