using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace WTHAdditionalModules;

public class CompSpawnerMechQuest : ThingComp
{
	private int ticksUntilSpawn;

	public CompProperties_MechQuest PropsSpawner => (CompProperties_MechQuest)props;

	private bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;

	public int RandomNumber(int min, int max)
	{
		return new Random().Next(min, max);
	}

	public override void PostSpawnSetup(bool respawningAfterLoad)
	{
		if (!respawningAfterLoad)
		{
			ResetCountdown();
		}
	}

	public override void CompTick()
	{
		TickInterval(1);
	}

	public override void CompTickRare()
	{
		TickInterval(250);
	}

	private void TickInterval(int interval)
	{
		if (!parent.Spawned)
		{
			return;
		}
		CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
		if (comp != null)
		{
			if (!comp.Awake)
			{
				return;
			}
		}
		else if (parent.Position.Fogged(parent.Map))
		{
			return;
		}
		if (!PropsSpawner.requiresPower || PowerOn)
		{
			ticksUntilSpawn -= interval;
			CheckShouldSpawn();
		}
	}

	private void CheckShouldSpawn()
	{
		if (ticksUntilSpawn <= 0)
		{
			TryDoSpawn();
			ResetCountdown();
		}
	}

	public void TryDoSpawn()
	{
		if (RandomNumber(1, 3) == 2)
		{
			Slate vars = new Slate();
			Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(IncidentDefOfMechQuest.WTH_LongRangeMineralScannerMechParts, vars);
			Find.LetterStack.ReceiveLetter((TaggedString)quest.name, quest.description, LetterDefOf.PositiveEvent, (LookTargets)null, (Faction)null, quest, (List<ThingDef>)null, (string)null);
		}
		else
		{
			Slate vars2 = new Slate();
			Quest quest2 = QuestUtility.GenerateQuestAndMakeAvailable(IncidentDefOfMechQuest.WTH_RoamingMechanoidsEncounterQuest, vars2);
			Find.LetterStack.ReceiveLetter((TaggedString)quest2.name, quest2.description, LetterDefOf.PositiveEvent, (LookTargets)null, (Faction)null, quest2, (List<ThingDef>)null, (string)null);
		}
	}

	private void ResetCountdown()
	{
		ticksUntilSpawn = PropsSpawner.spawnIntervalRange.RandomInRange;
	}

	public override void PostExposeData()
	{
		string text = (PropsSpawner.saveKeysPrefix.NullOrEmpty() ? null : (PropsSpawner.saveKeysPrefix + "_"));
		Scribe_Values.Look(ref ticksUntilSpawn, text + "ticksUntilSpawn", 0);
	}

	public override IEnumerable<Gizmo> CompGetGizmosExtra()
	{
		if (Prefs.DevMode)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEBUG: Spawn ",
				icon = TexCommand.DesirePower,
				action = delegate
				{
					TryDoSpawn();
					ResetCountdown();
				}
			};
		}
	}
}
