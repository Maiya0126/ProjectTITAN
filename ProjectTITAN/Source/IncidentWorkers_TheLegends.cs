using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace ProjectTITAN
{
    // ===========================================
    // 1. 繁盛之母：坠落治疗
    // ===========================================
    public class IncidentWorker_MatriarchCrash : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return base.CanFireNowSub(parms) && TitanUtils.HasPrototype0(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnLoc;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnLoc, map, CellFinder.EdgeRoadChance_Animal)) return false;

            PawnKindDef kind = PawnKindDef.Named("TITAN_Matriarch");
            Pawn matriarch = PawnGenerator.GeneratePawn(kind, null);
            GenSpawn.Spawn(matriarch, spawnLoc, map, Rot4.Random);

            Hediff sickness = matriarch.health.AddHediff(HediffDef.Named("TITAN_GeneticCollapse"));
            sickness.Severity = 0.5f;

            // 让繁盛之母也走向殖民地
            IntVec3 colonyCenter = map.mapPawns.FreeColonistsSpawned.Any()
                ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position
                : map.Center;

            LordMaker.MakeNewLord(matriarch.Faction, new LordJob_DefendPoint(colonyCenter), map, new List<Pawn> { matriarch });

            SendStandardLetter(parms, new LookTargets(matriarch));
            return true;
        }
    }

    // ===========================================
    // 2. 战争之痕：红魔入侵
    // ===========================================
    public class IncidentWorker_WarlordRaid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return base.CanFireNowSub(parms) && TitanUtils.HasPrototype0(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            IntVec3 spawnCenter;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, map, CellFinder.EdgeRoadChance_Animal)) return false;

            Pawn warlord = PawnGenerator.GeneratePawn(PawnKindDef.Named("TITAN_Warlord"), null);
            GenSpawn.Spawn(warlord, spawnCenter, map, Rot4.Random);

            float points = parms.points;
            PawnKindDef minionKind = PawnKindDef.Named("Warg");
            int minionCount = Mathf.Max(3, (int)(points / 150f));

            List<Pawn> minions = new List<Pawn>();
            for (int i = 0; i < minionCount; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 5, null);
                Pawn minion = PawnGenerator.GeneratePawn(minionKind, null);
                GenSpawn.Spawn(minion, loc, map, Rot4.Random);
                minions.Add(minion);
            }

            List<Pawn> allAttackers = new List<Pawn>(minions) { warlord };

            LordJob_DefendPoint lordJob = new LordJob_DefendPoint(spawnCenter);
            LordMaker.MakeNewLord(null, lordJob, map, allAttackers);

            foreach (var pawn in allAttackers)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, false);
            }

            SendStandardLetter(parms, new LookTargets(warlord));
            return true;
        }
    }

    // ===========================================
    // 3. 虚空行者：帝国追杀 (含修复)
    // ===========================================
    public class IncidentWorker_VoidWalkerRescue : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return base.CanFireNowSub(parms) && TitanUtils.HasPrototype0(map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // A. 生成虚空行者
            IntVec3 walkerLoc;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out walkerLoc, map, CellFinder.EdgeRoadChance_Friendly)) return false;

            PawnKindDef walkerKind = PawnKindDef.Named("TITAN_VoidWalker");
            Pawn walker = PawnGenerator.GeneratePawn(walkerKind, null);
            GenSpawn.Spawn(walker, walkerLoc, map, Rot4.Random);

            if (Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients) != null)
                walker.SetFaction(Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients));

            // 【剧情受伤】给它加几刀，体现正在被追杀
            for (int i = 0; i < 4; i++)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, 8, 0, -1, null, null, null);
                walker.TakeDamage(dinfo);
            }

            // 让它逃向殖民地中心
            IntVec3 safeSpot = map.mapPawns.FreeColonistsSpawned.Any()
                ? map.mapPawns.FreeColonistsSpawned.RandomElement().Position
                : map.Center;
            LordMaker.MakeNewLord(walker.Faction, new LordJob_DefendPoint(safeSpot), map, new List<Pawn> { walker });

            // B. 生成敌对追兵
            IntVec3 enemyLoc;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out enemyLoc, map, CellFinder.EdgeRoadChance_Neutral)) enemyLoc = walkerLoc;

            Faction enemyFaction = Find.FactionManager.OfPirates;

            // 【修复 CS1061】使用 LINQ 查找敌对派系，替代不存在的 HostileFactions
            if (enemyFaction == null)
            {
                enemyFaction = Find.FactionManager.AllFactions
                    .Where(f => f.HostileTo(Faction.OfPlayer) && !f.def.hidden && !f.defeated)
                    .FirstOrDefault();
            }

            // 如果实在没找到敌对派系，就用机械族保底，防止崩溃
            if (enemyFaction == null) enemyFaction = Find.FactionManager.OfMechanoids;

            PawnGroupMakerParms groupParms = new PawnGroupMakerParms();
            groupParms.groupKind = PawnGroupKindDefOf.Combat;
            groupParms.tile = map.Tile;
            groupParms.faction = enemyFaction;
            groupParms.points = Math.Max(parms.points, 500f);

            List<Pawn> enemies = PawnGroupMakerUtility.GeneratePawns(groupParms).ToList();
            if (enemies.Count == 0) return false;

            foreach (Pawn p in enemies)
            {
                GenSpawn.Spawn(p, enemyLoc, map, Rot4.Random);
            }

            LordJob_AssaultThings lordJob = new LordJob_AssaultThings(enemyFaction, new List<Thing> { walker });
            LordMaker.MakeNewLord(enemyFaction, lordJob, map, enemies);

            string label = "虚空行者求救";
            string text = "一只受伤的“虚空行者”逃入了该区域，它身上布满了帝国制式武器造成的伤痕。\n\n侦测到一支【伪装成帝国部队的非法佣兵】正在追杀它。\n\n虚空行者正在向你的基地寻求庇护！保护它！";

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, new LookTargets(walker, enemies.FirstOrDefault()));
            return true;
        }
    }

// 4. 移动光源组件 (已禁用 - CompGlower 自动处理移动更新)
    public class CompMobileLight : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }
    }

    public static class TitanUtils
    {
        public static bool HasPrototype0(Map map)
        {
            return map.mapPawns.AllPawnsSpawned.Any(p =>
                p.Faction == Faction.OfPlayer &&
                p.kindDef.defName == "TITAN_ThrumboPrototype" &&
                !p.Dead);
        }
    }
}