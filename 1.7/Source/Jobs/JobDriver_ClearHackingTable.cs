using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;

namespace WhatTheHack.Jobs
{
    internal class JobDriver_ClearHackingTable : JobDriver
    {
        protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;
        protected Building_HackingTable HackingTable => (Building_HackingTable)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(HackingTable, job, 1, -1, null, errorOnFailed);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalState(TargetIndex.A);

            // --- Step 1: Go to mech on table ---
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !pawn.CanReach(Takee, PathEndMode.Touch, Danger.Deadly))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            // --- Step 2: Interrupt job and clear bills ---
            var interruptJob = new Toil
            {
                initAction = () =>
                {
                    Takee.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                    Takee.health?.surgeryBills?.Clear();
                    HackingTable.TryRemovePawnForModification(Takee);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return interruptJob;

            // --- Step 3: Pick up mech ---
            var carryMech = Toils_Haul.StartCarryThing(TargetIndex.A);
            carryMech.initAction = () => job.count = 1; // ✅ Fix invalid count
            yield return carryMech;

            // --- Step 4: Find safe drop cell ---
            var findDropCell = new Toil
            {
                initAction = () =>
                {
                    IntVec3 dropCell;
                    if (!CellFinder.TryFindRandomReachableNearbyCell(
                            HackingTable.Position,
                            HackingTable.Map,
                            3f,
                            TraverseParms.For(pawn),
                            null,
                            null,
                            out dropCell))
                    {
                        dropCell = HackingTable.Position;
                    }

                    job.SetTarget(TargetIndex.C, dropCell);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return findDropCell;

            // --- Step 5: Go to drop cell ---
            yield return Toils_Goto.GotoCell(TargetIndex.C, PathEndMode.OnCell);

            // --- Step 6: Drop mech and cleanup ---
            var dropMech = new Toil
            {
                initAction = () =>
                {
                    if (pawn.carryTracker.CarriedThing != null)
                        pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Direct, out _);

                    pawn.ClearAllReservations();

                    var data = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(Takee);
                    data.isActive = true;
                    data.canWorkNow = true;
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return dropMech;
        }
    }
}
