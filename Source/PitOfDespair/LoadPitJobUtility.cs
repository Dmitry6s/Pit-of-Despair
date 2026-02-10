using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PitOfDespair { 

public static class LoadPitJobUtility
{
    private static readonly HashSet<Thing> neededThings = new HashSet<Thing>();

    private static readonly Dictionary<TransferableOneWay, int> tmpAlreadyLoading =
        new Dictionary<TransferableOneWay, int>();

    public static bool HasJobOnTransporter(Pawn pawn, CompPit transporter)
    {
        return transporter.AnythingLeftToLoad && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
               pawn.CanReach((LocalTargetInfo)transporter.parent, PathEndMode.Touch, pawn.NormalMaxDanger()) &&
               FindThingToLoad(pawn, transporter).Thing != null;
    }

    public static Job JobOnTransporter(Pawn p, CompPit transporter)
    {
        var thingCount = FindThingToLoad(p, transporter);
        return new Job(DefDatabase<JobDef>.GetNamed("PD_HaulToPit"), thingCount.Thing, transporter.parent)
        {
            count = thingCount.Count,
            ignoreForbidden = true
        };
    }

    public static ThingCount FindThingToLoad(Pawn p, CompPit transporter)
    {
        neededThings.Clear();
        var leftToLoad = transporter.leftToLoad;
        tmpAlreadyLoading.Clear();
        if (leftToLoad != null)
        {
            var allPawnsSpawned = transporter.Map.mapPawns.AllPawnsSpawned;
            foreach (var pawn in allPawnsSpawned)
            {
                if (pawn == p ||
                    pawn.CurJobDef != DefDatabase<JobDef>.GetNamed("PD_HaulToPit"))
                {
                    continue;
                }

                var jobDriver_HaulToPit = (JobDriver_HaulToPit)pawn.jobs.curDriver;
                if (jobDriver_HaulToPit.Container != transporter.parent)
                {
                    continue;
                }

                var transferableOneWay = TransferableUtility.TransferableMatchingDesperate(
                    jobDriver_HaulToPit.ThingToCarry, leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
                if (transferableOneWay == null)
                {
                    continue;
                }

                if (tmpAlreadyLoading.TryGetValue(transferableOneWay, out var value))
                {
                    tmpAlreadyLoading[transferableOneWay] = value + jobDriver_HaulToPit.initialCount;
                }
                else
                {
                    tmpAlreadyLoading.Add(transferableOneWay, jobDriver_HaulToPit.initialCount);
                }
            }

            foreach (var oneWay in leftToLoad)
            {
                if (!tmpAlreadyLoading.TryGetValue(oneWay, out var value2))
                {
                    value2 = 0;
                }

                if (oneWay.CountToTransfer - value2 <= 0)
                {
                    continue;
                }

                foreach (var item in oneWay.things)
                {
                    neededThings.Add(item);
                }
            }
        }

        if (!neededThings.Any())
        {
            tmpAlreadyLoading.Clear();
            return default;
        }

        var thing = GenClosest.ClosestThingReachable(p.Position, p.Map,
            ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.Touch, TraverseParms.For(p), 9999f,
            x => neededThings.Contains(x) && p.CanReserve(x));
        if (thing == null)
        {
            foreach (var neededThing in neededThings)
            {
                if (neededThing is not Pawn { IsPrisoner: true } pawn ||
                    !p.CanReserveAndReach(pawn, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                neededThings.Clear();
                tmpAlreadyLoading.Clear();
                return new ThingCount(pawn, 1);
            }
        }

        neededThings.Clear();
        if (thing != null)
        {
            TransferableOneWay transferableOneWay3 = null;
            if (leftToLoad != null)
            {
                foreach (var oneWay in leftToLoad)
                {
                    if (!oneWay.things.Contains(thing))
                    {
                        continue;
                    }

                    transferableOneWay3 = oneWay;
                    break;
                }
            }

            var value3 = 0;
            if (transferableOneWay3 != null && !tmpAlreadyLoading.TryGetValue(transferableOneWay3, out value3))
            {
                value3 = 0;
            }

            tmpAlreadyLoading.Clear();
            if (transferableOneWay3 != null)
            {
                return new ThingCount(thing, Mathf.Min(transferableOneWay3.CountToTransfer - value3, thing.stackCount));
            }
        }

        tmpAlreadyLoading.Clear();
        return default;
    }
} }