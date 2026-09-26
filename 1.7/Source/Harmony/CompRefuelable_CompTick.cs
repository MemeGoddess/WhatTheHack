using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WhatTheHack.Harmony
{
    //prevent fuel ticking if mechanoid isn't hacked
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.CompTick))]
    public static class WTH_CompRefuelable_CompTick_Patch
    {
        public static bool Prefix(CompRefuelable __instance)
        {
            if (__instance.parent is Pawn p && p.RaceProps.IsMechanoid && !p.IsHacked())
            {
                // Completely suppress fuel tick for non-hacked mechs
                return false;
            }
            return true;
        }
    }
}
