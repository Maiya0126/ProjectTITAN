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
        private int lastThrumboCount = -1;
        private static readonly int[] TierThresholds = { 10, 20, 50 };
        private static readonly string[] TierHediffs = { "TITAN_HerdBuff_Tier1", "TITAN_HerdBuff_Tier2", "TITAN_HerdBuff_Tier3" };
        private static readonly string[] TierThoughts = { "TITAN_HerdMood_Tier1", "TITAN_HerdMood_Tier2", "TITAN_HerdMood_Tier3" };
        private static readonly string[] TierLetters = { "TITAN_HerdAchievement_Tier1", "TITAN_HerdAchievement_Tier2", "TITAN_HerdAchievement_Tier3" };

        private static readonly HashSet<string> ThrumbokindRaces = new HashSet<string>
        {
            "Thrumbo",
            "TITAN_ThrumboPrototype", "TITAN_Spark",
            "TITAN_Warlord", "TITAN_VoidWalker", "TITAN_Matriarch",
            "TITAN_No7_AcidThrumbo", "TITAN_No13_SwampThrumbo",
            "TITAN_No26_ToyThrumbo", "TITAN_No42_AuroraThrumbo",
            "TITAN_No45_FireThrumbo", "TITAN_No50_DesertThrumbo",
            "TITAN_No64_PrairieThrumbo", "TITAN_No88_JungleThrumbo",
        };

        private static readonly HashSet<string> NTThrumbokindRaces = new HashSet<string>
        {
            "NT_ToyThrumbo", "NT_AcidThrumboRace", "NT_AuroraThrumboRace",
            "NT_DesertThrumbo", "NT_FireThrumboRace", "NT_JungleThrumbo",
            "NT_PrairieThrumbo", "NT_SwampThrumbo",
        };

        public GameComponent_ThrumboHerdTracker(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentTier, "currentTier", 0);
            Scribe_Values.Look(ref lastThrumboCount, "lastThrumboCount", -1);
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 != 0) return;

            int count = CountColonyThrumbos();
            if (count == lastThrumboCount) return;
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
        }

        private int CountColonyThrumbos()
        {
            return Find.Maps
                .SelectMany(m => m.mapPawns.SpawnedColonyAnimals)
                .Count(IsThrumbo);
        }

        private bool IsThrumbo(Pawn p)
        {
            string race = p.kindDef?.race?.defName ?? (p.kindDef?.defName ?? "");
            if (ThrumbokindRaces.Contains(race)) return true;
            if (TITAN_CodexMod.IsNewThrumboLoaded() && NTThrumbokindRaces.Contains(race)) return true;
            return false;
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
            for (int i = 0; i < oldTier && i < TierHediffs.Length; i++)
            {
                HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail(TierHediffs[i]);
                ThoughtDef thoughtDef = DefDatabase<ThoughtDef>.GetNamedSilentFail(TierThoughts[i]);
                if (buffDef != null)
                {
                    foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.SpawnedColonyAnimals).Where(IsThrumbo))
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
                foreach (var pawn in Find.Maps.SelectMany(m => m.mapPawns.SpawnedColonyAnimals).Where(IsThrumbo))
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
    }
}