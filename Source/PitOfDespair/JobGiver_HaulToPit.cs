using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PitOfDespair { 

public class JobGiver_HaulToPit : ThinkNode_JobGiver
{
    private static readonly List<CompPit> tmpTransporters = new List<CompPit>();

    protected override Job TryGiveJob(Pawn pawn)
    {
        var transportersGroup = pawn.mindState.duty.transportersGroup;
        PitUtility.GetTransportersInGroup(transportersGroup, pawn.Map, tmpTransporters);
        foreach (var transporter in tmpTransporters)
        {
            if (LoadPitJobUtility.HasJobOnTransporter(pawn, transporter))
            {
                return LoadPitJobUtility.JobOnTransporter(pawn, transporter);
            }
        }

        return null;
    }
} }