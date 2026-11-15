using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack;
using WhatTheHack.Storage;

namespace WTHExCore
{
    public class JobGiver_MechanoidRecharge : ThinkNode_JobGiver
    {
        private static string jobDefName = "WTHExCore_MechBatteryCharger";
        private static string thingDefName = "WTHExCore_MechBatteryCharger";

        public override Job TryGiveJob(Pawn pawn)
        {
            Job job = null;
            if (pawn.IsActivated() && pawn.ShouldRecharge())
            {
                if (!pawn.OnBaseMechanoidPlatform() && !pawn.Downed)
                {
                    Thing battery = GetAvailableMechBattery(pawn);
                    if (battery != null && pawn.CanReserve(battery))
                    {
                        if (pawn.CurJob != null)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                        job = new Job(DefDatabase<JobDef>.GetNamed(jobDefName), battery);
                    }
                }
                else
                {
                    pawn.drafter.Drafted = false;
                    ExtendedPawnData pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
                    pawnData.isActive = false;
                }
            }
            return job;
        }

        public static Thing GetAvailableMechBattery(Pawn pawn)
        {
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(WTHExCore_DefOf.WTHExCore_MechBatteryCharger), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.None, TraverseMode.ByPawn, false), 9999f, delegate (Thing b) {
                if (!b.IsBurning() &&
                !b.IsForbidden(pawn) &&
                pawn.CanReserve(b))
                {
                    return true;
                }
                return false;
            });
        }
    }
}
