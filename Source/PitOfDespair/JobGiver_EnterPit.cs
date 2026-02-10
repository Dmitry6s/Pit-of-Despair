using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PitOfDespair { 

public class JobGiver_EnterPit : ThinkNode_JobGiver
{
    private static readonly List<CompPit> tmpTransporters = new List<CompPit>();

    protected override Job TryGiveJob(Pawn pawn)
    {
        var transportersGroup = pawn.mindState.duty.transportersGroup;
        var allPawnsSpawned = pawn.Map.mapPawns.AllPawnsSpawned;
        foreach (var pawn1 in allPawnsSpawned)
        {
            if (pawn1 == pawn || pawn1.CurJobDef != DefDatabase<JobDef>.GetNamed("PD_HaulToPit"))
            {
                continue;
            }

            var transporter = ((JobDriver_HaulToPit)pawn1.jobs.curDriver).Transporter;
            if (transporter != null && transporter.groupID == transportersGroup)
            {
                return null;
            }
        }

        PitUtility.GetTransportersInGroup(transportersGroup, pawn.Map, tmpTransporters);
        var compPit = FindMyTransporter(tmpTransporters, pawn);
        tmpTransporters.Clear();
        if (compPit == null || !pawn.CanReach((LocalTargetInfo)compPit.parent, PathEndMode.Touch, Danger.Deadly))
        {
            return null;
        }

        return new Job(DefDatabase<JobDef>.GetNamed("PD_EnterPit"), compPit.parent);
    }

    public static CompPit FindMyTransporter(List<CompPit> transporters, Pawn me)
    {
        foreach (var pit in transporters)
        {
            var leftToLoad = pit.leftToLoad;
            if (leftToLoad == null)
            {
                continue;
            }

            foreach (var oneWay in leftToLoad)
            {
                if (!(oneWay.AnyThing is Pawn))
                {
                    continue;
                }

                var things = oneWay.things;
                foreach (var thing in things)
                {
                    if (thing == me)
                    {
                        return pit;
                    }
                }
            }
        }

        return null;
    }
} }