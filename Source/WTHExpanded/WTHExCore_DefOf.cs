using RimWorld;
using Verse;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore
{
    [DefOf]
    public class WTHExCore_DefOf
    {
        [MayRequire(Expanded)]
        public static ThingDef WTHExCore_MechBatteryCharger;
        [MayRequire(Expanded)]
        public static ThingDef WTHExCore_PortableHackingTable;
    }

    public class WTHExCore_RecipeDefOf
    {
        [MayRequire(Expanded)]
        public static RecipeDef WTHExCore_Craft_VanometricModule_VFE_SV;
        [MayRequire(Expanded)]
        public static RecipeDef WTHExCore_Craft_VanometricModule_VFE_LV;
    }   

    public class WTHExCore_WorkDefOf
    {
        [MayRequire(Expanded)]
        public static WorkTypeDef WTHExCore_Rescue = DefDatabase<WorkTypeDef>.GetNamed("WTHExCore_Rescue");
        [MayRequire(Expanded)]
        public static WorkTypeDef WTHExCore_Husbandry = DefDatabase<WorkTypeDef>.GetNamed("WTHExCore_Husbandry");
        [MayRequire(Expanded)]
        public static WorkTypeDef WTHExCore_Gastro = DefDatabase<WorkTypeDef>.GetNamed("WTHExCore_Gastro");
    }
}
