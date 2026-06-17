using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI.Group;

namespace ProjectTITAN
{
    public enum ExperimentArrivalType
    {
        ManhunterSelfTame,
        ColdHunter,
        GeneticDefect,
        FireRescue,
        StubbornWanderer,
        PirateHunt,
        FireFlee,
        CalmWalkIn
    }

    public static class ExperimentArrivalMapper
    {
        public static readonly Dictionary<string, ExperimentArrivalType> ArrivalTypes = new Dictionary<string, ExperimentArrivalType>
        {
            { "TITAN_No7_AcidThrumbo", ExperimentArrivalType.ManhunterSelfTame },
            { "TITAN_No13_SwampThrumbo", ExperimentArrivalType.ColdHunter },
            { "TITAN_No26_ToyThrumbo", ExperimentArrivalType.CalmWalkIn },
            { "TITAN_No42_AuroraThrumbo", ExperimentArrivalType.GeneticDefect },
            { "TITAN_No45_FireThrumbo", ExperimentArrivalType.FireRescue },
            { "TITAN_No50_DesertThrumbo", ExperimentArrivalType.StubbornWanderer },
            { "TITAN_No64_PrairieThrumbo", ExperimentArrivalType.PirateHunt },
            { "TITAN_No88_JungleThrumbo", ExperimentArrivalType.FireFlee },
        };

        public static readonly Dictionary<string, string> ArrivalIncidentDefs = new Dictionary<string, string>
        {
            { "TITAN_No7_AcidThrumbo", "TITAN_Incident_No7" },
            { "TITAN_No13_SwampThrumbo", "TITAN_Incident_No13" },
            { "TITAN_No26_ToyThrumbo", "TITAN_Incident_No26_FirstEncounter" },
            { "TITAN_No42_AuroraThrumbo", "TITAN_Incident_No42" },
            { "TITAN_No45_FireThrumbo", "TITAN_Incident_No45" },
            { "TITAN_No50_DesertThrumbo", "TITAN_Incident_No50" },
            { "TITAN_No64_PrairieThrumbo", "TITAN_Incident_No64" },
            { "TITAN_No88_JungleThrumbo", "TITAN_Incident_No88" },
        };
    }

