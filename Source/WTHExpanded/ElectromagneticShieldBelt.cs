using System.Reflection;
using RimWorld;
using Verse;

namespace WTHExCore
{
    public class ElectromagneticShieldBelt : CompShield
    {
        // Cache private CompShield methods
        private static readonly MethodInfo miBreak =
            typeof(CompShield).GetMethod("Break", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miAbsorbedDamage =
            typeof(CompShield).GetMethod("AbsorbedDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        public new CompProperties_ElectromagneticShieldBelt Props =>
            (CompProperties_ElectromagneticShieldBelt)props;

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            // Shield must be active and have an owner
            if (ShieldState != ShieldState.Active || PawnOwner == null)
                return;

            // Only absorb EMP damage
            if (dinfo.Def == DamageDefOf.EMP)
            {
                float drain = dinfo.Amount * Props.energyLossPerDamage * Props.empDrainMultiplier;
                energy -= drain;

                if (energy <= 0f)
                {
                    miBreak?.Invoke(this, null);
                }
                else
                {
                    miAbsorbedDamage?.Invoke(this, new object[] { dinfo });
                }

                absorbed = true;
            }
        }
    }
}
