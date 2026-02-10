using RimWorld;
using Verse;

namespace PitOfDespair { 

public class CompFilteredRefuelable : CompRefuelable, IStoreSettingsParent
{
    // private CompFlickable flickComp; // Unused
    private bool hasPawnsNeedingFood;
    public StorageSettings inputSettings;

    private Building_Pit pit;

    public bool HasStarvingPawns => hasPawnsNeedingFood && !HasFuel;

    private float ConsumptionRatePerTick => Props.fuelConsumptionRate / 60000f;

    public ThingFilter FuelFilter => inputSettings.filter;

    public bool StorageTabVisible => true;

    public StorageSettings GetStoreSettings()
    {
        return inputSettings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return parent.def.building.fixedStorageSettings;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (inputSettings == null)
        {
            inputSettings = new StorageSettings(this);
            if (parent.def.building.defaultStorageSettings != null)
            {
                inputSettings.CopyFrom(parent.def.building.defaultStorageSettings);
            }
        }

        pit = (Building_Pit)parent;
    }

    public override string CompInspectStringExtra()
    {
        var text =
            $"{Props.FuelLabel}: {Fuel.ToStringDecimalIfSmall()} / {Props.fuelCapacity.ToStringDecimalIfSmall()}";
        if (!Props.consumeFuelOnlyWhenUsed && HasFuel)
        {
            var numTicks = (int)(Fuel / Props.fuelConsumptionRate * 60000f);
            text = $"{text} ({numTicks.ToStringTicksToPeriod()})";
        }

        if (!HasFuel && !Props.outOfFuelMessage.NullOrEmpty())
        {
            text +=
                $"\n{Props.outOfFuelMessage} ({GetFuelCountToFullyRefuel()}x {Props.fuelFilter.AnyAllowedDef.label})";
        }

        if (Props.targetFuelLevelConfigurable)
        {
            text = $"{text}\n" + "ConfiguredTargetFuelLevel".Translate(TargetFuelLevel.ToStringDecimalIfSmall());
        }

        if (!HasFuel && hasPawnsNeedingFood)
        {
            text = $"{text}\n" + "PD_NoFoodInThePit".Translate();
        }

        return text;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref inputSettings, "inputSettings");
    }

    public override void CompTick()
    {
        hasPawnsNeedingFood = false;
        foreach (var thing in pit.GetComp<CompPit>().innerContainer)
        {
            if (thing is not Pawn { Dead: false } pawn || pawn.needs.food == null)
            {
                continue;
            }

            hasPawnsNeedingFood = true;
            break;
        }

        if (!Props.consumeFuelOnlyWhenUsed && pit != null && hasPawnsNeedingFood)
        {
            ConsumeFuel(ConsumptionRatePerTick);
        }
    }
         
        public void Notify_SettingsChanged() { }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
    }
} }