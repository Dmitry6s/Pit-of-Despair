using RimWorld;
using Verse;
using Verse.AI;

namespace PitOfDespair{

public class WorkGiver_FilteredRefuel : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ThingDef.Named("PD_PitOfDespair"));

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public virtual JobDef JobStandard => JobDefOf.Refuel;

    public virtual JobDef JobAtomic => JobDefOf.RefuelAtomic;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return FilteredRefuelWorkGiverUtility.CanRefuel(pawn, t, forced);
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return FilteredRefuelWorkGiverUtility.RefuelJob(pawn, t, forced, JobStandard, JobAtomic);
    }
} }