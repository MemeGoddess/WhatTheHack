using RimWorld;
using UnityEngine;
using Verse;
using WhatTheHack.Needs;
using WhatTheHack.Storage;

namespace WhatTheHack;

[StaticConstructorOnStartup]
public class Command_SetWorkThreshold : Command
{
    public BackupBattery battery;
    private TaggedString _label = "WTH_SetCanStartWorkThreshold".Translate();

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);

        var window = new Dialog_Slider(TextGetter,
            0,
            100,
            value => battery.canStartWorkThreshold = value,
            battery.canStartWorkThreshold);
        Find.WindowStack.Add(window);
        return;

        string TextGetter(int x) => _label.Formatted(x);
    }
}