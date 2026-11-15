using HarmonyLib;
using Verse;
using WhatTheHack.Needs;
using WhatTheHack;
using UnityEngine;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;
using static WhatTheHack.CompatibilityLoader;


namespace WTHExCore
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    [HarmonyPatchCategory(Expanded)]
    public class AddingGizmo_Patch
    {
        private static HediffDef EMP_Heddif;
        private static JobDef WTHExCore_Ability_EMPBurstModule;
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            List<Gizmo> gizmoList = __result.ToList();
            if (EMP_Heddif == null)
            {
                EMP_Heddif = DefDatabase<HediffDef>.GetNamed("WTHExCore_EMPBurstModule") ;
                if (EMP_Heddif == null) Log.Error("No HediffDef named WTHExCore_EMPBurstModule found to patch gizmo");
            }
            if (__instance.Faction != null && __instance.Faction.IsPlayer && __instance.IsHacked())
            {
                if (__instance.health.hediffSet.HasHediff(EMP_Heddif))
                {
                    gizmoList.Add(CreateGizmo_EMP(__instance));
                }

                gizmoList.Add(ConsumingPlasmoidFuelReserve(__instance));

            }
            __result = gizmoList;
        }

        private static Gizmo CreateGizmo_EMP(Pawn pawn)
        {
            var powerNeed = pawn.needs.energy;

            if (WTHExCore_Ability_EMPBurstModule == null)
            {
                WTHExCore_Ability_EMPBurstModule = DefDatabase<JobDef>.GetNamed("WTHExCore_Ability_EMPBurstModule");
                if (WTHExCore_Ability_EMPBurstModule == null)
                    Log.Error("No JobDef named WTHExCore_Ability_EMPModule found to patch gizmo");
            }
            JobDef jobDef = WTHExCore_Ability_EMPBurstModule;
            DefModExtension_Ability modExt = jobDef.GetModExtension<DefModExtension_Ability>();

            bool needsMorePower = powerNeed.CurLevel < modExt.powerDrain;
            bool notActivated = !pawn.IsActivated();
            bool isDisabled = needsMorePower || notActivated;
            string disabledReason = "";
            if (isDisabled)
            {
                if (notActivated)
                {
                    disabledReason = "WTH_Reason_NotActivated".Translate();
                }
                else if (needsMorePower)
                {
                    disabledReason = "WTH_Reason_NeedsMorePower".Translate(new object[] { modExt.powerDrain });
                }
            }

            Gizmo gizmo = new Command_Action
            {
                defaultLabel = "WTHExCore_EMPBurst_Label".Translate(),
                defaultDesc = "WTHExCore_EMPBurst_Description".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Gizmo/" + "emp_burst_gizmo"), true),
                Disabled = isDisabled,
                disabledReason = disabledReason,
                action = delegate
                {
                    Job job = new Job(jobDef, pawn);
                    pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                }
            };
            return gizmo;
        }

        private static Gizmo ConsumingPlasmoidFuelReserve(Pawn pawn)
        {
            Gizmo gizmo = new Command_Action
            {
                defaultLabel = "WTHExCore_ConsumingPFR_Label".Translate(),
                defaultDesc = "WTHExCore_ConsumingPFR_Description".Translate(),
                icon = ContentFinder<Texture2D>.Get(("UI/Gizmo/" + "consume_battery_gizmo"), true),
                action = delegate
                {
                    Job job = null;
                    Thing battery = JobGiver_MechanoidRecharge.GetAvailableMechBattery(pawn);
                    if (battery != null && pawn.CanReserve(battery))
                    {
                        if (pawn.CurJob != null)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                        job = new Job(DefDatabase<JobDef>.GetNamed("WTHExCore_MechBatteryCharger"), battery);
                    }
                    pawn.jobs.StartJob(job);
                }
            };
            return gizmo;
        }
    }
}
