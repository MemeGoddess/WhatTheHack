using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using WhatTheHack.Recipes;

namespace WhatTheHack.Buildings;

public class Building_HackingTable : Building_Bed
{
    public const int SLOTINDEX = 2;
    public CompPowerTrader powerComp;

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        powerComp = GetComp<CompPowerTrader>();
        LessonAutoActivator.TeachOpportunity(WTH_DefOf.WTH_Hacking, OpportunityType.Important);
    }

    public bool TryAddPawnForModification(Pawn pawn, RecipeDef recipeDef)
    {
        if (pawn == null || pawn.Dead)
            return false;

        try
        {
            // 1️ Ensure pawn isn't already in another bed
            if (pawn.InBed() && pawn.CurrentBed() != this)
            {
                Log.Warning($"[WhatTheHack] {pawn} is already in another bed, cannot assign to hacking table at {Position}.");
                return false;
            }

            // 2️ Clear old WhatTheHack bills
            if (pawn.health?.surgeryBills != null)
            {
                var billsToRemove = pawn.health.surgeryBills.Bills
                    .Where(b => b.recipe == WTH_DefOf.WTH_HackMechanoid ||
                                b.recipe == WTH_DefOf.WTH_ShutDownMechanoid ||
                                b.recipe == WTH_DefOf.WTH_InduceEmergencySignal)
                    .ToList();

                foreach (var billa in billsToRemove)
                    pawn.health.surgeryBills.Delete(billa);
            }

            // 3️ Create new hacking bill
            var bill = new Bill_Medical(recipeDef, null);
            var bodyparts = RecipeUtility.GetPartsToApplyOn(pawn, bill.recipe);
            bill.Part = bodyparts.FirstOrDefault();

            if (bill.Part == null)
            {
                Log.Warning($"[WhatTheHack] Could not find valid body part for {recipeDef.defName} on {pawn}.");
                return false;
            }

            pawn.health.surgeryBills.AddBill(bill);

            // 4️ Claim this bed if not already assigned
            if (pawn.ownership?.OwnedBed != this)
            {
                pawn.ownership?.ClaimBedIfNonMedical(this);
            }

            // 5️ Assign rest job explicitly
            if (pawn.CurJob == null || pawn.CurJob.def != WTH_DefOf.WTH_Mechanoid_Rest)
            {
                pawn.jobs?.StartJob(new Job(WTH_DefOf.WTH_Mechanoid_Rest, this), JobCondition.InterruptForced);
            }

            if (pawn.jobs != null && pawn.jobs.curDriver != null)
            {
                pawn.jobs.posture = PawnPosture.LayingInBed;
            }

            // 6️ Reset extended state
            var extData = Base.Instance?.GetExtendedDataStorage()?.GetExtendedDataFor(pawn);
            if (extData != null)
            {
                extData.isActive = false;
                extData.canWorkNow = false;
            }

            // 7️ Clear reservations
            pawn.ClearAllReservations();

            Log.Message($"[WhatTheHack] Added {pawn} to hacking table at {Position} with recipe {recipeDef.defName}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[WhatTheHack] TryAddPawnForModification failed for {pawn}: {ex}");
            return false;
        }
    }


    public bool HasPowerNow()
    {
        return powerComp is { PowerOn: true };
    }

    public override string GetInspectString()
    {
        StringBuilder sb = new StringBuilder();
        string text = InspectStringPartsFromComps();
        if (!text.NullOrEmpty())
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(text);
        }
        return sb.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var baseGizmos = base.GetGizmos();
        if (baseGizmos == null)
            yield break;

        foreach (var gizmo in baseGizmos)
        {
            if (gizmo == null)
                continue;

            // Skip AssignOwner
            if (gizmo is Command cmd && cmd.icon != null && cmd.icon.name == "AssignOwner")
                continue;

            // Skip AsMedical toggle
            if (gizmo is Command_Toggle toggle && toggle.icon != null && toggle.icon.name == "AsMedical")
                continue;

            yield return gizmo;
        }
    }

    public bool TryRemovePawnForModification(Pawn pawn)
    {
        if (pawn == null)
            return false;

        bool removed = false;

        try
        {
            // Eject from bed if currently lying here
            if (pawn.CurrentBed() == this)
            {
                pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced, true);

                // Move pawn safely to interaction cell
                if (pawn.Spawned)
                {
                    pawn.Position = this.InteractionCell;
                    pawn.Notify_Teleported(false, true);
                }

                // Free the slot by checking which occupant matches
                for (int i = 0; i < this.SleepingSlotsCount; i++)
                {
                    Pawn curOccupant = this.GetCurOccupant(i);
                    if (curOccupant == pawn)
                    {
                        this.GetComp<CompAssignableToPawn>()?.TryUnassignPawn(pawn);
                        break;
                    }
                }

                removed = true;
                Log.Message($"[WhatTheHack] Ejected {pawn} from hacking table at {Position}.");
            }

            // 2️ Clear ownership if still linked
            if (pawn.ownership != null && pawn.ownership.OwnedBed == this)
            {
                pawn.ownership.UnclaimBed();
                removed = true;
            }

            // 3️ Clear surgery bills related to WhatTheHack
            if (pawn.health != null && pawn.health.surgeryBills != null)
            {
                var billsToRemove = pawn.health.surgeryBills.Bills
                    .Where(b => b.recipe == null ||
                                b.recipe == WTH_DefOf.WTH_HackMechanoid ||
                                b.recipe == WTH_DefOf.WTH_ShutDownMechanoid ||
                                b.recipe == WTH_DefOf.WTH_InduceEmergencySignal)
                    .ToList();

                foreach (var billa in billsToRemove)
                    pawn.health.surgeryBills.Delete(billa);
            }

            // 4️ Reset extended data
            try
            {
                var extData = Base.Instance?.GetExtendedDataStorage()?.GetExtendedDataFor(pawn);
                if (extData != null)
                {
                    extData.isActive = true;
                    extData.canWorkNow = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[WhatTheHack] Failed to reset extended data for {pawn}: {ex}");
            }

            // 5️ Clear job + reservations
            try
            {
                pawn.ClearAllReservations();
            }
            catch (Exception ex)
            {
                Log.Warning($"[WhatTheHack] Failed to clear reservations for {pawn}: {ex}");
            }

            // 6️ Undraft and reset posture (safe explicit access)
            if (pawn.Drafted)
                pawn.drafter.Drafted = false;

            if (pawn.jobs != null)
                pawn.jobs.posture = PawnPosture.Standing;

            // 7️ Final position safety (if still on table tile)
            if (pawn.Spawned && pawn.Position == this.Position)
            {
                pawn.Position = this.InteractionCell;
                pawn.Notify_Teleported(false, true);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[WhatTheHack] TryRemovePawnForModification failed for {pawn}: {ex}");
        }

        return removed;
    }



    private void RefreshOccupantState(Pawn pawn)
    {
        try
        {
            // Ensure proper bed ownership
            if (pawn.ownership?.OwnedBed != this)
            {
                pawn.ownership?.ClaimBedIfNonMedical(this);
            }

            // Clear drafted/mechanical state
            if (pawn.Drafted)
                pawn.drafter.Drafted = false;

            var extData = Base.Instance?.GetExtendedDataStorage()?.GetExtendedDataFor(pawn);
            if (extData != null)
            {
                extData.isActive = false;
                extData.canWorkNow = false;
            }

            pawn.ClearAllReservations();
            Log.Message($"[WhatTheHack] Refreshed mech state for {pawn} at hacking table {Position}.");
        }
        catch (Exception ex)
        {
            Log.Warning($"[WhatTheHack] Failed to refresh occupant state for {pawn}: {ex}");
        }
    }
}
