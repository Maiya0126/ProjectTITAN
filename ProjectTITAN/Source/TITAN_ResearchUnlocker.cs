using System;
using System.Linq;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ProjectTITAN
{
    [HarmonyPatch]
    public static class TITAN_ResearchUnlocker
    {
        [HarmonyPatch(typeof(ResearchManager), "ReapplyAllMods")]
        public static void Postfix()
        {
            var plantTech = DefDatabase<ResearchProjectDef>.GetNamed("TITAN_PlantTech");
            if (plantTech != null && plantTech.IsFinished)
            {
                var plant = DefDatabase<ThingDef>.GetNamed("TITAN_Plant_TitanTuber");
                if (plant != null && plant.plant != null && plant.plant.sowResearchPrerequisites != null && plant.plant.sowResearchPrerequisites.Count > 0)
                {
                    plant.plant.sowResearchPrerequisites.Clear();
                }
            }
        }
    }
}