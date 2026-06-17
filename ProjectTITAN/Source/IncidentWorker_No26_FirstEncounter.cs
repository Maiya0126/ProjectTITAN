using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTITAN
{
    public class IncidentWorker_No26FirstEncounter : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            if (!MapHasTamedPrototype(map)) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null) return false;
            return !tracker.IsDiscovered("TITAN_Codex_No26");
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered("TITAN_Codex_No26")) return false;

            IntVec3 spawnCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCell, map, CellFinder.EdgeRoadChance_Friendly))
                return false;

            PawnKindDef no26Kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("TITAN_No26_ToyThrumbo");
            if (no26Kind == null) return false;

            PawnGenerationRequest req = new PawnGenerationRequest(no26Kind, Faction.OfPlayer,
                fixedBiologicalAge: 0.5f, fixedChronologicalAge: 0.5f,
                developmentalStages: DevelopmentalStage.Adult, allowDowned: true);
            TitanPawnGuard.BeginAllowed();
            Pawn no26 = PawnGenerator.GeneratePawn(req);
            TitanPawnGuard.EndAllowed();
            no26.Name = new NameSingle(no26Kind.label);
            GenSpawn.Spawn(no26, spawnCell, map, Rot4.Random);

            bool hasCompanion = false;
            if (TITAN_CodexMod.IsNewThrumboLoaded() && Rand.Chance(TITAN_CodexMod.Settings?.companionChance ?? 0.5f))
            {
                PawnKindDef companionKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("NT_ToyThrumbo");
                if (companionKind != null)
                {
                    Pawn companion = PawnGenerator.GeneratePawn(companionKind, Faction.OfPlayer);
                    companion.gender = Gender.Male;
                    GenSpawn.Spawn(companion, spawnCell, map, Rot4.Random);
                    hasCompanion = true;
                }
            }

            if (hasCompanion && no26.ageTracker != null)
            {
                float ticks = 1.1f * 3600000f;
                no26.ageTracker.AgeBiologicalTicks = (long)ticks;
                no26.ageTracker.AgeChronologicalTicks = (long)ticks;
            }

            tracker.Discover("TITAN_Codex_No26");
            TitanEvents.FireTitanEventTriggered(def.defName);
            TitanEvents.FireSubjectJoinedColony("TITAN_No26_ToyThrumbo");

            Find.LetterStack.ReceiveLetter(
                "TITAN_Letter_No26_Title".Translate(),
                "TITAN_Letter_No26_Desc".Translate(),
                LetterDefOf.PositiveEvent,
                no26
            );

            return true;
        }

        private bool MapHasTamedPrototype(Map map)
        {
            return map.mapPawns.SpawnedColonyAnimals.Any(p => p.kindDef?.defName == "TITAN_ThrumboPrototype");
        }
    }
}