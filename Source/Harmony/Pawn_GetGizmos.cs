using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using WhatTheHack.Comps;
using WhatTheHack.Needs;
using WhatTheHack.Storage;
using WhatTheHack.ThinkTree;

namespace WhatTheHack.Harmony;

[StaticConstructorOnStartup]
[HarmonyPatch(typeof(Pawn), "GetGizmos")]
public class Pawn_GetGizmos
{
    // ======= STATIC CACHES =======
    private static readonly Texture2D TexMechanoidWork = ContentFinder<Texture2D>.Get("UI/MechanoidWork");
    private static readonly Texture2D TexMechanoidWorkThreshold = ContentFinder<Texture2D>.Get("UI/MechanoidWorkThreshold");
    private static readonly Texture2D TexAutoRecharge = ContentFinder<Texture2D>.Get("UI/AutoRecharge");
    private static readonly Texture2D TexEnableSD = ContentFinder<Texture2D>.Get("UI/Enable_SD");
    private static readonly Texture2D TexSelfRepair = ContentFinder<Texture2D>.Get("Things/Mote_HealingCrossGreen");
    private static readonly Texture2D TexRepair = ContentFinder<Texture2D>.Get("Things/Mote_HealingCrossBlue");
    private static readonly Texture2D TexOverdrive = ContentFinder<Texture2D>.Get("Things/OverdriveModule");
    private static readonly Texture2D TexEquipBelt = ContentFinder<Texture2D>.Get("Things/Pawn/Humanlike/Apparel/ShieldBelt/ShieldBelt");
    private static readonly Texture2D TexSelfDestruct = ContentFinder<Texture2D>.Get("UI/Detonate");

    private static TargetingParameters repairParams;
    private static TargetingParameters equipBeltParams;

