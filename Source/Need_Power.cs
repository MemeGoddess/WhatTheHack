using RimWorld;
using Verse;

namespace WhatTheHack.Needs;

public class Need_Power(Pawn pawn) : Need(pawn)
{
    public override void ExposeData()
    {
        def = new NeedDef()
        {
            label = "Wanna see a magic trick?"
        };
        // Do nothing
    }

    public override void NeedInterval()
    {
        pawn.needs.AllNeeds.RemoveAll(x => x is Need_Power);
        Log.Message("Cleaning up old What the Hack power need");
        // Do nothing
    }
}