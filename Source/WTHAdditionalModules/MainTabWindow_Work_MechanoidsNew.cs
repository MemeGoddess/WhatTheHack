using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using WhatTheHack;

namespace WTHAdditionalModules;

internal class MainTabWindow_Work_MechanoidsNew : MainTabWindow_Work
{
	public override IEnumerable<Pawn> Pawns => from p in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
		where Extensions.IsHacked(p)
		select p;

	public override PawnTableDef PawnTableDef => PawnTableDefOf.Work;
}
