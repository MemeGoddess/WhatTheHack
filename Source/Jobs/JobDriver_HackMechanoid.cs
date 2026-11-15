using System;
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
        private Building_HackingTable HackingTable => job.targetC.Thing as Building_HackingTable;
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
            var partList = new List<ThingCount>();
            WorkGiver_DoBill.TryFindBestFixedIngredients(job.RecipeDef.ingredients, GetActor(), TargetThingC, partList);
            if (!partList.Any() || partList.Sum(x => x.Count) < RequiredPartCount)
            {
                if (errorOnFailed)
                    Messages.Message($"Not enough {MechPartDef.label} to hack mechanoid (need {RequiredPartCount}).", MessageTypeDefOf.RejectInput);
                return false;
            }

            var partsLeft = RequiredPartCount;
            foreach (var part in partList)
            {
                job.AddQueuedTarget(TargetIndex.B, part.Thing);
                
                var countAdd = Math.Min(part.Count, partsLeft);
                partsLeft -= countAdd;
                job.countQueue ??= [];
                job.countQueue.Add(countAdd);
            }
            
            job.count = RequiredPartCount;
            pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.B), job);
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.C);
            this.FailOn(() => Bill == null);

            // 1) Go pick up parts
            foreach (var item in JobDriver_DoBill.CollectIngredientsToils(TargetIndex.B, TargetIndex.C, TargetIndex.C,
                         subtractNumTakenFromJobCount: false, failIfStackCountLessThanJobCount: true))
                yield return item;

            yield return Toils_Haul.StartCarryThing(TargetIndex.C);

            // 2) Go to hacking table slot
            yield return Toils_Goto.GotoCell(HackingTable.Position, PathEndMode.OnCell);

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

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!TargetB.HasThing || TargetB.Thing is not Building_HackingTable)
                    return;
                if (!TargetC.HasThing || TargetC.Thing.def != MechPartDef)
                    return;
            
                Log.Warning($"[What The Hack] Running Job migration for Job_{job.loadID}");
                var bench = TargetB.Thing as Building_HackingTable;
                var stack = TargetC.Thing;

                job.targetB = null;
                job.targetC = null;
                job.AddQueuedTarget(TargetIndex.B, stack);
                job.SetTarget(TargetIndex.C, bench);
            }
            base.ExposeData();
        }
    }
}
