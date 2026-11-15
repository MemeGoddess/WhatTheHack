using System.Collections;
using System.Reflection.Emit;
using DevTools.Testing;
using DevTools.Testing.Utils;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WhatTheHack.Buildings;
using WhatTheHack.Harmony;

namespace WhatTheHack.Tests;

// TODO Looks like mech charging doesn't show green arrow when on platform
// TODO They don't get off the platform to haul
[Quickstart("WTHMechs")]
public class MechsQuickstart
{
    // private ScopedMethodHook _hook;
    // [OneTimeSetUp]
    // public void OneTimeSetup() =>
    //     _hook = new ScopedMethodHook(AccessTools.Method(typeof(Pawn_HealthTracker_HealthTick), "Postfix"), 
    //         transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(PatchHealRate))));

    private List<Thing> _cleanup = new();
    private List<Pawn> mechs = new(); 
    private Pawn worker = null!;
    [SetUp]
    public IEnumerator Setup()
    {
        Tweaker.SuperFast = true;
        var builder = PawnKindDefOf
            .Mech_Pikeman
            .AsBuilder()
            .WithFaction(Faction.OfMechanoids)
            .WithPostSpawn((x, i) => x.Position += new IntVec3(i, 0, -2))
            .WithModifer(pawn => HealthUtility.DamageUntilDowned(pawn));

        mechs.AddRange(builder.SpawnMany(5));
        
        // Fuck you >:(
        mechs.Where(x => !x.Downed).ToList().ForEach(x => x.Destroy());

        var map = Find.CurrentMap;  
        var workshopCenter = map.Center +  new IntVec3(0, 0, 10);
        
        var table = (Building_HackingTable)GenSpawn.Spawn(WTH_DefOf.WTH_HackingTable, workshopCenter, map);
        table.SetFaction(Faction.OfPlayer);

        for (var i = 1; i < 10; i++)
        {
            {
                var platform = (Building_MechanoidPlatform)GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidPlatform,
                    workshopCenter - new IntVec3(1 + 3 * i, 0, 0), map);
                var platformComp = platform.TryGetComp<CompRefuelable>();
                platformComp.Refuel(platformComp.Props.fuelCapacity);
                platform.SetFaction(Faction.OfPlayer);
                platform.powerComp.PowerOn = true;
                _cleanup.Add(platform);
            }
            {
                var platform = (Building_MechanoidPlatform)GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidPlatform, workshopCenter - new IntVec3(1 + 3 * i, 0, 4), map);
                var platformComp = platform.TryGetComp<CompRefuelable>();
                platformComp.Refuel(platformComp.Props.fuelCapacity);
                platform.SetFaction(Faction.OfPlayer);
                platform.powerComp.PowerOn = true;
                _cleanup.Add(platform);
            }
        }
        
        table.powerComp.PowerOn = true;
        
        var parts = GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidParts, workshopCenter + new IntVec3(0, 0, -3), map);
        var parts2 = GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidParts, workshopCenter + new IntVec3(0, 0, -4), map);
        parts.stackCount = 5;
        parts2.stackCount = 5;

        worker = TestPawns.Worker
            .WithGear(ThingDefOf.MealSurvivalPack)
            .WithWorkMaxed(WorkTypeDefOf.Doctor, WorkTypeDefOf.Crafting, WorkTypeDefOf.Smithing, WTH_DefOf.WTH_Hack)
            .Spawn();
        Find.ResearchManager.DebugSetAllProjectsFinished();
        Find.EntityCodex.debug_UnhideAllResearch = true;
        
        _cleanup.AddRange([table, parts, ..mechs, worker]);
        
        var mech = mechs.First();
        HealthCardUtility.CreateSurgeryBill(mech, WTH_DefOf.WTH_HackMechanoid,
            mech.RaceProps.body.AllParts.First(x =>
                WTH_DefOf.WTH_HackMechanoid.appliedOnFixedBodyParts.Contains(x.def)));

        if(TestRunner.Active)
            yield break;

        yield return new WaitUntilTimeout(() => table.AnyOccupants);
        yield return new WaitTicks(10);
        var job = JobMaker.MakeJob(WTH_DefOf.WTH_HackMechanoidJob, mech, null, table);
        worker.jobs.TryTakeOrderedJob(job);
        
        // Fight
        yield return new WaitUntil(() => !mech.health.Downed && mech.health.summaryHealth.SummaryHealthPercent >= 1 && !mech.ShouldRecharge());

        IncidentParms parms = new IncidentParms
        {
            target = Find.CurrentMap,
            points = 100,
            forced = true,
            raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn,
        };
        var raidWorker = (IncidentWorker_Raid)IncidentDefOf.RaidEnemy.Worker ;
        raidWorker.TryGenerateRaidInfo(parms, out var raiders);

        if (Find.TickManager.Paused)
            Tweaker.SuperFast = true;
        yield return new WaitTicks(1);
        var command = (Command_Toggle)Pawn_GetGizmos.CreateGizmo_SearchAndDestroy(mech, mech.GetExtendedData());
        command.toggleAction();

        builder.WithHack().WithLocation(mech.Position).SpawnMany(10);

        yield return new WaitUntil(() => raiders.All(x => x.Dead || x.Downed || !x.Spawned));
        builder.Destroy();
        
        // Haul
        if (ModsConfig.BiotechActive)
        {
            var lifters = PawnKindDef
                .Named("Mech_Lifter")
                .AsBuilder()
                .WithFaction(Faction.OfMechanoids)
                .WithHackBill()
                .SpawnMany(3);
            _cleanup.AddRange([..lifters]);

            yield return new WaitUntil(() => lifters.All(lifter =>
                !lifter.health.Downed && lifter.health.summaryHealth.SummaryHealthPercent >= 1 &&
                !lifter.ShouldRecharge()));

        }
        else
        {
            var lifter = PawnKindDefOf.Mech_Scyther
                .AsBuilder()
                .WithHackBill()
                .Spawn();
            GenSpawn.Spawn(WTH_DefOf.WTH_HaulingModule, map.Center + new IntVec3(1, 0, 0), map);
            var recipe = WTH_DefOf.WTH_InstallHaulingModule;
            _cleanup.Add(lifter);
            yield return new WaitUntil(() =>
                !lifter.health.Downed && lifter.health.summaryHealth.SummaryHealthPercent >= 1 &&
                !lifter.ShouldRecharge());
            HealthCardUtility.CreateSurgeryBill(lifter, recipe, lifter.RaceProps.body.AllParts.First(x =>
                recipe.appliedOnFixedBodyParts.Contains(x.def)));
        }
        
        var stockpileCells = GenRadial
            .RadialCellsAround(workshopCenter + new IntVec3(0, 0, 10), 5f, true)
            .ToArray();
        var itemCells = GenRadial
            .RadialCellsAround(workshopCenter + new IntVec3(0, 0, -10), 2f, true)
            .ToArray();
            
        Utils.CreateStockpile(map, stockpileCells);
            
        foreach (var spot in itemCells)
        {
            var haulable = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            haulable.stackCount = haulable.def.stackLimit;
            GenSpawn.Spawn(haulable, spot, map);
        }

        yield return new WaitTicks(GenDate.TicksPerHour * 3);
        
        Tweaker.SuperFast = false;
        
        
        
        // End
        Current.Game.tickManager.CurTimeSpeed = TimeSpeed.Fast;
        Messages.Message(new Message("That's all, folks", MessageTypeDefOf.NeutralEvent));
    }
    
    [TearDown]
    public void Teardown()
    {
        _cleanup.ForEach(x => x.Destroy());
        _cleanup.Clear();
    }

    [Test]
    public void CanHack()
    {
        
    }

    static IEnumerable<CodeInstruction> PatchHealRate(IEnumerable<CodeInstruction> instructions)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchEndForward(
            new(x => x.LoadsField(AccessTools.Field(typeof(WTH_DefOf), nameof(WTH_DefOf.WTH_RepairRate)))),
            new(x => x.opcode == OpCodes.Ldc_I4_1), 
            new(x => x.opcode == OpCodes.Ldc_I4_M1),
            new(x => x.Calls(AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue)))));
        
        matcher.InsertAfter(
            new(OpCodes.Ldc_R4, 100f), 
            new(OpCodes.Mul));

        return matcher.Instructions();
    }
    


}