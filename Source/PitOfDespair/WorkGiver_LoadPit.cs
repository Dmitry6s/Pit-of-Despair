using RimWorld;
using Verse;
using Verse.AI;

namespace PitOfDespair{

public class WorkGiver_LoadPit : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("PD_PitOfDespair"));

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn)
    {
        return Danger.Deadly;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var transporter = t.TryGetComp<CompPit>();
        return LoadPitJobUtility.HasJobOnTransporter(pawn, transporter);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var transporter = t.TryGetComp<CompPit>();
        return LoadPitJobUtility.JobOnTransporter(pawn, transporter);
    }
} }