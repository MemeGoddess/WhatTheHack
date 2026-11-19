// WTH_Startup.cs
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using WhatTheHack;

[StaticConstructorOnStartup]
public static class WTH_Startup
{
    static WTH_Startup()
    {
        // replicate what was in DefsLoaded()
        // Recreate allowedMechWork, allowedMechSkills etc from your old Base.DefsLoaded()

        Base.allowedMechWork = new List<WorkTypeDef>
        {
            WorkTypeDefOf.Hauling,
            WorkTypeDefOf.Growing,
            WorkTypeDefOf.Hunting,
            WorkTypeDefOf.Firefighter,
            WTH_DefOf.Cleaning,
            WTH_DefOf.PlantCutting
        };

        // replicate content from addon mod that used to use DefsLoaded
        if (CompatibilityLoader.ExpandedInstalled)
        {
            WTH_DefOf.WTHExCore_Rescue.visible = true;
            WTH_DefOf.WTHExCore_Husbandry.visible = true;
            WTH_DefOf.WTHExCore_Gastro.visible = true;

            Base.allowedMechWork.Add(WorkTypeDefOf.Mining);
            Base.allowedMechWork.Add(WorkTypeDefOf.Crafting);
            Base.allowedMechWork.Add(WorkTypeDefOf.Research);
            Base.allowedMechWork.Add(WorkTypeDefOf.Doctor);
            Base.allowedMechWork.Add(WorkTypeDefOf.Construction);
            Base.allowedMechWork.Add(WTH_DefOf.WTHExCore_Rescue);
            Base.allowedMechWork.Add(WTH_DefOf.WTHExCore_Husbandry);
            Base.allowedMechWork.Add(WTH_DefOf.WTHExCore_Gastro);

            if (DefDatabase<WorkTypeDef>.GetNamedSilentFail("Gastronomy_Waiting") != null)
            {
                Base.allowedMechWork.Add(DefDatabase<WorkTypeDef>.GetNamed("Gastronomy_Waiting"));
            }

        }

        Base.allowedMechSkills = new List<SkillDef>();
        foreach (var wtd in Base.allowedMechWork)
        {
            foreach (var skill in wtd.relevantSkills)
            {
                if (!Base.allowedMechSkills.Contains(skill))
                    Base.allowedMechSkills.Add(skill);
            }
        }
        Base.allowedMechSkills.Add(SkillDefOf.Melee);
        Base.allowedMechSkills.Add(SkillDefOf.Shooting);

        Base.allBelts = DefDatabase<ThingDef>.AllDefs.Where(t => Utilities.IsBelt(t.apparel)).ToList();
        Base.allSpawnableModules = DefDatabase<HediffDef>.AllDefs
            .Where(h => h.GetModExtension<DefModextension_Hediff>() is { spawnChance: > 0 }).ToList();

        //Base.allMechs = (from td in DefDatabase<ThingDef>.AllDefs where td.race is { IsMechanoid: true } select td).ToList();
        Base.allMechs = (
            from td in DefDatabase<ThingDef>.AllDefs
            where td.race is { IsMechanoid: true }
          && !td.IsCorpse
          && td.category == ThingCategory.Pawn
                select td
                ).ToList();

        Base.allFactionNames = (from td in DefDatabase<FactionDef>.AllDefs
                                where !td.isPlayer && td != FactionDefOf.Mechanoid && td != FactionDefOf.Insect
                                select td.defName).ToList();

        // Prepare default faction restrictions if none present
        var sr = WhatTheHackMod.Settings?.FactionRestrictions;
        Base.GetDefaultForFactionRestrictions(sr, Base.allMechs, Base.allFactionNames);

        // Generate implied defs etc.
        Base.GenerateImpliedRecipeDefs();
        DefDatabase<ThingDef>.ResolveAllReferences();
        Base.SetMechMarketValue();
        Base.ImpliedPawnColumnDefsForMechs();
    }
}
