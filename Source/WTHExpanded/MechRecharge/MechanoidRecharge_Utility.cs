using Verse;
using WhatTheHack;
using WhatTheHack.Needs;

namespace WTHExCore.MechRecharge
{
    public class MechanoidRecharge_Utility
    {
        public static float chargeValue = 0.3f;

        public static void BatteryCharge(Pawn pawn)
        {
            var powerNeed = pawn.needs.energy;
            powerNeed?.CurLevel += powerNeed.MaxLevel * chargeValue;
        }
    }
}
