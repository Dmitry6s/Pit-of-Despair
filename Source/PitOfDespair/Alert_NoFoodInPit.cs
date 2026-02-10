using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace PitOfDespair
{

    internal class Alert_NoFoodInPit : Alert
    {
        public Alert_NoFoodInPit()
        {
            defaultLabel = "PD_NoFoodAlertLabel".Translate();
            defaultExplanation = "PD_NoFoodInThePit".Translate();
            defaultPriority = AlertPriority.High;
        }

        protected override Color BGColor { get; } = new Color(1f, 0.9215686f, 0.01568628f, 0.35f);

        public override AlertReport GetReport()
        {
            var homeMap = Find.Maps.FirstOrDefault(map => map.IsPlayerHome);
            if (homeMap == null)
            {
                return false;
            }

            var pits = homeMap.listerBuildings.allBuildingsColonist.Where(building =>
                building.TryGetComp<CompFilteredRefuelable>()?.HasStarvingPawns == true);
            if (!pits.Any())
            {
                return false;
            }

            var report = new AlertReport { culpritsThings = new List<Thing>(), active = true };
            foreach (var building in pits)
            {
                report.culpritsThings.Add(building);
            }

            return report;
        }
    }
}