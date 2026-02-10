using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace PitOfDespair{

public class JobDriver_EnterPit : JobDriver
{
    private readonly TargetIndex TransporterInd = TargetIndex.A;

    public CompPit Transporter => job.GetTarget(TransporterInd).Thing?.TryGetComp<CompPit>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TransporterInd);
        this.FailOn(() => !Transporter.LoadingInProgressOrReadyToLaunch);
        yield return Toils_Goto.GotoThing(TransporterInd, PathEndMode.Touch);
        yield return new Toil
        {
            initAction = delegate
            {
                var transporter = Transporter;
                pawn.DeSpawn();
                transporter.GetDirectlyHeldThings().TryAdd(pawn);
            }
        };
    }
} }