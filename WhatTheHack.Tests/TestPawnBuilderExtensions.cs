using DevTools.Testing.Utils;
using RimWorld;
using Verse;
using WhatTheHack.Harmony;
using WhatTheHack.Recipes;

namespace WhatTheHack.Tests;

public static class TestPawnBuilderExtensions
{
    public static TestPawnBuilder WithHackBill(this TestPawnBuilder builder) =>
        builder.WithPostSpawn((pawn, index) =>
        {
            if (!pawn.IsMechanoid())
                return;
            
            HealthUtility.DamageUntilDowned(pawn);
            HealthCardUtility.CreateSurgeryBill(pawn, WTH_DefOf.WTH_HackMechanoid,
                pawn.RaceProps.body.AllParts.First(x =>
                    WTH_DefOf.WTH_HackMechanoid.appliedOnFixedBodyParts.Contains(x.def)));
        });

    public static TestPawnBuilder WithHack(this TestPawnBuilder builder) => builder.WithPostSpawn((pawn, index) =>
    {
        if (!pawn.IsMechanoid())
            return;
                
        pawn.health.Reset();
        WhatTheHackUtility.ApplyHack(pawn);
        
        var need = pawn.needs.TryGetNeed<Need_MechEnergy>(); 
        need.CurLevel = need.MaxLevel;
        
        // TODO Lol why are they meleeing?
        var command = (Command_Toggle)Pawn_GetGizmos.CreateGizmo_SearchAndDestroy(pawn, pawn.GetExtendedData());
        command.toggleAction();
    });
}