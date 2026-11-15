using RimWorld;
using WhatTheHack;

namespace WTHAdditionalModules;
using static CompatibilityLoader;
[DefOf]
public static class IncidentDefOfMechQuest
{
	[MayRequire(AdditionalModules)]
	public static QuestScriptDef WTH_LongRangeMineralScannerMechParts;

	[MayRequire(AdditionalModules)]
	public static QuestScriptDef WTH_RoamingMechanoidsEncounterQuest;

	static IncidentDefOfMechQuest()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(IncidentDefOf));
	}
}