    public abstract class IncidentWorker_ExperimentBase : IncidentWorker
    {
        protected abstract string PawnKindDefName { get; }
        protected abstract string CodexDefName { get; }
        protected virtual float JuvenileAge => 0.8f;
        protected virtual float YoungAdultAge => 2.2f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            if (!TitanUtils.HasPrototype0(map)) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null) return false;
            return !tracker.IsDiscovered(CodexDefName);
        }

        protected Pawn SpawnSubject(Map map, Faction faction = null, float? fixedAge = null)
        {
            IntVec3 spawnCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCell, map, CellFinder.EdgeRoadChance_Animal))
                return null;
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(PawnKindDefName);
            if (kind == null) return null;
            TitanPawnGuard.BeginAllowed();
            PawnGenerationRequest request = new PawnGenerationRequest(kind, faction,
                fixedBiologicalAge: fixedAge, fixedChronologicalAge: fixedAge,
                developmentalStages: DevelopmentalStage.Adult, allowDowned: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            TitanPawnGuard.EndAllowed();
            pawn.Name = new NameSingle(kind.label);
            GenSpawn.Spawn(pawn, spawnCell, map, Rot4.Random);
            return pawn;
        }

        protected bool TrySpawnCompanion(Pawn subject, Map map, Faction faction = null)
        {
            if (!TITAN_CodexMod.IsNewThrumboLoaded()) return false;
            var codexDef = DefDatabase<CodexEntryDef>.GetNamedSilentFail(CodexDefName);
            if (codexDef == null || string.IsNullOrEmpty(codexDef.companionPawnKindDef)) return false;
            if (!Rand.Chance(TITAN_CodexMod.Settings?.companionChance ?? 0.5f)) return false;
            PawnKindDef compKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(codexDef.companionPawnKindDef);
            if (compKind == null) return false;
            Faction compFaction = faction ?? subject.Faction;
            float companionAge = subject.ageTracker.AgeBiologicalYearsFloat;
            PawnGenerationRequest compReq = new PawnGenerationRequest(compKind, compFaction,
                fixedBiologicalAge: companionAge, fixedChronologicalAge: companionAge,
                developmentalStages: DevelopmentalStage.Adult);
            Pawn companion = PawnGenerator.GeneratePawn(compReq);
            companion.gender = subject.gender == Gender.Female ? Gender.Male : Gender.Female;
            GenSpawn.Spawn(companion, subject.Position, map, Rot4.Random);
            return true;
        }

        protected void DiscoverAndNotify(string letterTitleKey, string letterDescKey, Pawn lookTarget, LetterDef letterType, bool pause = false)
        {
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker != null) tracker.Discover(CodexDefName);
            TitanEvents.FireTitanEventTriggered(def.defName);
            if (!string.IsNullOrEmpty(PawnKindDefName))
                TitanEvents.FireSubjectJoinedColony(PawnKindDefName);
            Find.LetterStack.ReceiveLetter(
                letterTitleKey.Translate(),
                letterDescKey.Translate(),
                letterType,
                lookTarget
            );
            if (pause)
                Find.TickManager.Pause();
        }
    }

    public class IncidentWorker_No7Manhunter : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No7_AcidThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No7";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no7 = SpawnSubject(map, null, JuvenileAge);
            if (no7 == null) return false;

            no7.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true);
            LordMaker.MakeNewLord(null, new LordJob_AssaultColony(null, canTimeoutOrFlee: false), map, new List<Pawn> { no7 });

            var comp = no7.GetComp<CompSelfTameOnThreshold>();
            if (comp != null) comp.enabled = true;

            TrySpawnCompanion(no7, map);

            DiscoverAndNotify("TITAN_Letter_No7_Title", "TITAN_Letter_No7_Desc", no7, LetterDefOf.ThreatBig, true);
            return true;
        }
    }

    public class IncidentWorker_No13ColdHunter : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No13_SwampThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No13";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no13 = SpawnSubject(map, null, JuvenileAge);
            if (no13 == null) return false;

            for (int i = 0; i < 3; i++)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, 5, 0, -1, null, null, null);
                no13.TakeDamage(dinfo);
            }

            Pawn prototype0 = map.mapPawns.SpawnedColonyAnimals.FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype");
            IntVec3 targetPos = prototype0 != null ? prototype0.Position : map.Center;
            no13.SetFaction(Faction.OfPlayer);
            LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no13 });

            TrySpawnCompanion(no13, map);

            DiscoverAndNotify("TITAN_Letter_No13_Title", "TITAN_Letter_No13_Desc", no13, LetterDefOf.PositiveEvent, true);
            return true;
        }
    }

    public class IncidentWorker_No42GeneticDefect : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No42_AuroraThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No42";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no42 = SpawnSubject(map, null, JuvenileAge);
            if (no42 == null) return false;

            HediffDef driftDef = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_GeneticDrift");
            if (driftDef != null)
            {
                Hediff drift = no42.health.AddHediff(driftDef);
                drift.Severity = 0.6f;
            }

            IntVec3 targetPos = map.mapPawns.SpawnedColonyAnimals
                .FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype")?.Position ?? map.Center;
            LordMaker.MakeNewLord(null, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no42 });

            TrySpawnCompanion(no42, map);

            DiscoverAndNotify("TITAN_Letter_No42_Title", "TITAN_Letter_No42_Desc", no42, LetterDefOf.NeutralEvent, true);
            return true;
        }
    }

    public class IncidentWorker_No45FireRescue : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No45_FireThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No45";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no45 = SpawnSubject(map, Faction.OfPlayer, JuvenileAge);
            if (no45 == null) return false;

            FireUtility.TryStartFireIn(no45.Position, map, 0.5f, null);
            for (int i = 0; i < 4; i++)
            {
                IntVec3 nearby = CellFinder.RandomClosewalkCellNear(no45.Position, map, 5, null);
                FireUtility.TryStartFireIn(nearby, map, 0.3f + Rand.Value * 0.5f, null);
            }

            IntVec3 targetPos = map.mapPawns.SpawnedColonyAnimals
                .FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype")?.Position ?? map.Center;
            LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no45 });

            TrySpawnCompanion(no45, map);

            DiscoverAndNotify("TITAN_Letter_No45_Title", "TITAN_Letter_No45_Desc", no45, LetterDefOf.PositiveEvent, true);
            return true;
        }
    }

    public class IncidentWorker_No50StubbornWanderer : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No50_DesertThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No50";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no50 = SpawnSubject(map, Faction.OfPlayer, JuvenileAge);
            if (no50 == null) return false;

            Pawn prototype0 = map.mapPawns.SpawnedColonyAnimals.FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype");
            IntVec3 targetPos = prototype0 != null ? prototype0.Position : map.Center;
            LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no50 });

            TrySpawnCompanion(no50, map);

            DiscoverAndNotify("TITAN_Letter_No50_Title", "TITAN_Letter_No50_Desc", no50, LetterDefOf.NeutralEvent, true);
            return true;
        }
    }

    public class IncidentWorker_No64PirateHunt : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No64_PrairieThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No64";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no64 = SpawnSubject(map, null, JuvenileAge);
            if (no64 == null) return false;

            if (Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients) != null)
                no64.SetFaction(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients));

            IntVec3 targetPos = map.mapPawns.SpawnedColonyAnimals
                .FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype")?.Position ?? map.Center;
            LordMaker.MakeNewLord(no64.Faction, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no64 });

            Faction pirateFaction = Find.FactionManager.OfPirates;
            if (pirateFaction == null)
                pirateFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.HostileTo(Faction.OfPlayer) && !f.def.hidden && !f.defeated);
            if (pirateFaction == null)
                pirateFaction = Find.FactionManager.OfMechanoids;

            if (pirateFaction != null)
            {
                PawnGroupMakerParms groupParms = new PawnGroupMakerParms
                {
                    groupKind = PawnGroupKindDefOf.Combat,
                    tile = map.Tile,
                    faction = pirateFaction,
                    points = Math.Max(parms.points, 300f)
                };
                List<Pawn> enemies = PawnGroupMakerUtility.GeneratePawns(groupParms).ToList();
                IntVec3 enemySpawn;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out enemySpawn, map, CellFinder.EdgeRoadChance_Neutral))
                    enemySpawn = no64.Position;
                foreach (Pawn p in enemies) GenSpawn.Spawn(p, enemySpawn, map, Rot4.Random);
                LordMaker.MakeNewLord(pirateFaction, new LordJob_AssaultThings(pirateFaction, new List<Thing> { no64 }), map, enemies);
            }

            TrySpawnCompanion(no64, map);

            DiscoverAndNotify("TITAN_Letter_No64_Title", "TITAN_Letter_No64_Desc", no64, LetterDefOf.ThreatBig, true);
            return true;
        }
    }

    public class IncidentWorker_No88FireFlee : IncidentWorker_ExperimentBase
    {
        protected override string PawnKindDefName => "TITAN_No88_JungleThrumbo";
        protected override string CodexDefName => "TITAN_Codex_No88";

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || tracker.IsDiscovered(CodexDefName)) return false;

            Pawn no88 = SpawnSubject(map, Faction.OfPlayer, JuvenileAge);
            if (no88 == null) return false;

            HediffDef burnDef = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_No88_BurnTrauma");
            if (burnDef != null)
            {
                Hediff burn = no88.health.AddHediff(burnDef);
                burn.Severity = 0.4f;
            }
            for (int i = 0; i < 3; i++)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 8, 0, -1, null, null, null);
                no88.TakeDamage(dinfo);
            }

            IntVec3 waterCell = FindNearestWater(map, no88.Position);
            if (waterCell.IsValid) LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_DefendPoint(waterCell), map, new List<Pawn> { no88 });
            else
            {
                IntVec3 targetPos = map.mapPawns.SpawnedColonyAnimals
                    .FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype")?.Position ?? map.Center;
                LordMaker.MakeNewLord(Faction.OfPlayer, new LordJob_DefendPoint(targetPos), map, new List<Pawn> { no88 });
            }

            TrySpawnCompanion(no88, map);

            DiscoverAndNotify("TITAN_Letter_No88_Title", "TITAN_Letter_No88_Desc", no88, LetterDefOf.NeutralEvent, true);
            return true;
        }

        private IntVec3 FindNearestWater(Map map, IntVec3 from)
        {
            IntVec3 best = IntVec3.Invalid;
            float bestDist = float.MaxValue;
            int searchRadius = 40;
            CellRect rect = CellRect.CenteredOn(from, searchRadius);
            rect.ClipInsideMap(map);
            for (int z = rect.minZ; z <= rect.maxZ; z++)
            {
                for (int x = rect.minX; x <= rect.maxX; x++)
                {
                    IntVec3 c = new IntVec3(x, 0, z);
                    if (c.Walkable(map) && map.terrainGrid.TerrainAt(c).IsWater)
                    {
                        float dist = (c - from).LengthHorizontalSquared;
                        if (dist < bestDist) { bestDist = dist; best = c; }
                    }
                }
            }
            return best;
        }
    }
}