using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using WhatTheHack.Buildings;
using WhatTheHack.Needs;
using WhatTheHack.Storage;

namespace WhatTheHack;

public static class Extensions
{
    public static void RemoveRemoteControlLink(this Pawn pawn)
    {
        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        var remoteControlLink = pawnData.remoteControlLink;
        if (remoteControlLink != null)
        {
            var linkPawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(remoteControlLink);
            linkPawnData.remoteControlLink = null;
        }

        pawnData.remoteControlLink = null;
    }

    public static bool IsMechanoid(this PawnKindDef kindDef)
    {
        return kindDef.RaceProps.IsMechanoidLike();
    }

    public static bool IsMechanoid(this Pawn pawn)
    {
        return pawn.RaceProps.IsMechanoidLike();
    }

    public static bool IsMechanoidLike(this RaceProperties RaceProps)
    {
        return RaceProps.IsMechanoid;
    }

    public static bool IsHacked(this Pawn pawn)
    {
        if (!pawn.IsMechanoid())
        {
            return false;
        }

        if (pawn.health != null && (pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_TargetingHacked) ||
                                    pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_TargetingHackedPoorly)))
        {
            return true;
        }

        if (pawn.health != null && pawn.ControllingAI() != null)
        {
            return true;
        }

        {
            return false;
        }
    }

    public static void RevertToFaction(this Pawn pawn, Faction faction)
    {
        pawn.SetFaction(faction);
        pawn.story = null;
        if (pawn.GetLord() == null || pawn.GetLord().LordJob == null)
        {
            LordMaker.MakeNewLord(Faction.OfMechanoids, new LordJob_AssaultColony(Faction.OfMechanoids), pawn.Map,
                new List<Pawn> { pawn });
        }
    }

    public static bool UnableToControl(this Pawn pawn)
    {
        return pawn.DestroyedOrNull() || pawn.Downed || pawn.InMentalState || pawn.IsBurning() ||
               pawn.RemoteControlLink() == null || pawn.apparel == null ||
               pawn.apparel.WornApparel.FirstOrDefault(app => app.def == WTH_DefOf.WTH_Apparel_MechControllerBelt) ==
               null;
    }

    public static bool HasReplacedAI(this Pawn pawn)
    {
        return pawn.health != null && pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_ReplacedAI);
    }

    public static Building_RogueAI ControllingAI(this Pawn pawn)
    {
        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null)
        {
            return null;
        }

        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        //if (pawnData.controllingAI.Spawned)
        //{
        return pawnData.controllingAI;
        //}
    }

    public static Pawn RemoteControlLink(this Pawn pawn)
    {
        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null)
        {
            return null;
        }

        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        return pawnData.remoteControlLink;
    }

    public static bool HasMechAbility(this Pawn pawn)
    {
        return pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.def.GetModExtension<DefModextension_Hediff>() is
            { hasAbility: true }) != null;
    }


    public static BodyPartRecord TryGetReactor(this Pawn pawn)
    {
        BodyPartRecord result = null;
        if (pawn.health != null)
        {
            result = pawn.health.hediffSet.GetNotMissingParts().FirstOrDefault(bpr => bpr.def.defName == "Reactor");
        }

        return result;
    }

    public static void RemoveAllLinks(this Pawn pawn)
    {
        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null)
        {
            return;
        }

        var pawnData = store.GetExtendedDataFor(pawn);
        pawnData.isActive = false;
        pawn.RemoveRemoteControlLink();
        if (pawn.ControllingAI() == null)
        {
            return;
        }

        pawn.ControllingAI().controlledMechs.Remove(pawn);
        pawn.ControllingAI().hackedMechs.Remove(pawn);
        if (pawnData.originalFaction != null)
        {
            pawn.RevertToFaction(pawnData.originalFaction);
        }
    }

    public static void FailOnPlatformNoLongerUsable(this Toil toil, TargetIndex bedIndex)
    {
        toil.FailOnDespawnedOrNull(bedIndex);
        toil.FailOn(() => ((Building_Bed)toil.actor.CurJob.GetTarget(bedIndex).Thing).IsBurning());
        toil.FailOn(() =>
            !HealthAIUtility.ShouldSeekMedicalRest(toil.actor) &&
            !HealthAIUtility.ShouldSeekMedicalRestUrgent(toil.actor) &&
            ((Building_Bed)toil.actor.CurJob.GetTarget(bedIndex).Thing).Medical);
        toil.FailOn(() => toil.actor.IsColonist && !toil.actor.CurJob.ignoreForbidden && !toil.actor.Downed &&
                          toil.actor.CurJob.GetTarget(bedIndex).Thing.IsForbidden(toil.actor));
    }

    //public static bool OnHackingTable(this Pawn pawn)
    //{
    //     return pawn.CurrentBed() != null && pawn.CurrentBed() is Building_HackingTable &&
    //             HealthAIUtility.ShouldHaveSurgeryDoneNow(pawn);
    // }

    public static bool OnHackingTable(this Pawn pawn)
    {
        var bed = pawn.CurrentBed() as Building_HackingTable;
        if (bed == null) return false;

        // True if pawn is physically in the slot
        return pawn.Spawned && pawn.Position == bed.GetSleepingSlotPos(Building_HackingTable.SLOTINDEX);
    }

    public static bool OnBaseMechanoidPlatform(this Pawn pawn)
    {
        return pawn.CurrentBed() != null && pawn.CurrentBed() is Building_BaseMechanoidPlatform curBed &&
               curBed == pawn.ownership.OwnedBed;
    }

    public static bool HasValidCaravanPlatform(this Pawn pawn)
    {
        var caravan = pawn.GetCaravan();
        if (caravan == null)
        {
            return false;
        }

        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        return pawnData.caravanPlatform != null && pawnData.caravanPlatform.CaravanPawn == pawn;
    }

    public static Building_PortableChargingPlatform CaravanPlatform(this Pawn pawn)
    {
        var caravan = pawn.GetCaravan();
        if (caravan == null)
        {
            return null;
        }

        var pawnData = Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
        if (pawnData.caravanPlatform != null && pawnData.caravanPlatform.CaravanPawn == pawn)
        {
            return pawnData.caravanPlatform;
        }

        return null;
    }

    public static bool IsActivated(this Pawn pawn)
    {
        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null)
        {
            return false;
        }

        var pawnData = store.GetExtendedDataFor(pawn);
        return pawnData.isActive;
    }

    public static bool ShouldRecharge(this Pawn pawn)
    {
        // Is basically just a copy of JobGiver_GetEnergy at this point
        var energy = pawn.needs.energy;
        if (energy == null)
            return false;
        
        return (energy.CurLevelPercentage + 0.1f) * 100 < JobGiver_GetEnergy.GetMinAutorechargeThreshold(pawn);
    }

    public static bool ShouldBeMaintained(this Pawn pawn)
    {
        if (pawn.needs.TryGetNeed<Need_Maintenance>() is { } maintenanceNeed)
        {
            return maintenanceNeed.CurLevelPercentage < maintenanceNeed.maintenanceThreshold;
        }

        return false;
    }


    public static bool CanStartWorkNow(this Pawn pawn)
    {
        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null)
            return false;

        if (pawn.Downed ||
            pawn.ShouldRecharge() ||
            pawn.ShouldBeMaintained() ||
            pawn.OnHackingTable())
        {
            return false;
        }

        var pawnData = store.GetExtendedDataFor(pawn);
        return pawnData.canWorkNow;
    }


    public static bool HasFuel(this Caravan caravan)
    {
        return caravan.AllThings.Any(thing => thing.def == ThingDefOf.Chemfuel && thing.stackCount > 0);
    }

    public static ExtendedPawnData GetExtendedData(this Pawn pawn) =>
        Base.Instance.GetExtendedDataStorage().GetExtendedDataFor(pawn);
}