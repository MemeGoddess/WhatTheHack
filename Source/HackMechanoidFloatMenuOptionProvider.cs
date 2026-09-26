using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;

namespace WhatTheHack
{
    public class HackMechanoidFloatMenuOptionProvider : FloatMenuOptionProvider
    {
        public override bool Drafted => false;
        public override bool Undrafted => true;
        public override bool Multiselect => false;
        public override bool CanSelfTarget => false;
        public override bool RequiresManipulation => true;
        public override bool MechanoidCanDo => false;

        public override bool TargetPawnValid(Pawn targetPawn, FloatMenuContext context)
        {
            if (!base.TargetPawnValid(targetPawn, context))
                return false;

            if (!targetPawn.IsMechanoid())
                return false;

            // must be on a hacking table
            if (!targetPawn.OnHackingTable())
                return false;

            var table = targetPawn.CurrentBed() as Building_HackingTable;
            return table != null && table.GetCurOccupant(Building_HackingTable.SLOTINDEX) == targetPawn;
        }

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
        {
            if (!TargetPawnValid(clickedPawn, context))
                yield break;

            Pawn actingPawn = context.FirstSelectedPawn;
            if (actingPawn == null)
                yield break;

            var table = clickedPawn.CurrentBed() as Building_HackingTable;
            if (table == null)
                yield break;

            // collect any current hacking-related bills
            var bills = clickedPawn.BillStack?.Bills
                        .Where(b =>
                            b.recipe == WTH_DefOf.WTH_HackMechanoid ||
                            b.recipe == WTH_DefOf.WTH_ShutDownMechanoid ||
                            b.recipe == WTH_DefOf.WTH_InduceEmergencySignal)
                        .ToList() ?? new List<Bill>();

            // --- HACK option: only show if not hacked ---
            if (!clickedPawn.IsHacked() && bills.All(b => b.recipe != WTH_DefOf.WTH_HackMechanoid))
            {
                yield return new FloatMenuOption("WTH_Menu_HackMechanoid".Translate(), () =>
                {
                    Job job = JobMaker.MakeJob(WTH_DefOf.WTH_HackMechanoidJob, clickedPawn, table);
                    actingPawn.jobs.TryTakeOrderedJob(job);
                }, MenuOptionPriority.Default);
            }

            // --- SHUTDOWN and EMERGENCY SIGNAL: always available if not already queued ---
            foreach (var recipe in new[] { WTH_DefOf.WTH_ShutDownMechanoid, WTH_DefOf.WTH_InduceEmergencySignal })
            {
                if (bills.All(b => b.recipe != recipe))
                {
                    yield return new FloatMenuOption(recipe.LabelCap, () =>
                    {
                        Job job = JobMaker.MakeJob(WTH_DefOf.WTH_HackMechanoidJob, clickedPawn, table);
                        job.bill = new Bill_Production(recipe);
                        actingPawn.jobs.TryTakeOrderedJob(job);
                    }, MenuOptionPriority.Default);
                }
            }

            // --- Existing bills: show resume options ---
            foreach (var bill in bills)
            {
                yield return new FloatMenuOption(bill.recipe.LabelCap, () =>
                {
                    Job job = JobMaker.MakeJob(WTH_DefOf.WTH_HackMechanoidJob, clickedPawn, table);
                    job.bill = bill;
                    actingPawn.jobs.TryTakeOrderedJob(job);
                }, MenuOptionPriority.Default);
            }
        }
    }
}
