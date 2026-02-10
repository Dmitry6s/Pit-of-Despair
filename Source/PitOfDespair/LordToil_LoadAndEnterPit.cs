using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace PitOfDespair {

public class LordToil_LoadAndEnterPit : LordToil
{
    private readonly int transportersGroup;

    public LordToil_LoadAndEnterPit(int transportersGroup)
    {
        this.transportersGroup = transportersGroup;
    }

    public override bool AllowSatisfyLongNeeds => false;

    public override void UpdateAllDuties()
    {
        foreach (var pawn in lord.ownedPawns)
        {
            var pawnDuty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("PD_LoadAndEnterPit"))
            {
                transportersGroup = transportersGroup
            };
            pawn.mindState.duty = pawnDuty;
        }
    }
} }