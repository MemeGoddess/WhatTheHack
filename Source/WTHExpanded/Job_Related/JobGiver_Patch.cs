using RimWorld;
using HarmonyLib;
using Verse;
using System;
using WhatTheHack;
using System.Collections.Generic;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore
{

    [HarmonyPatch(typeof(JobGiver_Work), "PawnCanUseWorkGiver")]
    [HarmonyPatchCategory(nameof(Expanded))]
    public class JobGiver_Patch
    {
        private static List<WorkGiverDef> docGiverDefs;
        private static List<WorkGiverDef> constructionGiverDefs;
        private static List<WorkGiverDef> rescueGiverDefs;
        private static List<WorkGiverDef> husbandryGiverDefs;
        private static List<WorkGiverDef> gastroGiverDefs;

        [HarmonyPriority(200)]
        public static void Postfix(ref bool __result, Pawn pawn, WorkGiver giver)
        {
            if (__result && pawn.IsHacked())
            {
                if (giver.def.workType == WorkTypeDefOf.Construction)
                {
                    if (constructionGiverDefs == null)
                    {
                        constructionGiverDefs = new List<WorkGiverDef>();
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("FixBrokenDownBuilding"));
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Repair"));
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("BuildRoofs"));
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("RemoveRoofs"));
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("ConstructDeliverResourcesToFrames"));
                        constructionGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("ConstructDeliverResourcesToBlueprints"));
                    }
                    if (!constructionGiverDefs.Contains(giver.def)) __result = false;
                    return;
                }
                if (giver.def.workType == WorkTypeDefOf.Doctor)
                {
                    if(docGiverDefs == null)
                    {
                        docGiverDefs = new List<WorkGiverDef>();
                        docGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("DoctorTendEmergency"));
                        docGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("DoctorTendToHumanlikes"));
                        docGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("DoctorTendEmergency"));
                        docGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("DoctorTendToSelfEmergency"));
                    }
                    if(!docGiverDefs.Contains(giver.def)) __result = false;
                }
                /* Rescue */
                if (giver.def.workType == WTHExCore_WorkDefOf.WTHExCore_Rescue)
                {
                    if (rescueGiverDefs == null)
                    {
                        rescueGiverDefs = new List<WorkGiverDef>();
                        rescueGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Haul_Humanlike"));
                        rescueGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Haul_Animal"));
                    }
                    if (!rescueGiverDefs.Contains(giver.def)) __result = false;
                }
                /* Husbandry */
                if (giver.def.workType == WTHExCore_WorkDefOf.WTHExCore_Husbandry)
                {
                    if (husbandryGiverDefs == null)
                    {
                        husbandryGiverDefs = new List<WorkGiverDef>();
                        husbandryGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Husbandry_Milk"));
                        husbandryGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Husbandry_Shear"));
                    }
                    if (!husbandryGiverDefs.Contains(giver.def)) __result = false;
                }
                /* Gastro */
                if (giver.def.workType == WTHExCore_WorkDefOf.WTHExCore_Gastro)
                {
                    if (gastroGiverDefs == null)
                    {
                        gastroGiverDefs = new List<WorkGiverDef>();
                        gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_DoBillsBrew"));
                        gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_FillFermentingBarrel"));
                        if (DefDatabase<WorkTypeDef>.GetNamedSilentFail("Gastro_DoBillsLargeStoveCook") != null)
                        {
                            gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_DoBillsLargeStoveCook"));
                        }
                        if (DefDatabase<WorkTypeDef>.GetNamedSilentFail("Gastro_VCE_DoBillsPrep") != null)
                        {
                            gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_VCE_DoBillsPrep"));
                        }
                        if (DefDatabase<WorkTypeDef>.GetNamedSilentFail("Gastro_VCE_DoBillsSushi") != null)
                        {
                            gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_VCE_DoBillsSushi"));
                        }
                        gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_CookFillHopper"));
                        gastroGiverDefs.Add(DefDatabase<WorkGiverDef>.GetNamed("Gastro_DoBillsCook"));
                    }
                    if (!gastroGiverDefs.Contains(giver.def)) __result = false;
                }
            }
        }
    }
}
