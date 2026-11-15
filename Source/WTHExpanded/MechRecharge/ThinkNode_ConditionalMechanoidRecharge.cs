using RimWorld;
using Verse;
using Verse.AI;
using WhatTheHack;

namespace WTHExCore
{
    public class ThinkNode_ConditionalMechanoidRecharge : ThinkNode_Conditional
    {
        public override bool Satisfied(Pawn pawn) =>
            pawn.ShouldRecharge() && (pawn.CurrentBed() == null || pawn.CurrentBed().IsForbidden(pawn));
    }
}
