using Verse;
using Verse.AI.Group;

namespace PitOfDespair { 

public class LordJob_LoadAndEnterPit : LordJob
{
    public int transportersGroup = -1;

    public LordJob_LoadAndEnterPit()
    {
    }

    public LordJob_LoadAndEnterPit(int transportersGroup)
    {
        this.transportersGroup = transportersGroup;
    }

    public override bool AllowStartNewGatherings => false;

    public override bool AddFleeToil => false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref transportersGroup, "transportersGroup");
    }

    public override StateGraph CreateGraph()
    {
        var stateGraph = new StateGraph();
        var unused =
            (LordToil_LoadAndEnterPit)(stateGraph.StartingToil = new LordToil_LoadAndEnterPit(transportersGroup));
        var toil = new LordToil_End();
        stateGraph.AddToil(toil);
        return stateGraph;
    }
} }