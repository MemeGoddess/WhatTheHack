// WhatTheHackSettings.cs
using System.Collections.Generic;
using Verse;

namespace WhatTheHack
{
    public class WhatTheHackSettings : ModSettings
    {
        // Basic settings (converted from SettingHandle<T>)
        public bool settingsGroup_Raids = true;
        public int hackedMechChance = 60;
        public int minHackedMechPoints = 0;
        public int maxHackedMechPoints = 50;

        public bool settingsGroup_HackFailure = true;
        public int failureChanceNothing = 55;
        public int failureChanceCauseRaid = 7;
        public int failureChanceShootRandomDirection = 15;
        public int failureChanceHealToStanding = 8;
        public int failureChanceHackPoorly = 10;
        public int failureChanceIntRaidTooLarge = 10;

        public bool settingsGroup_Balance = true;
        public bool maintenanceDecayEnabled = true;
        public float maintenanceDecayModifier = 1.0f;
        public float repairConsumptionModifier = 1.0f;
        public float partDropRateModifier = 1.0f;
        public float chipDropRateModifier = 1.0f;
        public float powerFallModifier = 1.0f;
        public float powerChargeModifier = 1.0f;
        public float deathOnDownedChance = 50f;
        public float downedOnDeathThresholdChance = 25f;

        // The complex 2D dict previously stored in HugsLib SettingHandle<Dict2DRecordHandler>
        public Dict2DRecordHandler FactionRestrictions = new Dict2DRecordHandler();

        // If you previously used "tabs" and other UI state, keep them here:
        public string tabsHandler = "";

        public void ResetFactionRestrictions()
        {
            FactionRestrictions.Clear();
            // (Repopulate here if needed with defaults)
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref settingsGroup_Raids, "settingsGroup_Raids", false);
            Scribe_Values.Look(ref hackedMechChance, "hackedMechChance", 60);
            Scribe_Values.Look(ref minHackedMechPoints, "minHackedMechPoints", 0);
            Scribe_Values.Look(ref maxHackedMechPoints, "maxHackedMechPoints", 50);

            Scribe_Values.Look(ref settingsGroup_HackFailure, "settingsGroup_HackFailure", false);
            Scribe_Values.Look(ref failureChanceNothing, "failureChanceNothing", 55);
            Scribe_Values.Look(ref failureChanceCauseRaid, "failureChanceCauseRaid", 7);
            Scribe_Values.Look(ref failureChanceShootRandomDirection, "failureChanceShootRandomDirection", 15);
            Scribe_Values.Look(ref failureChanceHealToStanding, "failureChanceHealToStanding", 8);
            Scribe_Values.Look(ref failureChanceHackPoorly, "failureChanceHackPoorly", 10);
            Scribe_Values.Look(ref failureChanceIntRaidTooLarge, "failureChanceIntRaidTooLarge", 10);

            Scribe_Values.Look(ref settingsGroup_Balance, "settingsGroup_Balance", true);
            Scribe_Values.Look(ref maintenanceDecayEnabled, "maintenanceDecayEnabled", true);
            Scribe_Values.Look(ref maintenanceDecayModifier, "maintenanceDecayModifier", 1.0f);
            Scribe_Values.Look(ref repairConsumptionModifier, "repairConsumptionModifier", 1.0f);
            Scribe_Values.Look(ref partDropRateModifier, "partDropRateModifier", 1.0f);
            Scribe_Values.Look(ref chipDropRateModifier, "chipDropRateModifier", 1.0f);
            Scribe_Values.Look(ref powerFallModifier, "powerFallModifier", 1.0f);
            Scribe_Values.Look(ref powerChargeModifier, "powerChargeModifier", 1.0f);
            Scribe_Values.Look(ref deathOnDownedChance, "deathOnDownedChance", 50f);
            Scribe_Values.Look(ref downedOnDeathThresholdChance, "downedOnDeathThresholdChance", 25f);

            Scribe_Values.Look(ref tabsHandler, "tabsHandler", "");

            // Save/load the nested dictionary via Dict2DRecordHandler's ExposeData
            FactionRestrictions.ExposeData();
        }
    }
}