    private static TargetingParameters GetRepairParams()
    {
        if (repairParams == null)
        {
            repairParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = targ =>
                {
                    if (!targ.HasThing) return false;
                    if (targ.Thing is not Pawn pawn) return false;
                    return !pawn.Downed &&
                           pawn.IsHacked() &&
                           pawn.health?.hediffSet.HasNaturallyHealingInjury() == true &&
                           !pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_Repairing);
                }
            };
        }
        return repairParams;
    }

    private static TargetingParameters GetEquipBeltParams(Pawn pawn)
    {
        if (equipBeltParams == null)
        {
            equipBeltParams = new TargetingParameters
            {
                canTargetPawns = false,
                canTargetItems = true,
                canTargetBuildings = false,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = targ =>
                {
                    if (!targ.HasThing) return false;
                    if (targ.Thing is not Apparel apparel) return false;
                    if (!pawn.HasReplacedAI() && apparel.def == WTH_DefOf.WTH_Apparel_MechControllerBelt)
                        return false;
                    return Utilities.IsBelt(apparel.def.apparel) &&
                           pawn.CanReach(apparel, PathEndMode.ClosestTouch, Danger.Deadly);
                }
            };
        }
        return equipBeltParams;
    }

    // ======= POSTFIX =======
    public static void Postfix(ref IEnumerable<Gizmo> __result, Pawn __instance)
    {
        if (__instance.Dead) return; // Early exit for dead pawns

        var store = Base.Instance.GetExtendedDataStorage();
        if (store == null || __instance.Faction is not { IsPlayer: true }) return;
        if (!__instance.IsHacked()) return;

        var gizmoList = __result as List<Gizmo> ?? new List<Gizmo>(__result);
        AddHackedPawnGizmos(__instance, ref gizmoList, store);
        __result = gizmoList;
    }

    private static void AddHackedPawnGizmos(Pawn __instance, ref List<Gizmo> gizmoList, ExtendedDataStorage store)
    {
        var pawnData = store.GetExtendedDataFor(__instance);
        var maintenanceNeed = __instance.needs.TryGetNeed<Need_Maintenance>();
        var hediffSet = __instance.health.hediffSet;

        bool hasRepairModule = hediffSet.HasHediff(WTH_DefOf.WTH_RepairModule);
        bool hasRepairArm = hediffSet.HasHediff(WTH_DefOf.WTH_RepairArm);
        bool hasOverdrive = hediffSet.HasHediff(WTH_DefOf.WTH_Overdrive) || hediffSet.HasHediff(WTH_DefOf.WTH_Overdrive_GoneTooFar);
        bool hasBeltModule = hediffSet.HasHediff(WTH_DefOf.WTH_BeltModule);
        bool hasSelfDestruct = hediffSet.HasHediff(WTH_DefOf.WTH_SelfDestruct);
        var backupBattery = hediffSet.GetFirstHediff<BackupBattery>();

        gizmoList.Add(CreateGizmo_SearchAndDestroy(__instance, pawnData));

        if (backupBattery != null) gizmoList.Add(CreateGizmo_AutoRecharge(backupBattery));

        if (!__instance.IsColonistPlayerControlled && __instance.apparel != null)
        {
            foreach (var apparelGizmo in __instance.apparel.GetGizmos())
                gizmoList.Add(apparelGizmo);
        }

        if (__instance.workSettings != null)
        {
            gizmoList.Add(CreateGizmo_Work(__instance, pawnData));
            if (backupBattery != null) gizmoList.Add(CreateGizmo_WorkThreshold(backupBattery));
        }

        if (maintenanceNeed != null) gizmoList.Add(CreateGizmo_MaintenanceThreshold(maintenanceNeed));

        if (hasSelfDestruct) gizmoList.Add(CreateGizmo_SelfDestruct(__instance));
        if (hasRepairModule)
        {
            gizmoList.Add(CreateGizmo_SelfRepair(__instance));
            if (hasRepairArm) gizmoList.Add(CreateGizmo_Repair(__instance));
        }
        if (hasBeltModule) gizmoList.Add(CreateGizmo_EquipBelt(__instance));
        if (hasOverdrive) gizmoList.Add(CreateGizmo_Overdrive(__instance));
    }

    // ======= GIZMO CREATORS =======
    private static Gizmo CreateGizmo_Work(Pawn __instance, ExtendedPawnData pawnData) =>
        new Command_Toggle
        {
            defaultLabel = "WTH_Gizmo_Work_Label".Translate(),
            defaultDesc = "WTH_Gizmo_Work_Description".Translate(),
            icon = TexMechanoidWork,
            isActive = () => pawnData.canWorkNow,
            toggleAction = () =>
            {
                pawnData.canWorkNow = !pawnData.canWorkNow;
                if (__instance.CurJob.def != WTH_DefOf.WTH_Mechanoid_Rest)
                    __instance.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        };

    private static Gizmo CreateGizmo_WorkThreshold(BackupBattery backupBattery) =>
        new Command_SetWorkThreshold
        {
            battery = backupBattery,
            defaultLabel = "WTH_Gizmo_WorkThreshold_Label".Translate(),
            defaultDesc = "WTH_Gizmo_WorkThreshold_Description".Translate(),
            icon = TexMechanoidWorkThreshold
        };

    private static Gizmo CreateGizmo_MaintenanceThreshold(Need_Maintenance maintenanceNeed) =>
        new Command_SetMaintenanceThreshold
        {
            maintenanceNeed = maintenanceNeed,
            defaultLabel = "WTH_Gizmo_MaintenanceThreshold_Label".Translate(),
            defaultDesc = "WTH_Gizmo_MaintenanceThreshold_Description".Translate(),
            icon = TexRepair
        };

    internal static Gizmo CreateGizmo_SearchAndDestroy(Pawn __instance, ExtendedPawnData pawnData)
    {
        var disabledReason = "";
        bool disabled = false;

        if (__instance.Downed)
        {
            disabled = true;
            disabledReason = (string)"WTH_Reason_MechanoidDowned".Translate();
        }
        else if (__instance.ShouldRecharge())
        {
            disabled = true;
            disabledReason = (string)"WTH_Reason_PowerLow".Translate();
        }
        else if (__instance.ShouldBeMaintained())
        {
            disabled = true;
            disabledReason = (string)"WTH_Reason_MaintenanceLow".Translate();
        }

        return new Command_Toggle
        {
            defaultLabel = "WTH_Gizmo_SearchAndDestroy_Label".Translate(),
            defaultDesc = "WTH_Gizmo_SearchAndDestroy_Description".Translate(),
            icon = TexEnableSD,
            disabled = disabled,
            disabledReason = disabledReason,
            isActive = () => pawnData.isActive,
            toggleAction = () =>
            {
                pawnData.isActive = !pawnData.isActive;

                if (pawnData.isActive)
                {
                    if (__instance.GetLord() == null || __instance.GetLord().LordJob == null)
                        LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_SearchAndDestroy(), __instance.Map,
                            new List<Pawn> { __instance });

                    __instance.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    if (__instance.relations == null) __instance.relations = new Pawn_RelationsTracker(__instance);
                }
                else
                {
                    __instance.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    var closestPlatform = Utilities.GetAvailableMechanoidPlatform(__instance, __instance);
                    if (closestPlatform != null)
                        __instance.jobs.TryTakeOrderedJob(new Job(WTH_DefOf.WTH_Mechanoid_Rest, closestPlatform));
                }
            }
        };
    }

    private static Gizmo CreateGizmo_AutoRecharge(BackupBattery powerNeed) =>
        new Command_Toggle
        {
            defaultLabel = "WTH_Gizmo_AutoRecharge_Label".Translate(),
            defaultDesc = "WTH_Gizmo_AutoRecharge_Description".Translate(),
            icon = TexAutoRecharge,
            isActive = () => powerNeed.shouldAutoRecharge,
            toggleAction = () => powerNeed.shouldAutoRecharge = !powerNeed.shouldAutoRecharge
        };

    private static Gizmo CreateGizmo_SelfRepair(Pawn __instance)
    {
        var compRefuelable = __instance.GetComp<CompRefuelable>();
        var jobDef = WTH_DefOf.WTH_Ability_Repair;
        var modExt = jobDef.GetModExtension<DefModExtension_Ability>();
        var hediffSet = __instance.health.hediffSet;
        var powerNeed = __instance.needs.energy;


        bool alreadyRepairing = hediffSet.HasHediff(WTH_DefOf.WTH_Repairing);
        bool needsMorePower = powerNeed.CurLevel < modExt.powerDrain;
        bool needsMoreFuel = compRefuelable.Fuel < modExt.fuelDrain;
        bool notActivated = !__instance.IsActivated();
        bool noDamage = !hediffSet.HasNaturallyHealingInjury();

        bool disabled = alreadyRepairing || needsMorePower || needsMoreFuel || notActivated || noDamage;

        string disabledReason = "";
        if (disabled)
        {
            if (alreadyRepairing) disabledReason = "WTH_Reason_AlreadyRepairing".Translate();
            else if (notActivated) disabledReason = "WTH_Reason_NotActivated".Translate();
            else if (noDamage) disabledReason = "WTH_Reason_NoDamage".Translate();
            else if (needsMorePower) disabledReason = "WTH_Reason_NeedsMorePower".Translate(modExt.powerDrain);
            else disabledReason = "WTH_Reason_NeedsMoreFuel".Translate(modExt.fuelDrain);
        }

        return new Command_Action
        {
            defaultLabel = "WTH_Gizmo_SelfRepair_Label".Translate(),
            defaultDesc = "WTH_Gizmo_SelfRepair_Description".Translate(),
            icon = TexSelfRepair,
            disabled = disabled,
            disabledReason = disabledReason,
            action = () => __instance.jobs.StartJob(new Job(WTH_DefOf.WTH_Ability_Repair, __instance),
                JobCondition.InterruptForced)
        };
    }

    private static Gizmo CreateGizmo_Repair(Pawn __instance)
    {
        var compRefuelable = __instance.GetComp<CompRefuelable>();
        var powerNeed = __instance.needs.energy;
        var jobDef = WTH_DefOf.WTH_Ability_Repair;
        var modExt = jobDef.GetModExtension<DefModExtension_Ability>();

        bool alreadyRepairing = __instance.health.hediffSet.HasHediff(WTH_DefOf.WTH_Repairing);
        bool needsMorePower = powerNeed.CurLevel < modExt.powerDrain;
        bool needsMoreFuel = compRefuelable.Fuel < modExt.fuelDrain;
        bool notActivated = !__instance.IsActivated();

        bool disabled = needsMorePower || needsMoreFuel || notActivated;
        string disabledReason = disabled
            ? needsMorePower ? "WTH_Reason_NeedsMorePower".Translate(modExt.powerDrain)
                : needsMoreFuel ? "WTH_Reason_NeedsMoreFuel".Translate(modExt.fuelDrain)
                : "WTH_Reason_NotActivated".Translate()
            : "";

        return new Command_Target
        {
            defaultLabel = "WTH_Gizmo_Mech_Repair_Label".Translate(),
            defaultDesc = "WTH_Gizmo_Mech_Repair_Description".Translate(),
            icon = TexRepair,
            targetingParams = GetRepairParams(),
            disabled = disabled,
            disabledReason = disabledReason,
            action = target =>
            {
                if (target is not { HasThing: true, Thing: Pawn }) return;
                __instance.jobs.StartJob(new Job(WTH_DefOf.WTH_Ability_Repair, target),
                    JobCondition.InterruptForced);
            }
        };
    }

    private static Gizmo CreateGizmo_Overdrive(Pawn __instance)
    {
        var powerNeed = __instance.needs.energy;
        var jobDef = WTH_DefOf.WTH_Ability_Overdrive;
        var modExt = jobDef.GetModExtension<DefModExtension_Ability>();

        bool alreadyOverdriving = __instance.health.hediffSet.HasHediff(WTH_DefOf.WTH_Overdrive) ||
                                  __instance.health.hediffSet.HasHediff(WTH_DefOf.WTH_Overdrive_GoneTooFar);
        bool needsMorePower = powerNeed.CurLevel < modExt.powerDrain;
        bool notActivated = !__instance.IsActivated();

        bool disabled = alreadyOverdriving || needsMorePower || notActivated;

        string disabledReason = disabled
            ? alreadyOverdriving ? "WTH_Reason_AlreadyOverdriving".Translate()
                : needsMorePower ? "WTH_Reason_NeedsMorePower".Translate(modExt.powerDrain)
                : "WTH_Reason_NotActivated".Translate()
            : "";

        return new Command_Action
        {
            defaultLabel = "WTH_Gizmo_Overdrive_Label".Translate(),
            defaultDesc = "WTH_Gizmo_Overdrive_Description".Translate(),
            icon = TexOverdrive,
            disabled = disabled,
            disabledReason = disabledReason,
            action = () => __instance.jobs.StartJob(new Job(WTH_DefOf.WTH_Ability_Overdrive, __instance),
                JobCondition.InterruptForced)
        };
    }

    private static Gizmo CreateGizmo_EquipBelt(Pawn pawn) =>
        new Command_Target
        {
            defaultLabel = "WTH_Gizmo_Mech_EquipBelt_Label".Translate(),
            defaultDesc = "WTH_Gizmo_Mech_EquipBelt_Description".Translate(),
            icon = TexEquipBelt,
            targetingParams = GetEquipBeltParams(pawn),
            disabled = !pawn.IsActivated(),
            disabledReason = !pawn.IsActivated() ? "WTH_Reason_NotActivated".Translate() : "",
            action = target =>
            {
                if (target is not { HasThing: true, Thing: Apparel apparel }) return;
                if (!Utilities.IsBelt(apparel.def.apparel)) return;
                apparel.SetForbidden(false);
                pawn.jobs.TryTakeOrderedJob(new Job(JobDefOf.Wear, apparel), JobTag.Misc);
            }
        };

    private static Gizmo CreateGizmo_SelfDestruct(Pawn pawn)
    {
        var powerNeed = pawn.needs.energy;
        var jobDef = WTH_DefOf.WTH_Ability_SelfDestruct;
        var modExt = jobDef.GetModExtension<DefModExtension_Ability>();

        bool needsMorePower = powerNeed.CurLevel < modExt.powerDrain;
        bool notActivated = !pawn.IsActivated();
        bool isDisabled = needsMorePower || notActivated;

        string disabledReason = isDisabled
            ? notActivated ? (string)"WTH_Reason_NotActivated".Translate()
                           : (string)"WTH_Reason_NeedsMorePower".Translate(modExt.powerDrain)
            : "";

        return new Command_Action
        {
            defaultLabel = "WTH_Gizmo_SelfDestruct_Label".Translate(),
            defaultDesc = "WTH_Gizmo_SelfDestruct_Description".Translate(),
            icon = TexSelfDestruct,
            disabled = isDisabled,
            disabledReason = disabledReason,
            action = () => pawn.jobs.StartJob(new Job(WTH_DefOf.WTH_Ability_SelfDestruct, pawn),
                JobCondition.InterruptForced)
        };
    }
}
