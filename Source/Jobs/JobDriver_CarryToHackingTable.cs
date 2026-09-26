using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;

namespace WhatTheHack.Jobs
{
    internal class JobDriver_CarryToHackingTable : JobDriver
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

            // --- Step 1: Go to mech ---
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
                .FailOnDestroyedNullOrForbidden(TargetIndex.A)
                .FailOnDespawnedNullOrForbidden(TargetIndex.B)
                .FailOn(() => !pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly))
                .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

            // --- Step 2: Pick up the mech ---
            var carryMech = Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return carryMech;

            // --- Step 3: Go to table cell ---
            var gotoTableCell = new Toil
            {
                initAction = () =>
                {
                    IntVec3 tableCell = HackingTable.InteractionCell;
                    pawn.pather.StartPath(tableCell, PathEndMode.OnCell);
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
            yield return gotoTableCell;

            // --- Step 4: Place mech on table and start correct process ---
            var placeAndSetup = new Toil
            {
                initAction = () =>
                {
                    IntVec3 tableCell = HackingTable.Position;

                    // Drop pawn directly on table
                    if (pawn.carryTracker.CarriedThing != null)
                        pawn.carryTracker.TryDropCarriedThing(tableCell, ThingPlaceMode.Direct, out _);

                    pawn.ClearAllReservations();

                    // Detect which recipe is being worked
                    RecipeDef chosenRecipe = DetectActiveRecipe(Takee);

                    // Feed to table logic
                    HackingTable.TryAddPawnForModification(Takee, chosenRecipe);

                    if (Takee.Spawned)
                        Takee.Position = tableCell;

                    var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(Takee);
                    pawnData.isActive = false;
                    pawnData.canWorkNow = false;
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return placeAndSetup;
        }

        // --- Helper: Detect which recipe is associated with the carried mech ---
        private static RecipeDef DetectActiveRecipe(Pawn mech)
        {
            if (mech.BillStack?.Bills == null || mech.BillStack.Bills.Count == 0)
                return WTH_DefOf.WTH_HackMechanoid;

            // Check if any bill matches the known WhatTheHack recipes
            foreach (Bill bill in mech.BillStack.Bills)
            {
                RecipeDef recipe = bill.recipe;
                if (recipe == WTH_DefOf.WTH_HackMechanoid)
                    return WTH_DefOf.WTH_HackMechanoid;
                if (recipe == WTH_DefOf.WTH_ShutDownMechanoid)
                    return WTH_DefOf.WTH_ShutDownMechanoid;
                if (recipe == WTH_DefOf.WTH_InduceEmergencySignal)
                    return WTH_DefOf.WTH_InduceEmergencySignal;
            }

            // Default fallback
            return WTH_DefOf.WTH_HackMechanoid;
        }
    }
}
