using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace PitOfDespair
{
    public class JobDriver_HaulToPit : JobDriver_HaulToContainer
    {
        public int initialCount;

        public CompPit Transporter => Container?.TryGetComp<CompPit>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref initialCount, "initialCount");
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Use standard reservation logic for TargetA (Thing) and TargetB (Pit)
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && 
                   pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            initialCount = job.count;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (var toil in base.MakeNewToils())
            {
                yield return toil;
            }
            
            yield return Toils_General.DoAtomic(delegate
            {
                if (Transporter != null && job.targetA.Thing != null)
                {
                    Transporter.Notify_ThingAdded(job.targetA.Thing);
                    
                    // Give thoughts to everyone in the faction when the prisoner is actually thrown in
                    foreach (var item in Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
                    {
                        if (item.needs?.mood?.thoughts == null)
                        {
                            continue;
                        }

                        item.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PD_ThrownPrisonerIntoPit"));
                        item.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("PD_ThrownPrisonerIntoPitImLovinIt"));
                    }
                }
            });
        }
    }
}
