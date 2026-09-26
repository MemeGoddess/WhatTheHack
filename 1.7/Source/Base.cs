using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using WhatTheHack.Comps;
using WhatTheHack.Recipes;
using WhatTheHack.Storage;

namespace WhatTheHack;


public class Base
{
    
    public static List<ThingDef> allMechs;
    public static List<string> allFactionNames;
    public static List<WorkTypeDef> allowedMechWork = new List<WorkTypeDef>();
    public static List<ThingDef> allBelts = new List<ThingDef>();
    public static List<HediffDef> allSpawnableModules = new List<HediffDef>();
    public static List<SkillDef> allowedMechSkills = new List<SkillDef>();

    public static bool enabled_AlienRaces =
        ModsConfig.ActiveModsInLoadOrder.Any(m => m.PackageIdPlayerFacing == "erdelf.HumanoidAlienRaces");

    private static readonly Color highlight1 = new Color(0.5f, 0, 0, 0.1f);

    private ExtendedDataStorage _extendedDataStorage;   

    //temp accessible storage
    public float daysOfFuel = 0;
    public string daysOfFuelReason = "";

    public Base()
    {       
    }

    private static Base _instance;
    public static Base Instance
    {
        get
        {
            if (_instance == null)
                _instance = new Base();
            return _instance;
        }
        private set => _instance = value;
    }


    public static void ImpliedPawnColumnDefsForMechs()
    {
        var workTable = WTH_DefOf.WTH_Work_Mechanoids;
        var moveWorkTypeLabelDown = false;


        foreach (var def in (from d in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder
                     where d.visible && allowedMechWork.Contains(d)
                     select d).Reverse())
        {
            moveWorkTypeLabelDown = !moveWorkTypeLabelDown;
            var d2 = new PawnColumnDef
            {
                defName = $"WorkPriority_{def.defName}",
                workType = def,
                moveWorkTypeLabelDown = moveWorkTypeLabelDown,
                workerClass = typeof(PawnColumnWorker_WorkPriority),
                sortable = true,
                modContentPack = def.modContentPack
            };
            workTable.columns.Insert(
                workTable.columns.FindIndex(x => x.Worker is PawnColumnWorker_CopyPasteWorkPriorities) + 1, d2);
        }
    }

    

    public static void GenerateImpliedRecipeDefs()
    {
        var extraRecipeDefs = ImpliedRecipeDefs();
        foreach (var td in extraRecipeDefs)
        {
            DefGenerator.AddImpliedDef(td);
        }
    }

    public static IEnumerable<RecipeDef> ImpliedRecipeDefs()
    {
        //Add all mount turret recipes. 
        foreach (var def in from d in DefDatabase<ThingDef>.AllDefs
                 where d.HasComp(typeof(CompMountable))
                 select d)
        {
            var r = new RecipeDef
            {
                defName = $"WTH_Mount_{def.defName}",
                label = "WTH_Mount".Translate(def.label),
                jobString = "WTH_Mount_Jobstring".Translate(def.label),
                workerClass = typeof(Recipe_MountTurret),
                appliedOnFixedBodyParts = new List<BodyPartDef> { WTH_DefOf.Reactor },
                anesthetize = false,
                effectWorking = DefDatabase<EffecterDef>.AllDefs.FirstOrDefault(ed => ed.defName == "Repair"),
                surgerySuccessChanceFactor = 99999f,
                modContentPack = def.modContentPack,
                workAmount = 2000f,
                addsHediff = WTH_DefOf.WTH_MountedTurret
            };
            var ic = new IngredientCount();
            ic.SetBaseCount(1f);
            ic.filter.SetAllow(def, true);
            r.ingredients.Add(ic);
            r.fixedIngredientFilter.SetAllow(def, true);
            r.recipeUsers = new List<ThingDef>();
            r.modExtensions = new List<DefModExtension>
            {
                new DefModExtension_Recipe
                {
                    requireBed = true,
                    requiredHediff = WTH_DefOf.WTH_TurretModule
                }
            };
            foreach (var current in DefDatabase<ThingDef>.AllDefs.Where(d =>
                         d.category == ThingCategory.Pawn && d.race.IsMechanoid))
            {
                r.recipeUsers.Add(current);
            }

            r.ResolveReferences();
            yield return r;
        }

        //Add all remove module hediffs
        foreach (var def in from d in DefDatabase<HediffDef>.AllDefs
                 where d.HasModExtension<DefModextension_Hediff>()
                 select d)
        {
            var modExt = def.GetModExtension<DefModextension_Hediff>();
            if (!modExt.canUninstall)
            {
                continue;
            }

            var r = new RecipeDef
            {
                defName = $"WTH_UninstallModule_{def.defName}",
                label = "WTH_UninstallModule".Translate(def.label),
                jobString = "WTH_UninstallModule_Jobstring".Translate(def.label),
                appliedOnFixedBodyParts = new List<BodyPartDef> { WTH_DefOf.Reactor },
                workerClass = typeof(Recipe_ModifyMechanoid_UninstallModule),
                anesthetize = false,
                effectWorking = DefDatabase<EffecterDef>.AllDefs.FirstOrDefault(ed => ed.defName == "Repair"),
                surgerySuccessChanceFactor = 99999f,
                modContentPack = def.modContentPack,
                workAmount = 5000f,
                recipeUsers = new List<ThingDef>(),
                modExtensions = new List<DefModExtension>
                {
                    new DefModExtension_Recipe
                    {
                        requireBed = true,
                        requiredHediff = def
                    }
                }
            };
            foreach (var current in DefDatabase<ThingDef>.AllDefs.Where(d =>
                         d.category == ThingCategory.Pawn && d.race.IsMechanoid))
            {
                r.recipeUsers.Add(current);
            }

            r.ResolveReferences();
            yield return r;
        }
    }

