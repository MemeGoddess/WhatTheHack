using HarmonyLib;
using Verse;
using WhatTheHack.Comps;

namespace WhatTheHack.Harmony;

//TODO: transpile this for better performance. 
[HarmonyPatch(typeof(GenGrid), "Standable")]
internal class GenGrid_Standable
{
    private static void Postfix(ref bool __result, IntVec3 c, Map map)
    {
        if (__result || map == null || !c.InBounds(map))
            return;

        var things = map.thingGrid.ThingsListAtFast(c);

        for (int i = 0; i < things.Count; i++)
        {
            if (things[i] is ThingWithComps twc &&
                twc.GetComp<CompMountable>() is { Active: true })
            {
                __result = true;
                return;
            }
        }
    }
}