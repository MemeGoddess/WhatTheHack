using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using WhatTheHack.Buildings;
using WhatTheHack.Needs;
using WhatTheHack.Storage;

namespace WhatTheHack.Alerts;

internal class Alert_PowerLow : Alert
{
    public Alert_PowerLow()
    {
        defaultLabel = "WTH_Alert_Power_Low_Label".Translate();
        defaultPriority = AlertPriority.High;
    }

    private IEnumerable<Pawn> LowPowerPawns
    {
        get
        {
            foreach (var p in PawnsFinder.AllMaps_Spawned)
            {
                if(p.needs.energy == null)
                    continue;
                
                if(p.needs.energy.CurLevel > 0.1f)
                    continue;
                
                if(p.Faction != Faction.OfPlayer)
                    continue;
                
                if(p.CurrentBed() is Building_BaseMechanoidPlatform)
                    continue;

                if(!p.health.hediffSet.HasHediff<BackupBattery>())
                    continue;
                
                yield return p;
            }
        }
    }

    public override TaggedString GetExplanation()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine();
        foreach (var current in LowPowerPawns)
        {
            stringBuilder.AppendLine($"    {current.Name}");
        }

        stringBuilder.AppendLine();
        return string.Format("WTH_Alert_Power_Low_Description".Translate(), stringBuilder);
    }

    public override AlertReport GetReport()
    {
        return AlertReport.CulpritIs(LowPowerPawns.FirstOrDefault());
    }
}