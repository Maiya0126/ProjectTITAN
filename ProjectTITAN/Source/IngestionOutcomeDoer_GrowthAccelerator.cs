using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTITAN
{
    public class IngestionOutcomeDoer_GrowthAccelerator : IngestionOutcomeDoer
    {
        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || ingested == null) return;

            float currentAge = pawn.ageTracker.AgeBiologicalYearsFloat;
            float adultMinAge = 2.0f;

            if (pawn.kindDef?.race?.race.lifeStageAges != null)
            {
                foreach (var stage in pawn.kindDef.race.race.lifeStageAges)
                {
                    if (stage.def.defName == "AnimalAdult")
                        adultMinAge = stage.minAge;
                }
            }

            if (currentAge >= adultMinAge)
            {
                Messages.Message(string.Format("TITAN_Message_GrowthPaste_AlreadyAdult".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent);
                return;
            }

            long growthTicks = (long)(0.5f * 3600000f);
            pawn.ageTracker.AgeBiologicalTicks += growthTicks;

            HediffDef growthHediff = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_RapidGrowth");
            if (growthHediff != null)
            {
                pawn.health.AddHediff(growthHediff);
            }

            Messages.Message(string.Format("TITAN_Message_GrowthPaste_Applied".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}