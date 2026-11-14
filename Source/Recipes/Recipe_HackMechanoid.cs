using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using WhatTheHack.Buildings;
using Random = System.Random;

namespace WhatTheHack.Recipes;

public class Recipe_HackMechanoid : Recipe_Hacking
{
    protected new bool allowMultipleParts = false;

    protected override bool IsValidPawn(Pawn pawn)
    {
        return pawn.Faction != Faction.OfPlayer;
    }

    protected override void PostSuccessfulApply(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients,
        Bill bill) => WhatTheHackUtility.ApplyHack(pawn, billDoer);


    protected override void HackingFailEvent(Pawn hacker, Pawn hackee, BodyPartRecord part, Random r)
    {
        int[] chances =
        {
        WhatTheHackMod.Settings.failureChanceHackPoorly,
        WhatTheHackMod.Settings.failureChanceCauseRaid,
        WhatTheHackMod.Settings.failureChanceShootRandomDirection,
        WhatTheHackMod.Settings.failureChanceHealToStanding,
        WhatTheHackMod.Settings.failureChanceNothing
    };

        var totalChance = chances.Sum();
        var randInt = r.Next(1, totalChance);
        Action<Pawn, BodyPartRecord, RecipeDef>[] functions =
        {
        RecipeUtility.HackPoorly,
        RecipeUtility.CauseMechanoidRaidByHackingFailure,
        RecipeUtility.ShootRandomDirection,
        RecipeUtility.HealToStanding,
        RecipeUtility.Nothing
    };

        var acc = 0;
        for (var i = 0; i < chances.Length; i++)
        {
            if (randInt < acc + chances[i])
            {
                functions[i].Invoke(hackee, part, recipe);
                break;
            }
            acc += chances[i];
        }

        // 🔧 Failsafe for adding the mech back to the table
        if (hackee.Downed && !hackee.IsHacked())
        {
            Building_HackingTable table = hackee.CurrentBed() as Building_HackingTable;

            // Fallback: find a nearby hacking table if the pawn isn't in one
            if (table == null && hackee.Map != null)
            {
                table = GenClosest.ClosestThingReachable(
                    hackee.Position,
                    hackee.Map,
                    ThingRequest.ForDef(WTH_DefOf.WTH_HackingTable),
                    PathEndMode.InteractionCell,
                    TraverseParms.For(hackee),
                    maxDistance: 10f
                ) as Building_HackingTable;

                if (table != null)
                {
                    Log.Warning($"[WhatTheHack] Failsafe: Found nearby hacking table for {hackee} at {table.Position}.");
                }
            }

            if (table != null)
            {
                table.TryAddPawnForModification(hackee, recipe);
            }
            else
            {
                Log.Warning($"[WhatTheHack] HackingFailEvent: No valid hacking table found for {hackee}, cannot re-add for modification.");
            }
        }
    }


    //Used to make hacking more powerful mechs more difficult. Capped at 1000 points. At this value, hacking is 50% more difficult.  
    private static float CalcCombatPowerFactorCapped(Pawn hackee)
    {
        var combatPowerFactor = Mathf.Min(hackee.kindDef.combatPower / 1000, 1.0f);
        var maxDifficultyPentaly = 0.5f;
        var combatPowerFactorCapped = 1 - (maxDifficultyPentaly * combatPowerFactor);
        return combatPowerFactorCapped;
    }
}