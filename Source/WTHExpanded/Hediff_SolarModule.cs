using Verse;

namespace WTHExCore
{
    public class Hediff_SolarModule : HediffWithComps
	{
		public override HediffStage CurStage
		{
			get
			{
				if (!def.stages.NullOrEmpty())
				{
					if (pawn?.Map?.skyManager?.CurSkyGlow == null || pawn.Map.roofGrid == null) return def.stages[0];
					return (pawn.Map.skyManager.CurSkyGlow < 0.5f || pawn.Map.roofGrid.Roofed(pawn.Position)) ? def.stages[0] : def.stages[1];
				}
				return null;
			}
		}
	}
}
