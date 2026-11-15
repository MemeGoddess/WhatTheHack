using RimWorld;
using Verse;
using HarmonyLib;
using WhatTheHack;
using WhatTheHack.Buildings;
using Verse.AI;
using static WhatTheHack.CompatibilityLoader;

namespace WTHExCore.Portable_Hacking_Table
{
    [HarmonyPatch(typeof(RestUtility), "CanUseBedEver")]
    [HarmonyPatchCategory(Expanded)]
    class RestUtility_CanUseBedEver
    {
        static bool Prefix(ref bool __result, Pawn p, ThingDef bedDef)
        {
            if (!p.RaceProps.IsMechanoid && bedDef == WTHExCore_DefOf.WTHExCore_PortableHackingTable)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    
    public class Utilities_GetAvailableHackingTable
    {
        public static bool Prefix(ref Building_HackingTable __result, Pawn pawn, Pawn targetPawn)
        {
            Building_HackingTable portable = (Building_HackingTable)GenClosest.ClosestThingReachable(targetPawn.Position, targetPawn.Map, ThingRequest.ForDef(WTHExCore_DefOf.WTHExCore_PortableHackingTable), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, delegate (Thing b)
            {
                if (b is Building_HackingTable ht &&
                ht.TryGetComp<CompFlickable>() is CompFlickable flickable && flickable.SwitchIsOn &&
                !b.IsBurning() &&
                !b.IsForbidden(pawn) &&
                pawn.CanReserve(b))
                {
                    if (!(ht.GetCurOccupant(Building_HackingTable.SLOTINDEX) is Pawn pawnOnTable && pawnOnTable.OnHackingTable()))
                    {
                        return true;
                    }

                }
                return false;
            });
            
            Building_HackingTable table = (Building_HackingTable)GenClosest.ClosestThingReachable(targetPawn.Position, targetPawn.Map, ThingRequest.ForDef(WTH_DefOf.WTH_HackingTable), PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, delegate (Thing b)
            {
                if (b is Building_HackingTable ht &&
                ht.TryGetComp<CompFlickable>() is CompFlickable flickable && flickable.SwitchIsOn &&
                !b.IsBurning() &&
                !b.IsForbidden(pawn) &&
                pawn.CanReserve(b))
                {
                    if (!(ht.GetCurOccupant(Building_HackingTable.SLOTINDEX) is Pawn pawnOnTable && pawnOnTable.OnHackingTable()))
                    {
                        return true;
                    }

                }
                return false;
            });

            if (table == null && portable == null)
            {
                return false;
            }
            else
            {
                if (portable == null)
                {
                    __result = table;
                    return false;
                }
                else if (table == null)
                {
                    __result = portable;  
                    return false;
                }
                else
                {
                    if (targetPawn.Position.DistanceTo(portable.Position) < targetPawn.Position.DistanceTo(table.Position))
                    {
                        __result = portable;
                        return false;
                    }
                    else
                    {
                        __result = table;
                        return false;
                    }
                }                
            }          
        }
    }
}
