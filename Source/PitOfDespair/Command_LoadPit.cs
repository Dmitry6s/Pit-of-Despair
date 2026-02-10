using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PitOfDespair{

[StaticConstructorOnStartup]
public class Command_LoadPit : Command
{
    private static HashSet<Building> tmpFuelingPortGivers = new HashSet<Building>();
    public CompPit transComp;

    private List<CompPit> transporters;

    public override void ProcessInput(Event ev)
    {
        base.ProcessInput(ev);
        if (transporters == null)
        {
            transporters = new List<CompPit>();
        }

        if (!transporters.Contains(transComp))
        {
            transporters.Add(transComp);
        }

        foreach (var pit in transporters)
        {
            if (pit != transComp && !transComp.Map.reachability.CanReach(transComp.parent.Position,
                    pit.parent, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)))
            {
                Messages.Message("MessageTransporterUnreachable".Translate(), pit.parent,
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            pit.prisonerthrowingdone = true;
        }

        Find.WindowStack.Add(new Dialog_LoadPit(transComp.Map, transporters));
    }

    public override bool InheritInteractionsFrom(Gizmo other)
    {
        var command_LoadPit = (Command_LoadPit)other;
        if (command_LoadPit.transComp.parent.def != transComp.parent.def)
        {
            return false;
        }

        if (transporters == null)
        {
            transporters = new List<CompPit>();
        }

        transporters.Add(command_LoadPit.transComp);
        return false;
    }
} }