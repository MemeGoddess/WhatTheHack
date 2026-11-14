using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace WhatTheHack;

[PublicAPI]
public static class WhatTheHackUtility
{
    public static void ApplyHack(Pawn pawn, Pawn hacker = null)
    {
        if(!pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_TargetingHacked))
            pawn.health.AddHediff(WTH_DefOf.WTH_TargetingHacked);
        if(!pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_BackupBattery))
            pawn.health.AddHediff(WTH_DefOf.WTH_BackupBattery, pawn.TryGetReactor());
        
        if(pawn.Faction !=  Faction.OfPlayer)
            pawn.SetFaction(Faction.OfPlayer);
        pawn.relations ??= new Pawn_RelationsTracker(pawn);
        pawn.story ??= new Pawn_StoryTracker(pawn);
        pawn.ownership ??= new Pawn_Ownership(pawn);
        
        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        Utilities.InitWorkTypesAndSkills(pawn, pawnData);

        pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(pawn);
        
        LessonAutoActivator.TeachOpportunity(WTH_DefOf.WTH_Power, OpportunityType.Important);
        LessonAutoActivator.TeachOpportunity(WTH_DefOf.WTH_Maintenance, OpportunityType.Important);
        LessonAutoActivator.TeachOpportunity(WTH_DefOf.WTH_Concept_MechanoidParts, OpportunityType.Important);

        if (hacker == null)
            return;
        Find.LetterStack.ReceiveLetter("WTH_Letter_Success_Label".Translate(),
            "WTH_Letter_Success_Label_Description".Translate(hacker.Name.ToStringShort, pawn.Name.ToStringShort),
            LetterDefOf.PositiveEvent, pawn);
        
        if (!pawn.InBed())
            return;
        hacker.jobs.jobQueue
            .EnqueueFirst(new Job(WTH_DefOf.WTH_ClearHackingTable, pawn, pawn.CurrentBed())
            {
                count = 1
            });
    }

    public static void ApplyModule(Pawn pawn, RecipeDef installModuleRecipe, Pawn doctor = null)
    {
        var part = pawn.RaceProps.body.AllParts.First(part =>
            installModuleRecipe.appliedOnFixedBodyParts.Contains(part.def));
        var bill = HealthCardUtility.CreateSurgeryBill(pawn, installModuleRecipe, part);
        bill.Notify_BillWorkStarted(pawn);
        bill.Notify_IterationCompleted(pawn, []);
        bill.Notify_BillWorkFinished(pawn);
    }
}