using RimWorld;
using Verse;
using System.Collections.Generic;
using WhatTheHack;
using System.Reflection;
using System.Text;

namespace WTHExCore.MechRecharge
{

    public class CaravanFuelCalcul_Patch
    {
        public static void Postfix(List<TransferableOneWay> transferables)
        {
            // Absolute safety guards
            if (transferables == null || WhatTheHack.Base.Instance == null)
                return;

            int batteryAmount = 0;
            int mechCount = 0;
            float powerConsumption = 0f;

            foreach (var tow in transferables)
            {
                if (tow?.ThingDef == null)
                    continue;

                if (tow.ThingDef.race?.IsMechanoid == true &&
                    tow.AnyThing is Pawn pawn &&
                    pawn.Spawned &&                      // important
                    pawn.IsHacked() &&
                    !pawn.health.hediffSet.HasHediff(WTH_DefOf.WTH_VanometricModule))
                {
                    float rate = pawn.GetStatValue(WTH_DefOf.WTH_PowerRate);
                    if (rate > 0f)
                    {
                        powerConsumption += rate;
                        mechCount++;
                    }
                }

                if (tow.ThingDef == WTHExCore_DefOf.WTHExCore_MechBatteryCharger)
                {
                    batteryAmount += tow.CountToTransfer;
                }
            }

            // Nothing meaningful to do
            if (mechCount == 0 || batteryAmount == 0 || powerConsumption <= 0f)
                return;

            float batteryFuelDay =
                batteryAmount * MechanoidRecharge_Utility.chargeValue / powerConsumption;

            var baseInst = WhatTheHack.Base.Instance;
            var type = baseInst.GetType();

            var daysOfFuelField = type.GetField("daysOfFuel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var daysOfFuelReasonField = type.GetField("daysOfFuelReason",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (daysOfFuelField == null || daysOfFuelReasonField == null)
                return;

            float currentDays = (float)daysOfFuelField.GetValue(baseInst);
            daysOfFuelField.SetValue(baseInst, currentDays + batteryFuelDay);

            var sb = new StringBuilder(daysOfFuelReasonField.GetValue(baseInst) as string ?? string.Empty);
            sb.AppendLine("Number of Battery : " + batteryAmount);
            sb.AppendLine("Power needed per day for all Mech : " + powerConsumption.ToString("0.#"));
            sb.AppendLine("Day of battery : " + batteryFuelDay.ToString("0.#"));

            daysOfFuelReasonField.SetValue(baseInst, sb.ToString());
        }

    }
}
