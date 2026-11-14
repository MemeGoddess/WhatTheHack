using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using WhatTheHack.Buildings;
using WhatTheHack.Storage;

namespace WhatTheHack.Harmony;

//TODO Improve these patches

[HarmonyPatch(typeof(Need_MechEnergy), nameof(Need_MechEnergy.GUIChangeArrow), MethodType.Getter)]
public static class RestUtility_IsCharging
{
    public static bool Prefix(Need_MechEnergy __instance, ref int __result)
    {
        var pawn = __instance.pawn;
        if (pawn == null)
            return true;
        
        if (!__instance.pawn.health.hediffSet.HasHediff<BackupBattery>())
            return true;
        
        if (!pawn.OnBaseMechanoidPlatform()) 
            return true;

        __result = __instance.CurLevel >= __instance.MaxLevel ? 0 : 1;
        return false;
    }
}

[HarmonyPatch(typeof(InspectPaneFiller), "DrawMechEnergy")]
public static class InspectPaneFiller_DrawMechEnergy
{
    public static bool Prepare() => !ModsConfig.BiotechActive;
    
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.MatchStartForward(new CodeMatch(OpCodes.Ldstr, "Energy"))
            .ThrowIfInvalid("Unable to find Energy label in DrawMechEnergy");

        matcher.Instruction.operand = "PowerSwitch_Power";
        return matcher.Instructions();
    }
}

[HarmonyPatch(typeof(Need_MechEnergy), nameof(Need_MechEnergy.GetTipString))]
internal static class Need_MechEnergy_GetTipString
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    { 
        var matcher = new CodeMatcher(instructions);

        matcher.End();
        matcher.MatchStartBackwards([
            new(x => x.opcode == OpCodes.Callvirt),
            new(x => x.opcode == OpCodes.Ret)
        ])
        .ThrowIfInvalid("Unable to find exit point for TipString");
        
        var label = generator.DefineLabel();
        matcher.Instruction.labels.Add(label);

        matcher.Start();
        matcher.MatchStartForward(new CodeMatch(x => x.opcode == OpCodes.Newobj))
            .ThrowIfInvalid("Unable to find StringBuilder being created");
        matcher.InsertAfter([
            new CodeInstruction(OpCodes.Ldsfld,
                AccessTools.Field(typeof(ModsConfig), nameof(ModsConfig.biotechActive))),
            new CodeInstruction(OpCodes.Brfalse, label)
        ]).ThrowIfInvalid("Instruction list invalid after skipping stat when Biotech not installed");
        
        return matcher.Instructions();
    }
}

[HarmonyPatch(typeof(Need_MechEnergy), nameof(Need_MechEnergy.FallPerDay), MethodType.Getter)]
internal static class Need_MechEnergy_FallPerDay
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var matcher = new CodeMatcher(instructions);
        matcher.End();

        var label = generator.DefineLabel();
        matcher.Instruction.labels.Add(label);

        matcher.MatchStartBackwards([
            new(x => x.opcode == OpCodes.Ldarg_0),
            new(x => x.opcode == OpCodes.Ldfld),
            new(x => x.LoadsField(AccessTools.Field(typeof(StatDefOf), nameof(StatDefOf.MechEnergyUsageFactor)))),
            new(x => x.opcode == OpCodes.Ldc_I4_1),
            new(x => x.opcode == OpCodes.Ldc_I4_M1),
            new(x => x.Calls(AccessTools.Method(typeof(StatExtension), nameof(StatExtension.GetStatValue)))),
            new(x => x.opcode == OpCodes.Mul),
            new(x => x.opcode == OpCodes.Ret),
        ])
        .ThrowIfInvalid("Unable to find Stat DefOf.MechEnergyUsageFactor");


        if(!ModsConfig.BiotechActive)
            matcher.RemoveInstructions(7);
        return matcher.Instructions();
    }

    public static void Postfix(ref float __result, Need_MechEnergy __instance)
    {
        __result /= __instance.pawn.GetStatValue(WTH_DefOf.WTH_BatteryCapacity) / 100f;
        __result /= __instance.pawn.GetStatValue(WTH_DefOf.WTH_BatteryCapacityPercentage) / 100f;
        __result *= __instance.pawn.GetStatValue(WTH_DefOf.WTH_PowerRate) / 100f;
        __result *= __instance.pawn.GetStatValue(WTH_DefOf.WTH_PowerRatePercentage) / 100f;
    }
}

