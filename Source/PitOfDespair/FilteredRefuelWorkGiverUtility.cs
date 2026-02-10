using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace PitOfDespair{

public static class FilteredRefuelWorkGiverUtility
{
    public static bool CanRefuel(Pawn pawn, Thing t, bool forced = false)
    {
        var compFilteredRefuelable = t.TryGetComp<CompFilteredRefuelable>();
        if (compFilteredRefuelable?.IsFull ?? true)
        {
            return false;
        }

        if (!forced && !compFilteredRefuelable.ShouldAutoRefuelNow)
        {
            return false;
        }

        if (t.IsForbidden(pawn))
        {
            return false;
        }

        LocalTargetInfo target = t;
        if (!pawn.CanReserve(target, 1, -1, null, forced))
        {
            return false;
        }

        if (t.Faction != pawn.Faction)
        {
            return false;
        }

        if (FindBestFuel(pawn, t) == null)
        {
            var fuelFilter = t.TryGetComp<CompFilteredRefuelable>().FuelFilter;
            JobFailReason.Is("PD_NoFood".Translate(fuelFilter.Summary));
            return false;
        }

        if (!t.TryGetComp<CompFilteredRefuelable>().Props.atomicFueling || FindAllFuel(pawn, t) != null)
        {
            return true;
        }

        var fuelFilter2 = t.TryGetComp<CompFilteredRefuelable>().FuelFilter;
        JobFailReason.Is("PD_NoFood".Translate(fuelFilter2.Summary));
        return false;
    }

    public static Job RefuelJob(Pawn pawn, Thing t, bool forced = false, JobDef customRefuelJob = null,
        JobDef customAtomicRefuelJob = null)
    {
        if (!t.TryGetComp<CompFilteredRefuelable>().Props.atomicFueling)
        {
            var thing = FindBestFuel(pawn, t);
            return new Job(customRefuelJob ?? JobDefOf.Refuel, t, thing);
        }

        var source = FindAllFuel(pawn, t);
        var job = new Job(customAtomicRefuelJob ?? JobDefOf.RefuelAtomic, t)
        {
            targetQueueB = source.Select(f => new LocalTargetInfo(f)).ToList()
        };
        return job;
    }

    private static Thing FindBestFuel(Pawn pawn, Thing refuelable)
    {
        var filter = refuelable.TryGetComp<CompFilteredRefuelable>().FuelFilter;

        bool Predicate(Thing x)
        {
            return !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
        }

        var position = pawn.Position;
        var map = pawn.Map;
        var bestThingRequest = filter.BestThingRequest;
        var peMode = PathEndMode.ClosestTouch;
        var traverseParams = TraverseParms.For(pawn);
        var validator = (Predicate<Thing>)Predicate;
        return GenClosest.ClosestThingReachable(position, map, bestThingRequest, peMode, traverseParams, 9999f,
            validator);
    }

    private static List<Thing> FindAllFuel(Pawn pawn, Thing refuelable)
    {
        var quantity = refuelable.TryGetComp<CompFilteredRefuelable>().GetFuelCountToFullyRefuel();
        var filter = refuelable.TryGetComp<CompFilteredRefuelable>().FuelFilter;

        bool Validator(Thing x)
        {
            return !x.IsForbidden(pawn) && pawn.CanReserve(x) && filter.Allows(x);
        }

        var position = refuelable.Position;
        var region = position.GetRegion(pawn.Map, RegionType.Normal | RegionType.Portal);
        var traverseParams = TraverseParms.For(pawn);

        bool EntryCondition(Region from, Region r)
        {
            return r.Allows(traverseParams, false);
        }

        var chosenThings = new List<Thing>();
        var accumulatedQuantity = 0;

        bool RegionProcessor(Region r)
        {
            var list = r.ListerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.HaulableEver));
            foreach (var thing in list)
            {
                if (!Validator(thing) || chosenThings.Contains(thing) ||
                    !ReachabilityWithinRegion.ThingFromRegionListerReachable(thing, r, PathEndMode.ClosestTouch,
                        pawn))
                {
                    continue;
                }

                chosenThings.Add(thing);
                accumulatedQuantity += thing.stackCount;
                if (accumulatedQuantity >= quantity)
                {
                    return true;
                }
            }

            return false;
        }

        RegionTraverser.BreadthFirstTraverse(region, EntryCondition, RegionProcessor, 99999,
            RegionType.Normal | RegionType.Portal);
        if (accumulatedQuantity >= quantity)
        {
            return chosenThings;
        }

        return null;
    }
} }