using RimWorld;
using System.Linq;
using Verse;
using WhatTheHack.Buildings;

namespace WhatTheHack.Alerts;

internal class Alert_NotEnoughPlatforms : Alert
{
    public Alert_NotEnoughPlatforms()
    {
        defaultLabel = "WTH_Alert_NotEnoughPlatforms_Label".Translate();
        defaultExplanation = "WTH_Alert_NotEnoughPlatforms_Description".Translate();
    }

    public override AlertReport GetReport()
    {
        return ShouldAlert();
    }

    private bool ShouldAlert()
    {
        var maps = Find.Maps;
        foreach (var map in maps)
        {
            if (NeedPlatforms(map))
            {
                return true;
            }
        }

        return false;
    }

    private bool NeedPlatforms(Map map)
    {
        if (map == null || !map.IsPlayerHome)
        {
            return false;
        }

        var hackedMechanoids = map.mapPawns.AllPawnsSpawned
            .Where(p => p.IsHacked() && p.Faction == Faction.OfPlayer)
            .ToList();

        if (hackedMechanoids.Count == 0 || !map.listerBuildings.allBuildingsColonist.Any())
        {
            return false;
        }

        int mechanoidPlatformCount = map.listerBuildings.allBuildingsColonist
            .Count(b => b is Building_BaseMechanoidPlatform);

        return mechanoidPlatformCount < hackedMechanoids.Count;
    }
}