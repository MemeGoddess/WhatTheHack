using Verse;

namespace WhatTheHack.Storage;

public class BackupBattery : Hediff
{
    public int canStartWorkThreshold = 50;
    public bool shouldAutoRecharge = true;
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref canStartWorkThreshold, "canStartWorkThreshold");
        Scribe_Values.Look(ref shouldAutoRecharge, "shouldAutoRecharge");
    }
}