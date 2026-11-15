using RimWorld;
using HarmonyLib;
using Verse;
using System.Linq;
using System.Collections.Generic;
using WhatTheHack;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore
{
    [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestBillIngredients")]
    [HarmonyPatchCategory(Expanded)]
    class Bill_Patch
    {
        static bool Prefix(WorkGiver_DoBill __instance, Bill bill, Pawn pawn, ref List<ThingCount> chosen, ref bool __result)
        {
            if (bill.recipe == WTHExCore_RecipeDefOf.WTHExCore_Craft_VanometricModule_VFE_SV)
            {
                Thing thing = pawn.Map.spawnedThings.FirstOrDefault(
                    (Thing t) => (t.GetInnerIfMinified().def == DefDatabase<ThingDef>.GetNamed("VFE_SmallVanometricPowerCell")) &&
                    bill.IsFixedOrAllowedIngredient(t) &&
                    pawn.CanReach(t, Verse.AI.PathEndMode.Touch, Danger.Deadly) &&
                    !t.IsForbidden(pawn) &&
                    t is MinifiedThing);
                if (thing != null)
                {
                    ThingCountUtility.AddToList(chosen, thing, 1);
                    __result = true;
                    return false;
                }
            }
            else if (bill.recipe == WTHExCore_RecipeDefOf.WTHExCore_Craft_VanometricModule_VFE_LV)
            {
                Thing thing = pawn.Map.spawnedThings.FirstOrDefault(
                    (Thing t) => (t.GetInnerIfMinified().def == DefDatabase<ThingDef>.GetNamed("VFE_LargeVanometricPowerCell")) &&
                    bill.IsFixedOrAllowedIngredient(t) &&
                    pawn.CanReach(t, Verse.AI.PathEndMode.Touch, Danger.Deadly) &&
                    !t.IsForbidden(pawn) &&
                    t is MinifiedThing);
                if (thing != null)
                {
                    ThingCountUtility.AddToList(chosen, thing, 1);
                    __result = true;
                    return false;
                }
            }
            return true;
        }
    }

}
