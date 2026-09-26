//using HarmonyLib;
//using RimWorld;
//using Verse;

//namespace WhatTheHack.Harmony
//{    [HarmonyPatch(typeof(CompAssignableToPawn))]
//    [HarmonyPatch(nameof(CompAssignableToPawn.ShouldShowAssignmentGizmo))]
//    public static class Patch_CompAssignableToPawn_ShouldShowAssignmentGizmo
//    {
//        public static void Postfix(CompAssignableToPawn __instance, ref bool __result)
//        {
//            if (__instance?.parent?.def?.defName == "WTH_HackingTable")
//            {
//                __result = false;
//            }
//       }
//   }
//}

using HarmonyLib;
using RimWorld;

[HarmonyPatch(typeof(CompAssignableToPawn_Bed), nameof(CompAssignableToPawn_Bed.ShouldShowAssignmentGizmo))]
public static class Patch_ShouldShowAssignmentGizmo
{
    public static bool Prefix(CompAssignableToPawn_Bed __instance, ref bool __result)
    {
        if (__instance?.parent?.def?.defName == "WTH_HackingTable")
        {
            __result = false;
            return false; // skip the original method
        }

        return true; // run original method for everything else
    }
}

