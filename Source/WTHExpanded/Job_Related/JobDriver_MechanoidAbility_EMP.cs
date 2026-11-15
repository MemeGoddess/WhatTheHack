using RimWorld;
using Verse;
using WhatTheHack.Jobs;
using WhatTheHack;
using System.Collections.Generic;

namespace WTHExCore
{
    public class JobDriver_MechanoidAbility_EMP : JobDriver_MechanoidAbility
    {

        static private float explosionRange = 3f;
        protected override void PerformAbility(DefModExtension_Ability modExt)
        {
            base.PerformAbility(modExt);
            List<Thing> ignore = new List<Thing>();
            ignore.Add(pawn);
            GenExplosion.DoExplosion(pawn.Position, pawn.Map, explosionRange, DamageDefOf.EMP, pawn, DamageDefOf.EMP.defaultDamage, DamageDefOf.EMP.defaultArmorPenetration, DamageDefOf.EMP.soundExplosion,ignoredThings: ignore);
        }

    }
}
