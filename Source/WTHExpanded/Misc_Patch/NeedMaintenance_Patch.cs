using HarmonyLib;
using Verse;
using WhatTheHack;
using WhatTheHack.Needs;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore
{
    // TODO merge this patch
    [HarmonyPatch(typeof(Need_Maintenance), "FallPerTick")]
    [HarmonyPatchCategory(Expanded)]
    public class NeedMaintenance_Patch
    {
        private static HediffDef maintenanceModuleHediff;
        private static HediffDef MaintenanceModuleHediff
        {
            get
            {
                if (maintenanceModuleHediff != null) return maintenanceModuleHediff;
                maintenanceModuleHediff = DefDatabase<HediffDef>.GetNamed("WTHExCore_Craft_SelfRetainmentMechanitePackage");
                if (maintenanceModuleHediff == null)
                    Log.Error("No HediffDef named WTHExCore_CraftSelfRetainmentMechanitePackage found");
                return maintenanceModuleHediff;
            }
        }
        public static void Postfix(Need_Maintenance __instance, Pawn ___pawn, ref float __result)
        {            
            if (___pawn.health.hediffSet.HasHediff(MaintenanceModuleHediff))
                __result -= __instance.MaxLevel;
        }
    }
}
