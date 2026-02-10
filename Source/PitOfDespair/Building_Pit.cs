using Verse;

namespace PitOfDespair { 

public class Building_Pit : Building
{
    public override Graphic Graphic
    {
        get
        {
            if (GetComp<CompPit>().buildingGod == "cthulhu")
            {
                return GraphicDatabase.Get(def.graphicData.graphicClass, "Things/Building/PD_PitOfDespairTentacled",
                    def.graphicData.shaderType.Shader, def.graphicData.drawSize, DrawColor, DrawColorTwo);
            }

            if (GetComp<CompPit>().buildingGod == "bast")
            {
                return GraphicDatabase.Get(def.graphicData.graphicClass, "Things/Building/PD_PitOfDespairCats",
                    def.graphicData.shaderType.Shader, def.graphicData.drawSize, DrawColor, DrawColorTwo);
            }

            if (GetComp<CompPit>().buildingGod == "bones")
            {
                return GraphicDatabase.Get(def.graphicData.graphicClass, "Things/Building/PD_PitOfDespairBones",
                    def.graphicData.shaderType.Shader, def.graphicData.drawSize, DrawColor, DrawColorTwo);
            }

            if (GetComp<CompPit>().buildingGod == "none")
            {
                return GraphicDatabase.Get(def.graphicData.graphicClass, "Things/Building/PD_PitOfDespair",
                    def.graphicData.shaderType.Shader, def.graphicData.drawSize, DrawColor, DrawColorTwo);
            }

            return base.Graphic;
        }
    }
} }