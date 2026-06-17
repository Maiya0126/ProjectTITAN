using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTITAN
{
    public class GameComponent_ThrumboHerdTracker : GameComponent
    {
        private int currentTier = 0;
        public int CurrentTier => currentTier;
        private int lastThrumboCount = -1;
        private bool needsReapply = true;
        public bool NeedsReapply => needsReapply;
        private static readonly int[] TierThresholds = { 10, 20, 50 };
        private static readonly string[] TierHediffs = { "TITAN_HerdBuff_Tier1", "TITAN_HerdBuff_Tier2", "TITAN_HerdBuff_Tier3" };
        private static readonly string[] TierThoughts = { "TITAN_HerdMood_Tier1", "TITAN_HerdMood_Tier2", "TITAN_HerdMood_Tier3" };
        private static readonly string[] TierLetters = { "TITAN_HerdAchievement_Tier1", "TITAN_HerdAchievement_Tier2", "TITAN_HerdAchievement_Tier3" };

        public GameComponent_ThrumboHerdTracker(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentTier, "currentTier", 0);
            Scribe_Values.Look(ref lastThrumboCount, "lastThrumboCount", -1);
            Scribe_Values.Look(ref needsReapply, "needsReapply", true);
        }

        public void TickHerd()
        {
            if (needsReapply && currentTier > 0)
            {
                needsReapply = false;
                ApplyNewBuffs(currentTier);
            }

            int count = CountColonyThrumbos();
            if (count != lastThrumboCount)
            {
                lastThrumboCount = count;

                int newTier = 0;
                for (int i = TierThresholds.Length - 1; i >= 0; i--)
                {
                    if (count >= TierThresholds[i]) { newTier = i + 1; break; }
                }

                if (newTier > currentTier)
                {
                    ApplyTierChange(currentTier, newTier);
                    currentTier = newTier;
                }
                else if (newTier < currentTier)
                {
                    RemoveOldBuffs(currentTier);
                    ApplyNewBuffs(newTier);
                    currentTier = newTier;
                }
            }

            if (currentTier > 0)
            {
                EnsureAllHaveBuff(currentTier);
            }
        }

        private int CountColonyThrumbos()
        {
            return Find.Maps
                .SelectMany(m => m.mapPawns.SpawnedColonyAnimals)
                .Count(p => TitanImplantUtils.IsThrumboKind(p));
        }

        private void ApplyTierChange(int oldTier, int newTier)
        {
            for (int i = 0; i < newTier; i++)
            {
                var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
                if (tracker != null && !tracker.IsTierAchieved(TierHediffs[i]))
                {
                    tracker.AchieveTier(TierHediffs[i]);
                    SendAchievementLetter(i);
                }
            }

            RemoveOldBuffs(oldTier);
            ApplyNewBuffs(newTier);
        }

        private void RemoveOldBuffs(int oldTier)
        {
            for (int i = 0; i < TierHediffs.Length; i++)
            {
                HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(TierHediffs[i]);
                ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(TierThoughts[i]);
                if (buffDef != null)
                {
                    foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.SpawnedColonyAnimals).Where(p => TitanImplantUtils.IsThrumboKind(p)))
                        RemoveHediff(pawn, buffDef);
                }
                if (thoughtDef != null)
                {
                    foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists))
                        RemoveMemoryThought(pawn, thoughtDef);
                }
            }
        }

        private void ApplyNewBuffs(int newTier)
        {
            if (newTier <= 0 || newTier > TierHediffs.Length) return;
            int idx = newTier - 1;

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(TierHediffs[idx]);
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(TierThoughts[idx]);

            if (buffDef != null)
            {
                foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.SpawnedColonyAnimals).Where(p => TitanImplantUtils.IsThrumboKind(p)))
                    pawn.health.GetOrAddHediff(buffDef).Severity = 1.0f;
            }
            if (thoughtDef != null)
            {
                foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                }
            }
        }

        private void RemoveHediff(Pawn pawn, HediffDef def)
        {
            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            if (hediff != null) pawn.health.RemoveHediff(hediff);
        }

        private void RemoveMemoryThought(Pawn pawn, ThoughtDef def)
        {
            var memory = pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(def);
            if (memory != null) pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
        }

        private void SendAchievementLetter(int tierIndex)
        {
            string titleKey = "TITAN_Letter_HerdAchievement_Title";
            string descKey = TierLetters[tierIndex];
            Find.LetterStack.ReceiveLetter(
                titleKey.Translate(),
                descKey.Translate(),
                LetterDefOf.PositiveEvent
            );
        }

        private void EnsureAllHaveBuff(int tier)
        {
            int idx = tier - 1;
            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(TierHediffs[idx]);
            if (buffDef != null)
            {
                foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.SpawnedColonyAnimals).Where(p => TitanImplantUtils.IsThrumboKind(p)))
                {
                    if (!pawn.health.hediffSet.HasHediff(buffDef))
                        pawn.health.GetOrAddHediff(buffDef).Severity = 1.0f;
                }
            }
            ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(TierThoughts[idx]);
            if (thoughtDef != null)
            {
                foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.FreeColonists))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(thoughtDef);
                }
            }
        }
    }
}