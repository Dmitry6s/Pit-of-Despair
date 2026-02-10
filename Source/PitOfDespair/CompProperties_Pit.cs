using Verse;

namespace PitOfDespair { 

public class CompProperties_Pit : CompProperties
{
    public float massCapacity = 150f;

    public int maxPrisoners = 0;

    public float restEffectiveness;

    public CompProperties_Pit()
    {
        compClass = typeof(CompPit);
    }
}}