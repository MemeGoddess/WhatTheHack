using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using WhatTheHack.Comps;

namespace WhatTheHack.Harmony;

[HarmonyPatch(typeof(Thing), "get_DrawPos")]
internal static class Thing_get_DrawPos_TurretsOnly
{
    private static bool Prefix(Thing __instance, ref Vector3 __result)
    {
        if (__instance is Building_TurretGun turret)
        {
            var comp = turret.GetComp<CompMountable>();
            if (comp?.overrideDrawPos is { } pos)
            {
                __result = pos;
                return false;
            }
        }
        return true;
    }
}

