using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WhatTheHack.Storage;

namespace WhatTheHack.Harmony;

[HarmonyPatch(typeof(JobGiver_GetEnergy), "GetMinAutorechargeThreshold")]
internal static class JobGiver_GetEnergy_GetMinAutorechargeThreshold
{
    private static bool Prefix(ref int __result, Pawn pawn)
    {
        var hediff = pawn.health.hediffSet.GetFirstHediff<BackupBattery>();
        if (hediff == null)
            return true;

        __result = hediff.shouldAutoRecharge 
            ? Mathf.RoundToInt(hediff.canStartWorkThreshold) 
            : 0;
        return false;
    }
}