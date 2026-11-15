using RimWorld;
using Verse;
using System.Collections.Generic;
using Verse.AI;
using WTHExCore.MechRecharge;

namespace WTHExCore
{
    public class JobDriver_MechanoidRecharge : JobDriver
    {

        protected Thing Battery
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.HasReserved(Battery))
            {
                return true;
            }
            bool result = pawn.Reserve(Battery, job, 1);
            return result;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            //pawn.Reserve(Battery, job, 1);
            yield return ReserveBattery();
            Toil gotoToPickup = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Jump.JumpIf(gotoToPickup, () => pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);
            Toil chew = Toils_Ingest.ChewIngestible(pawn, 1, TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Jump.Jump(chew);
            yield return gotoToPickup;
            yield return Toils_Ingest.PickupIngestible(TargetIndex.A, pawn);
            yield return chew;
            Toil finalize = new Toil();
            finalize.initAction = delegate
            {
                MechanoidRecharge_Utility.BatteryCharge(pawn);
                //ingester.records.AddTo(RecordDefOf.NutritionEaten, num2);
            };
            finalize.AddFinishAction(delegate
            {
                if (Battery.stackCount - job.count <= 0) Battery.Destroy();
                else Battery.stackCount -= job.count;
                //ingester.records.AddTo(RecordDefOf.NutritionEaten, num2);
            });
            finalize.defaultCompleteMode = ToilCompleteMode.Instant;
			yield return finalize;
        }


        private Toil ReserveBattery()
        {
            return new Toil
            {
                initAction = delegate
                {
                    if (pawn.Faction != null)
                    {
                        Thing thing = job.GetTarget(TargetIndex.A).Thing;
                        if (pawn.carryTracker.CarriedThing != thing)
                        {
                            int maxAmountToPickup = 1;
                            if (maxAmountToPickup != 0)
                            {
                                if (!pawn.Reserve(thing, job, 10, maxAmountToPickup))
                                {
                                    Log.Error(string.Concat("Pawn Battery reservation for ", pawn, " on job ", this, " failed, because it could not register Battery from ", thing, " - amount: ", maxAmountToPickup));
                                    pawn.jobs.EndCurrentJob(JobCondition.Errored);
                                }
                                job.count = maxAmountToPickup;
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                atomicWithPrevious = true
            };
        }

    }
}