    public static void SetMechMarketValue()
    {
        foreach (var kind in from kd in DefDatabase<PawnKindDef>.AllDefs where kd.IsMechanoid() select kd)
        {
            if (kind.race.BaseMarketValue < 1.0f && kind.combatPower < 10000f)
            {
                kind.race.BaseMarketValue = kind.combatPower * 3.0f;
            }
        }
    }

    //public static SettingHandle<Dict2DRecordHandler> GetDefaultForFactionRestrictions(
    //    SettingHandle<Dict2DRecordHandler> factionRestrictions, List<ThingDef> allMechs, List<string> allFactionNames)
   // {
   //     factionRestrictions.Value =
   //         GetDefaultForFactionRestrictions(factionRestrictions.Value, allMechs, allFactionNames);
   //     return factionRestrictions;
   // }

    public static Dict2DRecordHandler GetDefaultForFactionRestrictions(Dict2DRecordHandler factionRestrictionsDict,
        List<ThingDef> allMechs, List<string> allFactionNames)
    {
        if (factionRestrictionsDict == null)
        {
            factionRestrictionsDict = new Dict2DRecordHandler();
        }

        if (factionRestrictionsDict.InnerList == null)
        {
            factionRestrictionsDict.InnerList = new Dictionary<string, Dictionary<string, Record>>();
        }

        foreach (var factionDef in from td in DefDatabase<FactionDef>.AllDefs
                 where allFactionNames.Contains(td.defName)
                 select td)
        {
            if (!factionRestrictionsDict.InnerList.ContainsKey(factionDef.defName))
            {
                factionRestrictionsDict.InnerList.Add(factionDef.defName, new Dictionary<string, Record>());
            }
        }

        foreach (var name in allFactionNames)
        {
            var selection = factionRestrictionsDict.InnerList[name];
            GUIDrawUtility.FilterSelection(ref selection, allMechs, name);
            factionRestrictionsDict.InnerList[name] = selection;
        }

        return factionRestrictionsDict;
    }

    public bool EmergencySignalRaidInbound()
    {
        if (GetExtendedDataStorage() != null)
        {
            return Find.TickManager.TicksGame > 0 && Find.TickManager.TicksGame <=
                GetExtendedDataStorage().lastEmergencySignalTick + GetExtendedDataStorage().lastEmergencySignalDelay;
        }

        return false;
    }

    public bool EmergencySignalRaidCoolingDown()
    {
        var storage = GetExtendedDataStorage();
        if (storage == null)
        {
            return false;
        }

        int ticks = Find.TickManager?.TicksGame ?? -1;
        if (ticks < 0)
        {
            return false;
        }

        return ticks <= storage.lastEmergencySignalTick + storage.lastEmergencySignalCooldown;
    }



    //Removes comps if necessary
    //Explanation: Vanilla doesn't support conditional comps. Example: For the repair module, we only want mechs to have comp_refuelable when the mech has one installed. 
    //So to support conditional comps like this, we first allow all comps to be loaded. Then we remove the comps for which the condition doesn't hold. In this case, the refuelable comp for the repair module is
    //removed when a mechanoid doens't have one installed. 
    public static void RemoveComps(ThingWithComps __instance)
    {
        var pawn = (Pawn)__instance;
        //var comps = Traverse.Create(__instance).Field("comps").GetValue<List<ThingComp>>();
        var localComps = new List<ThingComp>();

        foreach (var thingComp in pawn.comps)
        {
            if (thingComp.props is not CompProperties_Refuelable refuelableProps ||
                !refuelableProps.fuelFilter.Allows(WTH_DefOf.WTH_MechanoidParts))
            {
                localComps.Add(thingComp);
                continue;
            }

            if (refuelableProps.fuelFilter.Allows(WTH_DefOf.WTH_MechanoidParts)
                && pawn.IsHacked()
                && pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_RepairModule))
            {
                localComps.Add(thingComp);
            }
        }

        pawn.comps = localComps;
        //Traverse.Create(__instance).Field("comps").SetValue(newComps);
    }

    public ExtendedDataStorage GetExtendedDataStorage()
    {
        if (_extendedDataStorage == null && Find.World != null)
        {
            _extendedDataStorage = Find.World.GetComponent<ExtendedDataStorage>();
        }

        return _extendedDataStorage;
    }
}