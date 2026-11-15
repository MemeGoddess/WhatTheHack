using RimWorld;
using Verse;

namespace WTHExCore
{
    public class CompProperties_ElectromagneticShieldBelt : CompProperties_Shield
    {
        public new float energyLossPerDamage = 0.02f;   // base energy loss per EMP damage
        public float empDrainMultiplier = 1.5f;     // how much harsher EMP drain is

        public CompProperties_ElectromagneticShieldBelt()
        {
            compClass = typeof(ElectromagneticShieldBelt);
        }
    }
}
