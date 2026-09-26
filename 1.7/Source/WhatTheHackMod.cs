// WhatTheHackMod.cs (replace existing DrawFactionRestrictionsUI + Dialog implementation)
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace WhatTheHack
{
    public class WhatTheHackMod : Mod
    {
        public static WhatTheHackSettings Settings;
        private Vector2 scrollPosition;

        public WhatTheHackMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<WhatTheHackSettings>();            

        }

        public override string SettingsCategory() => "WTH_ModName".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var viewRect = new Rect(0f, 0f, inRect.width - 20f, 1300f); // slightly extended height
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);

            var listing = new Listing_Standard();
            listing.Begin(viewRect);

            // --- RAIDS ---
            listing.Label("WTH_SettingsGroup_Raids_Title".Translate().CapitalizeFirst(), -1f, "WTH_SettingsGroup_Raids_Description".Translate());
            listing.Gap(4f);
            listing.CheckboxLabeled("WTH_EnableGroup".Translate(), ref Settings.settingsGroup_Raids);
            listing.Gap(8f);

            if (Settings.settingsGroup_Raids)
            {
                listing.Label("WTH_HackedMechChance_Title".Translate() + ": " + Settings.hackedMechChance + "%", tooltip: "WTH_HackedMechChance_Description".Translate());
                Settings.hackedMechChance = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.hackedMechChance, 0, 100, false, "", "0", "100");
                listing.Gap(6f);

                listing.Label("WTH_MinHackedMechPoints_Title".Translate() + ": " + Settings.minHackedMechPoints, tooltip: "WTH_MinHackedMechPoints_Description".Translate());
                Settings.minHackedMechPoints = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.minHackedMechPoints, 0, 500, false, "", "0", "500");
                listing.Gap(6f);

                listing.Label("WTH_MaxHackedMechPoints_Title".Translate() + ": " + Settings.maxHackedMechPoints, tooltip: "WTH_MaxHackedMechPoints_Description".Translate());
                Settings.maxHackedMechPoints = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.maxHackedMechPoints, 0, 500, false, "", "0", "500");
            }

            listing.GapLine(8f);

            // --- HACK FAILURE ---
            listing.Label("WTH_SettingsGroup_HackFailure_Title".Translate().CapitalizeFirst(), -1f, "WTH_SettingsGroup_HackFailure_Description".Translate());
            listing.Gap(4f);
            listing.CheckboxLabeled("WTH_EnableGroup".Translate(), ref Settings.settingsGroup_HackFailure);
            listing.Gap(8f);

            if (Settings.settingsGroup_HackFailure)
            {
                void AddFailureSlider(string labelKey, string descKey, ref int value)
                {
                    listing.Label(labelKey.Translate() + ": " + value + "%", tooltip: descKey.Translate());
                    value = (int)Widgets.HorizontalSlider(listing.GetRect(24f), value, 0, 100, false, "", "0", "100");
                    listing.Gap(6f);
                }

                AddFailureSlider("WTH_FailureChance_Nothing_Title", "WTH_FailureChance_Nothing_Description", ref Settings.failureChanceNothing);
                AddFailureSlider("WTH_FailureChance_CauseRaid_Title", "WTH_FailureChance_CauseRaid_Description", ref Settings.failureChanceCauseRaid);
                AddFailureSlider("WTH_FailureChance_ShootRandomDirection_Title", "WTH_FailureChance_ShootRandomDirection_Description", ref Settings.failureChanceShootRandomDirection);
                AddFailureSlider("WTH_FailureChance_HealToStanding_Title", "WTH_FailureChance_HealToStanding_Description", ref Settings.failureChanceHealToStanding);
                AddFailureSlider("WTH_FailureChance_HackPoorly_Title", "WTH_FailureChance_HackPoorly_Description", ref Settings.failureChanceHackPoorly);
                AddFailureSlider("WTH_FailureChance_IntRaidTooLarge_Title", "WTH_FailureChance_IntRaidTooLarge_Description", ref Settings.failureChanceIntRaidTooLarge);
            }

            listing.GapLine(8f);

            // --- BALANCE ---
            listing.Label("WTH_SettingsGroup_Balance_Title".Translate().CapitalizeFirst(), -1f, "WTH_SettingsGroup_Balance_Description".Translate());
            listing.Gap(4f);
            listing.CheckboxLabeled("WTH_EnableGroup".Translate(), ref Settings.settingsGroup_Balance);
            listing.Gap(8f);

            if (Settings.settingsGroup_Balance)
            {
                listing.CheckboxLabeled("WTH_MaintenanceDedayEnabled_Title".Translate(), ref Settings.maintenanceDecayEnabled, "WTH_MaintenanceDedayEnabled_Description".Translate());
                listing.Gap(6f);

                if (Settings.maintenanceDecayEnabled)
                {
                    listing.Label("WTH_MaintenanceDedayModifier_Title".Translate() + ": " + Settings.maintenanceDecayModifier.ToString("0.##"),
                        tooltip: "WTH_MaintenanceDedayModifier_Description".Translate());
                    Settings.maintenanceDecayModifier = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.maintenanceDecayModifier, 0f, 2f, false, "", "0", "2");
                    listing.Gap(6f);
                }

                void AddBalanceSlider(string labelKey, string descKey, ref float value, float min, float max, string minLabel, string maxLabel)
                {
                    listing.Label(labelKey.Translate() + ": " + value.ToString("0.##"), tooltip: descKey.Translate());
                    value = Widgets.HorizontalSlider(listing.GetRect(24f), value, min, max, false, "", minLabel, maxLabel);
                    listing.Gap(6f);
                }

                AddBalanceSlider("WTH_RepairConsumptionModifier_Title", "WTH_RepairConsumptionModifier_Description", ref Settings.repairConsumptionModifier, 0.05f, 2f, "0.05", "2");
                AddBalanceSlider("WTH_PartDropRateModifier_Title", "WTH_PartDropRateModifier_Description", ref Settings.partDropRateModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_ChipDropRateModifier_Title", "WTH_ChipDropRateModifier_Description", ref Settings.chipDropRateModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_PowerFallModifier_Title", "WTH_PowerFallModifier_Description", ref Settings.powerFallModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_PowerChargeModifier_Title", "WTH_PowerChargeModifier_Description", ref Settings.powerChargeModifier, 0.05f, 5f, "0.05", "5");

                listing.Label("WTH_DeathOnDownedChance_Title".Translate() + ": " + Settings.deathOnDownedChance.ToString("0.##") + "%",
                    tooltip: "WTH_DeathOnDownedChance_Description".Translate());
                Settings.deathOnDownedChance = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.deathOnDownedChance, 0f, 100f, false, "", "0", "100");
                listing.Gap(6f);

                listing.Label("WTH_DownedOnDeathThresholdChance_Title".Translate() + ": " + Settings.downedOnDeathThresholdChance.ToString("0.##") + "%",
                    tooltip: "WTH_DownedOnDeathThresholdChance_Description".Translate());
                Settings.downedOnDeathThresholdChance = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.downedOnDeathThresholdChance, 0f, 100f, false, "", "0", "100");
                listing.Gap(8f);
            }

            listing.GapLine(8f);

            // --- Faction Restrictions ---
            listing.Label("WTH_FactionRestrictions_Title".Translate());
            listing.Gap(4f);
            if (listing.ButtonText("WTH_OpenFactionRestrictionEditor".Translate()))
            {
                if (Settings.FactionRestrictions.InnerList == null && Base.allFactionNames != null)
                {
                    foreach (var name in Base.allFactionNames)
                    {
                        Settings.FactionRestrictions.GetOrCreateInner(name);
                    }
                }

                Find.WindowStack.Add(new Dialog_EditFactionRestrictions(
                    Settings.FactionRestrictions,
                    Base.allMechs,
                    Base.allFactionNames,
                    () => Settings.tabsHandler,
                    s => Settings.tabsHandler = s
                ));
            }

            listing.End();
            Widgets.EndScrollView();
        }        


    }


}
