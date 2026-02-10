using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace PitOfDespair { 

public static class PitUtility
{
    public static void GetTransportersInGroup(int transportersGroup, Map map, List<CompPit> outTransporters)
    {
        outTransporters.Clear();
        if (transportersGroup < 0)
        {
            return;
        }

        var list = map.listerThings.ThingsOfDef(ThingDef.Named("PD_PitOfDespair"));
        foreach (var thing in list)
        {
            var compPit = thing.TryGetComp<CompPit>();
            if (compPit.groupID == transportersGroup)
            {
                outTransporters.Add(compPit);
            }
        }
    }

    public static Lord FindLord(int transportersGroup, Map map)
    {
        var lords = map.lordManager.lords;
        foreach (var lord in lords)
        {
            if (lord.LordJob is LordJob_LoadAndEnterPit lordJob_LoadAndEnterPit &&
                lordJob_LoadAndEnterPit.transportersGroup == transportersGroup)
            {
                return lord;
            }
        }

        return null;
    }

    public static bool WasLoadingCanceled(Thing transporter)
    {
        var compPit = transporter.TryGetComp<CompPit>();
        return compPit is { LoadingInProgressOrReadyToLaunch: false };
    }
} }