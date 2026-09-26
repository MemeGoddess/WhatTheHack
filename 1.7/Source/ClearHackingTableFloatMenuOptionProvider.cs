using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;

namespace WhatTheHack
{
    public class ClearHackingTableFloatMenuOptionProvider : FloatMenuOptionProvider
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

            var table = targetPawn.CurrentBed() as Building_HackingTable;
            return table != null && table.GetCurOccupant(Building_HackingTable.SLOTINDEX) == targetPawn;
        }

        public override FloatMenuOption GetSingleOptionFor(Pawn targetPawn, FloatMenuContext context)
        {
            Pawn actingPawn = context.FirstSelectedPawn;
            var table = targetPawn.CurrentBed() as Building_HackingTable;

            if (actingPawn == null || targetPawn == null || table == null)
                return null;

            return new FloatMenuOption("WTH_Menu_ClearTable".Translate(), () =>
            {
                Job job = JobMaker.MakeJob(WTH_DefOf.WTH_ClearHackingTable, targetPawn, table);
                job.count = 1; // ensure valid count
                actingPawn.jobs.TryTakeOrderedJob(job);
            }, MenuOptionPriority.Low);
        }
    }
}
