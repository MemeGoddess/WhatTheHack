using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MemeGoddess.RimWorld.TabsHandler;
using UnityEngine;
using Verse;

namespace WhatTheHack;

public class WhatTheHackSettingsTabs : TabsHandler<SelectedTab>
{
    private readonly WhatTheHackSettings Settings;
    private Dialog_EditFactionRestrictions editFactionRestrictions;

    public WhatTheHackSettingsTabs(WhatTheHackSettings settings)
    {
        Settings = settings;
    }

    public override void DoTabRect(SelectedTab selectedTab, Rect body)
    {
        var listing = new Listing_Standard();
        listing.Begin(body);
        switch(selectedTab)
        {
            case SelectedTab.Raids:
                listing.Label("WTH_HackedMechChance_Title".Translate());
                listing.Gap(4f);
                Settings.hackedMechChance = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.hackedMechChance, 0, 100, false, "", "0", "100");
                listing.Gap(6f);

                listing.Label("WTH_MinHackedMechPoints_Title".Translate() + ": " + Settings.minHackedMechPoints, tooltip: "WTH_MinHackedMechPoints_Description".Translate());
                listing.Gap(4f);
                Settings.minHackedMechPoints = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.minHackedMechPoints, 0, 500, false, "", "0", "500");
                listing.Gap(6f);

                listing.Label("WTH_MaxHackedMechPoints_Title".Translate() + ": " + Settings.maxHackedMechPoints, tooltip: "WTH_MaxHackedMechPoints_Description".Translate());
                listing.Gap(4f);
                Settings.maxHackedMechPoints = (int)Widgets.HorizontalSlider(listing.GetRect(24f), Settings.maxHackedMechPoints, 0, 500, false, "", "0", "500");
                break;

            case SelectedTab.Hack:
                AddFailureSlider("WTH_FailureChance_Nothing_Title", "WTH_FailureChance_Nothing_Description", ref Settings.failureChanceNothing);
                AddFailureSlider("WTH_FailureChance_CauseRaid_Title", "WTH_FailureChance_CauseRaid_Description", ref Settings.failureChanceCauseRaid);
                AddFailureSlider("WTH_FailureChance_ShootRandomDirection_Title", "WTH_FailureChance_ShootRandomDirection_Description", ref Settings.failureChanceShootRandomDirection);
                AddFailureSlider("WTH_FailureChance_HealToStanding_Title", "WTH_FailureChance_HealToStanding_Description", ref Settings.failureChanceHealToStanding);
                AddFailureSlider("WTH_FailureChance_HackPoorly_Title", "WTH_FailureChance_HackPoorly_Description", ref Settings.failureChanceHackPoorly);
                AddFailureSlider("WTH_FailureChance_IntRaidTooLarge_Title", "WTH_FailureChance_IntRaidTooLarge_Description", ref Settings.failureChanceIntRaidTooLarge);
                break;

            case SelectedTab.Balance:
                listing.CheckboxLabeled("WTH_MaintenanceDedayEnabled_Title".Translate(), ref Settings.maintenanceDecayEnabled, "WTH_MaintenanceDedayEnabled_Description".Translate());
                listing.Gap(6f);

                if (Settings.maintenanceDecayEnabled)
                {
                    listing.Label("WTH_MaintenanceDedayModifier_Title".Translate() + ": " + Settings.maintenanceDecayModifier.ToString("0.##"),
                        tooltip: "WTH_MaintenanceDedayModifier_Description".Translate());
                    listing.Gap(4f);
                    Settings.maintenanceDecayModifier = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.maintenanceDecayModifier, 0f, 2f, false, "", "0", "2");
                    listing.Gap(6f);
                }

                AddBalanceSlider("WTH_RepairConsumptionModifier_Title", "WTH_RepairConsumptionModifier_Description", ref Settings.repairConsumptionModifier, 0.05f, 2f, "0.05", "2");
                AddBalanceSlider("WTH_PartDropRateModifier_Title", "WTH_PartDropRateModifier_Description", ref Settings.partDropRateModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_ChipDropRateModifier_Title", "WTH_ChipDropRateModifier_Description", ref Settings.chipDropRateModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_PowerFallModifier_Title", "WTH_PowerFallModifier_Description", ref Settings.powerFallModifier, 0.05f, 5f, "0.05", "5");
                AddBalanceSlider("WTH_PowerChargeModifier_Title", "WTH_PowerChargeModifier_Description", ref Settings.powerChargeModifier, 0.05f, 5f, "0.05", "5");

                listing.Label("WTH_DeathOnDownedChance_Title".Translate() + ": " + Settings.deathOnDownedChance.ToString("0.##") + "%",
                    tooltip: "WTH_DeathOnDownedChance_Description".Translate());
                listing.Gap(4f);
                Settings.deathOnDownedChance = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.deathOnDownedChance, 0f, 100f, false, "", "0", "100");
                listing.Gap(6f);

                listing.Label("WTH_DownedOnDeathThresholdChance_Title".Translate() + ": " + Settings.downedOnDeathThresholdChance.ToString("0.##") + "%",
                    tooltip: "WTH_DownedOnDeathThresholdChance_Description".Translate());
                listing.Gap(4f);
                Settings.downedOnDeathThresholdChance = Widgets.HorizontalSlider(listing.GetRect(24f), Settings.downedOnDeathThresholdChance, 0f, 100f, false, "", "0", "100");
                break;
            
            case SelectedTab.Faction:
                editFactionRestrictions ??= new Dialog_EditFactionRestrictions(
                    Settings.FactionRestrictions,
                    Base.allMechs,
                    Base.allFactionNames,
                    () => Settings.tabsHandler,
                    s => Settings.tabsHandler = s);
                
                editFactionRestrictions.DoWindowContents(listing.GetRect(body.height - listing.CurHeight));
                break;
            
            default:
                listing.Label("Hi");
                break;
        }
        listing.End();
        return;

        void AddFailureSlider(string labelKey, string descKey, ref int value)
        {
            listing.Label(labelKey.Translate() + ": " + value + "%", tooltip: descKey.Translate());
            listing.Gap(4f);
            value = (int)Widgets.HorizontalSlider(listing.GetRect(24f), value, 0, 100, false, "", "0", "100");
            listing.Gap(6f);
        }
        
        void AddBalanceSlider(string labelKey, string descKey, ref float value, float min, float max, string minLabel, string maxLabel)
        {
            listing.Label(labelKey.Translate() + ": " + value.ToString("0.##"), tooltip: descKey.Translate());
            listing.Gap(4f);
            value = Widgets.HorizontalSlider(listing.GetRect(24f), value, min, max, false, "", minLabel, maxLabel);
            listing.Gap(6f);
        }
    }

    public override string TabTitle(SelectedTab selectedTab)
    {
        return selectedTab switch
        {
            SelectedTab.Raids => "WTH_SettingsGroup_Raids_Title".Translate(),
            SelectedTab.Hack => "WTH_SettingsGroup_HackFailure_Title".Translate(),
            SelectedTab.Balance => "WTH_SettingsGroup_Balance_Title".Translate(),
            SelectedTab.Faction => "WTH_FactionRestrictions_Title".Translate(),
            _ => selectedTab.ToString()
        };
    }
    

}