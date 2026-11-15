using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace WTHExCore.Module
{
    public class WorkGiver_HaulAnimal : WorkGiver_RescueDowned
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Pawn> pawnList = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int index = 0; index < pawnList.Count; ++index)
            {
                if (pawnList[index].Downed && !pawnList[index].InBed() && !pawnList[index].RaceProps.Animal)
                    return false;
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn targetPawn = t as Pawn;
            if (!targetPawn.RaceProps.Animal)
                return false;
            if (!(t is Pawn pawn1) || !pawn1.Downed || (pawn1.Faction != pawn.Faction || pawn1.InBed()) || (!pawn.CanReserve(pawn1, ignoreOtherReservations: forced) || GenAI.EnemyIsNear(pawn1, 40f)))
                return false;
            Thing bed = FindBed(pawn, pawn1);
            return bed != null && pawn1.CanReserve(bed);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn patient = t as Pawn;
            if (patient.RaceProps.Animal)
            {
                Thing bed = FindBed(pawn, patient);
                Job job = JobMaker.MakeJob(JobDefOf.Rescue, patient, bed);
                job.count = 1;
                return job;
            }

            return null;
        }
    }
}