[HarmonyPatch]
internal static class Need_MechEnergy_NeedInterval
{
    private static FieldInfo pawnField = AccessTools.Field(typeof(Need_MechEnergy), "pawn");
    private static System.Reflection.MethodBase TargetMethod()
    {
        // Use reflection to find the Biotech class safely
        var type = AccessTools.TypeByName("RimWorld.Need_MechEnergy");
        return AccessTools.Method(type, "NeedInterval");
    }

    private static bool Prefix(Need_MechEnergy __instance)
    {
        // Get the pawn field dynamically
        var pawn = pawnField?.GetValue(__instance) as Pawn;

        if (pawn == null)
            return true; // If pawn is missing, run vanilla behavior

        // Skip the drain logic if:
        // 1. Pawn is hacked (player controlled via WTH)
        // 2. Pawn belongs to a non-player faction
        if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer)
            return false; // skip original method

        var powerProduction = pawn.GetStatValue(WTH_DefOf.WTH_PowerProduction);
        if(powerProduction > 0.0f)
            __instance.CurLevel += powerProduction / GenDate.TicksPerDay * 150f;
        if (DirectlyPowered(pawn) || pawn.HasValidCaravanPlatform() && pawn.GetCaravan()?.HasFuel() is true)
        {
            var oldMax = pawn.GetStatValue(WTH_DefOf.WTH_BatteryCapacity) / 100f;
            __instance.CurLevel += PowerProduction(__instance, pawn) * 150f / oldMax;
            FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, WTH_DefOf.WTH_Fleck_Charging);
            SetHediffs(__instance, pawn);
            return false;
        }

        return true; // allow vanilla behavior otherwise
    }
    
    public static bool DirectlyPowered(Pawn pawn)
    {
        return !pawn.IsActivated() && pawn.OnBaseMechanoidPlatform() &&
               ((Building_BaseMechanoidPlatform)pawn.CurrentBed()).HasPowerNow();
    }

    private static float PowerProduction(Need_MechEnergy __instance, Pawn pawn)
    {
        var result = 0f;
        Building_BaseMechanoidPlatform platform = null;
        if (pawn.CurrentBed() is Building_BaseMechanoidPlatform mapPlatform && mapPlatform.HasPowerNow())
        {
            platform = mapPlatform;
        }
        else if (pawn.CaravanPlatform() is { } caravanPlatform &&
                 pawn.GetCaravan().HasFuel())
        {
            platform = caravanPlatform;
        }

        if (platform != null)
        {
            result += platform.GetStatValue(WTH_DefOf.WTH_RechargeRate);
        }

        result /= GenDate.TicksPerDay;
        return result;
    }
    
    public static void SetHediffs(Need_MechEnergy __instance, Pawn pawn)
    {
        if (__instance.CurLevelPercentage > 0f)
        {
            var noPowerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(WTH_DefOf.WTH_NoPower);
            if (noPowerHediff != null)
            {
                pawn.health.RemoveHediff(noPowerHediff);
            }
        }
        else if (!pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_NoPower))
        {
            var noPowerHediff = HediffMaker.MakeHediff(WTH_DefOf.WTH_NoPower, pawn);
            pawn.health.AddHediff(noPowerHediff);
        }

        if (__instance.CurLevelPercentage > 0.1f)
        {
            var veryLowPowerHediff = pawn.health.hediffSet.GetFirstHediffOfDef(WTH_DefOf.WTH_VeryLowPower);
            if (veryLowPowerHediff != null)
            {
                pawn.health.RemoveHediff(veryLowPowerHediff);
            }
        }
        else if (!pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_VeryLowPower))
        {
            var veryLowPowerHediff = HediffMaker.MakeHediff(WTH_DefOf.WTH_VeryLowPower, pawn);
            pawn.health.AddHediff(veryLowPowerHediff);
        }
    }
}