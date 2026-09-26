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

[Quickstart("WTHMechs")]
public class MechsQuickstart
{
    private ScopedMethodHook _hook;
    [OneTimeSetUp]
    public void OneTimeSetup() =>
        _hook = new ScopedMethodHook(AccessTools.Method(typeof(Pawn_HealthTracker_HealthTick), "Postfix"), 
            transpiler: new HarmonyMethod(AccessTools.Method(GetType(), nameof(PatchHealRate))));

    private List<Thing> _cleanup = new();
    private List<Pawn> mechs = new(); 
    private Pawn worker = null!;
    [SetUp]
    public IEnumerator Setup()
    {
        var builder = PawnKindDefOf
            .Mech_Pikeman
            .AsBuilder()
            .WithFaction(Faction.OfMechanoids)
            .WithPostSpawn((x, i) => x.Position += new IntVec3(i, 0, -2))
            .WithModifer(pawn => HealthUtility.DamageUntilDowned(pawn));

        mechs.AddRange(builder.SpawnMany(5));

        var map = Find.CurrentMap;  
        var workshopCenter = map.Center +  new IntVec3(0, 0, 10);
        
        var table = (Building_HackingTable)GenSpawn.Spawn(WTH_DefOf.WTH_HackingTable, workshopCenter, map);
        table.SetFaction(Faction.OfPlayer);
        
        var powerCell = GenSpawn.Spawn(WTH_DefOf.VanometricPowerCell, workshopCenter + new IntVec3(0, 0, 2), map);
        powerCell.SetFaction(Faction.OfPlayer);
        
        var platform = (Building_MechanoidPlatform)GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidPlatform, workshopCenter + new IntVec3(-4, 0, 0), map);
        var platformComp = platform.TryGetComp<CompRefuelable>();
        platformComp.Refuel(platformComp.Props.fuelCapacity);
        platform.SetFaction(Faction.OfPlayer);
        
        table.powerComp.PowerOn = true;
        platform.powerComp.PowerOn = true;
        
        var parts = GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidParts, workshopCenter + new IntVec3(0, 0, -3), map);
        parts.stackCount = parts.def.stackLimit;

        worker = TestPawns.Worker
            .WithGear(ThingDefOf.MealSurvivalPack)
            .WithWorkMaxed(WorkTypeDefOf.Doctor, WorkTypeDefOf.Crafting, WorkTypeDefOf.Smithing)
            .Spawn();
        Find.ResearchManager.DebugSetAllProjectsFinished();
        Find.EntityCodex.debug_UnhideAllResearch = true;
        
        _cleanup.AddRange([table, powerCell, platform, parts, ..mechs, worker]);
        
        var mech = mechs.First();
        HealthCardUtility.CreateSurgeryBill(mech, WTH_DefOf.WTH_HackMechanoid,
            mech.RaceProps.body.AllParts.First(x =>
                WTH_DefOf.WTH_HackMechanoid.appliedOnFixedBodyParts.Contains(x.def)));

        if(TestRunner.Active)
            yield return null;

        worker.jobs.debugLog = true;
        yield return new WaitUntilTimeout(() => table.AnyOccupants);
        yield return new WaitTicks(10);
        var job = JobMaker.MakeJob(WTH_DefOf.WTH_HackMechanoidJob, mech, table);
        worker.jobs.TryTakeOrderedJob(job);
        yield return worker.WaitJob(WTH_DefOf.WTH_ClearHackingTable, GenDate.TicksPerDay);
        yield return worker.WaitJob(JobDefOf.Rescue, waitJobType: WaitJobType.StartsJob);
        Find.TickManager.Pause();
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