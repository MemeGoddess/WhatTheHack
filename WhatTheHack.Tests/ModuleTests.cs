using System.Collections;
using DevTools.Testing;
using DevTools.Testing.Utils;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack.Harmony;
using WhatTheHack.Recipes;

namespace WhatTheHack.Tests;

[TestFixture(TestType.Playing)]
public class ModuleTests
{
    public static bool PatchCheckHackingFail() => false;
    private ScopedMethodHook NoFail, NoWarning;
    private Pawn mech = null!;
    private TestPawnBuilder builder;
    private List<Thing> _cleanup = [];
    private float animalDensity = 0f;
    private float autosaveDays = 0.5f;
    
    private IntVec3 _offset => Find.CurrentMap.Center + new IntVec3(10, 0, 0);
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var harmonyMethod = new HarmonyMethod(AccessTools.Method(GetType(), nameof(PatchCheckHackingFail)));
        NoFail = new ScopedMethodHook(AccessTools.Method(typeof(Recipe_Hacking), "CheckHackingFail"),
            prefix: harmonyMethod);
        NoWarning = new ScopedMethodHook(AccessTools.Method(typeof(Bill), nameof(Bill.CreateNoPawnsWithSkillDialog)),
            prefix: harmonyMethod);
        Tweaker.SuperFast = true;
        DebugSettings.unlimitedPower = true;
        
        animalDensity = Find.CurrentMap.Biome.animalDensity;
        Find.CurrentMap.Biome.animalDensity = 0;
        
        autosaveDays = Prefs.AutosaveIntervalDays;
        Prefs.AutosaveIntervalDays = 20f;
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        Tweaker.SuperFast = false;
        DebugSettings.unlimitedPower = false;
        Find.CurrentMap.Biome.animalDensity = animalDensity;
        Prefs.AutosaveIntervalDays = autosaveDays;
    }

    [SetUp]
    public void Setup()
    {
        builder = PawnKindDefOf.Mech_Scyther
            .AsBuilder()
            .WithHack();
        mech = builder.Spawn();
    }

    [TearDown]
    public void TearDown()
    {
        mech!.Destroy();
        foreach (var thing in _cleanup)
        {
            thing.Destroy();
        }
        _cleanup.Clear();
    }

    [Test]
    public IEnumerator Growing()
    {
        WhatTheHackUtility.ApplyModule(mech, WTH_DefOf.WTH_InstallGrowingModule);
        mech.jobs.EndCurrentJob(JobCondition.InterruptForced);
        
        var map = Find.CurrentMap;
        var hydro = (Building_PlantGrower)GenSpawn.Spawn(ThingDefOf.HydroponicsBasin, _offset, map);
        (hydro.PowerComp as CompPowerTrader)!.PowerOn = true;

        _cleanup.AddRange([hydro]);
        _cleanup.ForEach(x => x.SetFaction(Faction.OfPlayer));
        
        yield return new WaitUntilTimeout(() => hydro.PlantsOnMe.Any());
    }
    
    [Test]
    public IEnumerator PlantCutting()
    {
        WhatTheHackUtility.ApplyModule(mech, WTH_DefOf.WTH_InstallGrowingModule);
        mech.jobs.EndCurrentJob(JobCondition.InterruptForced);

        var map = Find.CurrentMap;
        var plant = (Plant)GenSpawn.Spawn(ThingDefOf.Plant_Grass, _offset, map);
        map.designationManager.AddDesignation(
            new Designation(plant, DesignationDefOf.CutPlant));

        yield return new WaitUntilTimeout(() => plant.Destroyed);
    }

    [Test]
    public IEnumerator Hauling()
    {
        WhatTheHackUtility.ApplyModule(mech, WTH_DefOf.WTH_InstallHaulingModule);
        mech.jobs.EndCurrentJob(JobCondition.InterruptForced);

        var map = Find.CurrentMap;
        var wood = ThingDefOf.WoodLog;
        
        var stockPile = Utils.CreateStockpile(map, [map.Center + new IntVec3(1, 0, 0)]);
        var stack = GenSpawn.Spawn(wood, _offset, map);
        stack.stackCount = 1;
        _cleanup.Add(stack);
        
        yield return new WaitUntilTimeout(() => stockPile.AllContainedThings.Any(x => x.def == wood));
        stockPile.Delete();
    }

    [Test]
    public IEnumerator Firefighting()
    {
        WhatTheHackUtility.ApplyModule(mech, WTH_DefOf.WTH_InstallFirefightingModule);
        mech.jobs.EndCurrentJob(JobCondition.InterruptForced);
        
        var map = Find.CurrentMap;
        var wood = ThingDefOf.WoodLog;
        var fire = ThingDefOf.Fire;
        
        var woodStack =  GenSpawn.Spawn(wood, _offset, map);
        var fireStack = (Fire)GenSpawn.Spawn(fire, _offset, map);
        fireStack.fireSize = 1f;
        
        _cleanup.Add(woodStack);

        yield return new WaitUntilTimeout(() => fireStack.Destroyed);
    }

    [Test]
    public IEnumerator Cleaning()
    {
        WhatTheHackUtility.ApplyModule(mech, WTH_DefOf.WTH_InstallCleaningModule);
        mech.jobs.EndCurrentJob(JobCondition.InterruptForced);

        FilthMaker.TryMakeFilth(_offset, Find.CurrentMap, ThingDefOf.Filth_Blood);
        Expect.IsTrue(_offset.GetThingList(Find.CurrentMap).Any(x => x.def == ThingDefOf.Filth_Blood));

        yield return new WaitUntilTimeout(() => _offset.GetThingList(Find.CurrentMap).All(y => y is not Filth));
    }

    [Test]
    public IEnumerator Battery()
    {
        var powerProduction = builder.Spawn();
        powerProduction.health.AddHediff(WTH_DefOf.WTH_VanometricModule);
        // Should be above 95%
        
        var batteryExpansion = builder.Spawn();
        batteryExpansion.health.AddHediff(WTH_DefOf.WTH_BatteryExpansionModule);
        // Should be above 65%

        yield return new WaitUntilTimeout(() => mech.needs.energy.CurLevel <= 90f, GenDate.TicksPerDay * 5);
        
        var powerProductionCurLevel = powerProduction.needs.energy.CurLevel;
        Expect.GreaterThan(powerProduction.needs.energy.CurLevel, 98f, $"Power Production isn't working, curLevel is {powerProductionCurLevel}");
        var batteryExpansionCurLevel = batteryExpansion.needs.energy.CurLevel;
        Expect.GreaterThan(batteryExpansionCurLevel, 91f, $"Battery Expansion isn't working, curLevel is {batteryExpansionCurLevel}");
    }

}