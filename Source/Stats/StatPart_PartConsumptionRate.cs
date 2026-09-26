using System.Text;
using RimWorld;
using Verse;
using WhatTheHack.Buildings;

namespace WhatTheHack.Stats;

internal class StatPart_PartConsumptionRate : StatPart
{
    public override string ExplanationPart(StatRequest req)
    {
        var sb = new StringBuilder();
        if (req.Thing is not Building_MechanoidPlatform platform)
        {
            return sb.ToString();
        }

        sb.AppendLine("WTH_Explanation_ConsumptionRate".Translate() + ": " +
                      platform.refuelableComp.Props.fuelConsumptionRate.ToStringByStyle(ToStringStyle.Integer));
        if (WhatTheHackMod.Settings.repairConsumptionModifier != 1f)
        {
            sb.AppendLine("WTH_Explanation_RepairConsumptionModifier".Translate() + ": " +
                          (1f - WhatTheHackMod.Settings.powerFallModifier).ToStringByStyle(ToStringStyle.PercentZero,
                              ToStringNumberSense.Offset));
        }

        return sb.ToString();
    }

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is not Building_MechanoidPlatform platform)
        {
            return;
        }

        val += platform.refuelableComp.Props.fuelConsumptionRate;
        val *= WhatTheHackMod.Settings.repairConsumptionModifier;
    }
}