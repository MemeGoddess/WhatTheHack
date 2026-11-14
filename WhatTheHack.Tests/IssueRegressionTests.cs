using System.Collections;
using System.Reflection;
using DevTools.Testing;
using DevTools.Testing.Utils;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;
using WhatTheHack.Harmony;
using WhatTheHack.Jobs;
using WhatTheHack.WorldIncidents;

namespace WhatTheHack.Tests;

[TestFixture(TestType.Playing)]
public class IssueRegressionTests
{
    private readonly List<Thing> cleanup = [];
    
    [TearDown]
    public void TearDown()
    {
        foreach (var thing in cleanup)
        {
            if (!thing.Destroyed)
                thing.Destroy();
        }

        cleanup.Clear();
    }

    [Test]
    public void RogueAiQueuedActionsCanAddActionsWithoutInvalidatingTheTick()
    {
        var map = Find.CurrentMap;
        var colonist = TestPawns.Worker.Spawn();
        var rogueAI = (Building_RogueAI)GenSpawn.Spawn(
            WTH_DefOf.WTH_RogueAI,
            map.Center + new IntVec3(10, 0, 0),
            map);
        cleanup.AddRange([colonist, rogueAI]);

        var queueField = AccessTools.Field(typeof(Building_RogueAI), "queuedActions");
        var queue = (IList)queueField.GetValue(rogueAI)!;
        var actionItemType = AccessTools.Inner(typeof(Building_RogueAI), "ActionItem");
        var actionItemConstructor = actionItemType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(Action), typeof(int)],
            null);

        object NewActionItem(Action action, int ticks) =>
            actionItemConstructor!.Invoke([action, ticks]);

        queue.Add(NewActionItem(() => queue.Add(NewActionItem(() => { }, 10)), 1));

        var tickQueue = AccessTools.Method(typeof(Building_RogueAI), "TickActionQueue")!;
        Expect.DoesNotThrow(() => tickQueue.Invoke(rogueAI, null),
            "Adding delayed work while the queue is ticking should not throw.");
    }

    [Test]
    public void RogueAiCanRemoveDestroyedTurretsWhileTicking()
    {
        var map = Find.CurrentMap;
        var colonist = TestPawns.Worker.Spawn();
        var rogueAI = (Building_RogueAI)GenSpawn.Spawn(
            WTH_DefOf.WTH_RogueAI,
            map.Center + new IntVec3(10, 0, 0),
            map);
        var turret = ThingMaker.MakeThing(ThingDefOf.Turret_MiniTurret,
            GenStuff.DefaultStuffFor(ThingDefOf.Turret_MiniTurret));
        GenSpawn.Spawn(turret, map.Center + new IntVec3(12, 0, 0), map);
        var controlledTurrets = (IList)AccessTools.Field(typeof(Building_RogueAI), "controlledTurrets")!
            .GetValue(rogueAI)!;
        controlledTurrets.Add(turret);
        cleanup.AddRange([colonist, rogueAI]);
        turret.Destroy();

        var tickRare = AccessTools.Method(typeof(Building_RogueAI), nameof(Building_RogueAI.TickRare))!;
        Expect.DoesNotThrow(() => tickRare.Invoke(rogueAI, null),
            "Removing a destroyed turret during the rare tick should not invalidate list iteration.");
    }

    [Test]
    public void RaidArrivalPostfixDoesNotInsertAlreadyQueuedPawnsAgain()
    {
        var mech = PawnKindDefOf.Mech_Scyther.AsBuilder()
            .WithFaction(Faction.OfMechanoids)
            .Spawn();
        cleanup.Add(mech);

        var previousModules = Base.allSpawnableModules.ToList();
        Base.allSpawnableModules.Clear();
        try
        {
            // The prefix has already appended generated pawns before the postfix initializes them.
            var arrivalPawns = new List<Pawn> { mech };
            IncidentWorker_Raid_TryExecuteWorker.Postfix(
                arrivalPawns,
                new IncidentParms(),
                new List<Pawn> { mech });

            Expect.AreEqual(1, arrivalPawns.Count(pawn => ReferenceEquals(pawn, mech)),
                "Each generated mech should occur once in the arrival list.");
        }
        finally
        {
            Base.allSpawnableModules.AddRange(previousModules);
        }
    }

    [Test]
    public void ClearingHackingTablePreservesUnrelatedSurgeryBills()
    {
        var takee = TestPawns.Worker.Spawn();
        cleanup.Add(takee);
        var table = (Building_HackingTable)ThingMaker.MakeThing(WTH_DefOf.WTH_HackingTable);
        cleanup.Add(table);

        var unrelatedBill = new Bill_Medical(WTH_DefOf.WTH_InstallRepairModule, null);
        takee.health.surgeryBills.AddBill(unrelatedBill);

        var job = JobMaker.MakeJob(WTH_DefOf.WTH_ClearHackingTable, takee, table);
        var driver = new JobDriver_ClearHackingTable();
        AccessTools.Field(typeof(JobDriver), "job").SetValue(driver, job);

        var clearBillsToil = driver.MakeNewToils().ElementAt(1);
        clearBillsToil.initAction();

        Expect.IsTrue(takee.health.surgeryBills.Bills.Contains(unrelatedBill),
            "Clearing a mech from the table must not erase another surgery order.");
    }

    [Test]
    public void HackJobCanReservePartsAcrossMultipleStacks()
    {
        var map = Find.CurrentMap;
        var center = map.Center + new IntVec3(10, 0, 0);
        var worker = TestPawns.Worker.Spawn();
        var mech = PawnKindDefOf.Mech_Scyther.AsBuilder()
            .WithFaction(Faction.OfPlayer)
            .WithLocation(center + new IntVec3(0, 0, 2))
            .Spawn();
        var table = (Building_HackingTable)GenSpawn.Spawn(
            WTH_DefOf.WTH_HackingTable,
            center + new IntVec3(2, 0, 0),
            map);
        var firstParts = GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidParts, center + new IntVec3(0, 0, 1), map);
        var secondParts = GenSpawn.Spawn(WTH_DefOf.WTH_MechanoidParts, center + new IntVec3(1, 0, 1), map);
        firstParts.stackCount = 5;
        secondParts.stackCount = 5;
        cleanup.AddRange([worker, mech, table, firstParts, secondParts]);

        var findPartsStack = AccessTools.Method(typeof(JobDriver_HackMechanoid), "FindPartsStack")!;
        var partsStack = findPartsStack.Invoke(new JobDriver_HackMechanoid(), [worker, 10]);

        Expect.IsNotNull(partsStack,
            "Ten parts split across reachable stacks should satisfy the ten-part requirement.");
    }

    [Test]
    public void HiveTempleRectangleCandidatesAreLocalToEachGeneration()
    {
        var possibleRectsField = AccessTools.Field(typeof(GenStep_HiveTemple), "possibleRects");
        var possibleRects = (IList)possibleRectsField.GetValue(null)!;
        var previousRects = possibleRects.Cast<CellRect>().ToList();
        possibleRects.Clear();

        try
        {
            var genStep = new GenStep_HiveTemple();
            var getRect = AccessTools.Method(typeof(GenStep_HiveTemple), "getRect");
            var center = CellRect.SingleCell(Find.CurrentMap.Center);

            getRect.Invoke(genStep, [center, Find.CurrentMap]);
            var candidateCountAfterFirstCall = possibleRects.Count;
            getRect.Invoke(genStep, [center, Find.CurrentMap]);

            Expect.AreEqual(candidateCountAfterFirstCall, possibleRects.Count,
                "A generation call should not retain candidate rectangles from earlier maps/sites.");
        }
        finally
        {
            possibleRects.Clear();
            foreach (var rect in previousRects)
                possibleRects.Add(rect);
        }
    }

}
