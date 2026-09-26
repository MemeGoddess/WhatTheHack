using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;

namespace WhatTheHack
{
    public class JobDriver_HackMechanoid : JobDriver
    {
        private Pawn Mech => job.targetA.Thing as Pawn;
        private Building_HackingTable HackingTable => job.targetB.Thing as Building_HackingTable;
        private Bill Bill => job.bill;

        private const int RequiredPartCount = 10;
        private static ThingDef MechPartDef => ThingDef.Named("WTH_MechanoidParts");

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Mech == null || HackingTable == null)
                return false;

            // Reserve mech and table
            if (!pawn.Reserve(Mech, job, 1, -1, null, errorOnFailed) ||
                !pawn.Reserve(HackingTable, job, 1, -1, null, errorOnFailed))
                return false;

            // Find a stack with enough parts
            var partsStack = FindPartsStack(pawn, RequiredPartCount);
            if (partsStack == null)
            {
                if (errorOnFailed)
                    Messages.Message($"Not enough {MechPartDef.label} to hack mechanoid (need {RequiredPartCount}).", MessageTypeDefOf.RejectInput);
                return false;
            }

            if (!pawn.Reserve(partsStack, job, 1, RequiredPartCount, null, errorOnFailed))
                return false;

            job.SetTarget(TargetIndex.C, partsStack);
            job.count = RequiredPartCount;
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => Bill == null);

            // 1) Go pick up parts
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch)
                .FailOnDestroyedNullOrForbidden(TargetIndex.C)
                .FailOnSomeonePhysicallyInteracting(TargetIndex.C);

            yield return Toils_Haul.StartCarryThing(TargetIndex.C);

            // 2) Go to hacking table slot
            yield return Toils_Goto.GotoCell(HackingTable.InteractionCell, PathEndMode.OnCell);

            // 3) Drop parts at the table slot
            var dropAtTable = new Toil
            {
                initAction = () =>
                {
                    if (pawn.carryTracker.CarriedThing != null)
                    {
                        //var slot = HackingTable.GetSleepingSlotPos(Building_HackingTable.SLOTINDEX);
                        //pawn.carryTracker.TryDropCarriedThing(slot, ThingPlaceMode.Near, out _);
                        IntVec3 interactionCell = HackingTable.InteractionCell;
                        pawn.carryTracker.TryDropCarriedThing(interactionCell, ThingPlaceMode.Direct, out _);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return dropAtTable;

            // 4) Hack progress
            var hack = new Toil
            {
                initAction = () => pawn.pather.StopDead(),
                tickAction = () =>
                {
                    pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f);
                    if (pawn.IsHashIntervalTick(60))
                        FleckMaker.ThrowMetaIcon(Mech.Position, Mech.Map, FleckDefOf.HealingCross);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 2000
            };
            hack.WithProgressBar(TargetIndex.A, () => 1f - (hack.actor.jobs.curDriver.ticksLeftThisToil / 2000f));
            yield return hack;

            // 5) Consume parts and apply recipe
            var finish = new Toil
            {
                initAction = () =>
                {
                    try
                    {
                        IntVec3 interactionCell = HackingTable.InteractionCell;
                        //var slotCell = HackingTable.GetSleepingSlotPos(Building_HackingTable.SLOTINDEX);
                        var partsList = interactionCell.GetThingList(Map).Where(t => t.def == MechPartDef).ToList();
                        int totalAvailable = partsList.Sum(t => t.stackCount);

                        if (totalAvailable < RequiredPartCount)
                        {
                            Messages.Message($"Hacking failed — {RequiredPartCount} {MechPartDef.label} required at the table.",
                                MessageTypeDefOf.RejectInput);
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }

                        int remaining = RequiredPartCount;
                        foreach (var stack in partsList)
                        {
                            int take = Mathf.Min(remaining, stack.stackCount);
                            stack.SplitOff(take).Destroy(DestroyMode.Vanish);
                            remaining -= take;
                            if (remaining <= 0) break;
                        }

                        if (Bill?.recipe?.workerClass != null)
                        {
                            var recipeWorker = (RecipeWorker)System.Activator.CreateInstance(Bill.recipe.workerClass);
                            recipeWorker.recipe = Bill.recipe;
                            recipeWorker.ApplyOnPawn(Mech, null, pawn, new List<Thing>(), Bill);
                        }

                        Mech?.BillStack?.Delete(Bill);
                        Messages.Message($"Hacking complete: {Bill?.recipe?.label ?? "Unknown"}",
                            Mech, MessageTypeDefOf.PositiveEvent);
                    }
                    finally
                    {
                        CleanupTableAndPawn();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield return finish;
        }

        private Thing FindPartsStack(Pawn actor, int minCount)
        {
            var req = ThingRequest.ForDef(MechPartDef);
            return GenClosest.ClosestThingReachable(
                actor.Position,
                actor.Map,
                req,
                PathEndMode.ClosestTouch,
                TraverseParms.For(actor, Danger.Deadly, TraverseMode.ByPawn, false),
                maxDistance: 9999f,
                validator: t => t.stackCount >= minCount && !t.IsForbidden(actor) && actor.CanReserve(t)
            );
        }

        public new void EndJobWith(JobCondition condition)
        {
            base.EndJobWith(condition);
            CleanupTableAndPawn();
        }

        private void CleanupTableAndPawn()
        {
            try
            {
                //if (Mech != null && HackingTable != null && Mech.CurrentBed() == HackingTable)
                    //Mech.ownership?.UnclaimBed();

                //Mech?.BillStack?.Clear();
                pawn.ClearAllReservations();
            }
            catch (System.Exception e)
            {
                Log.Warning($"[WhatTheHack] Cleanup failed: {e}");
            }
        }
    }
}
