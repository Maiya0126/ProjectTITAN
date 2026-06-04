using System;
using System.Collections.Generic;
using System.Linq; // 【关键】必须引用这个才能用 .Any()
using UnityEngine;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class IncidentWorker_TitanHunterAttack : IncidentWorker
    {
        // 判定事件是否可以触发
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // 1. 执行原版的基础检查 (天气、难度等)
            if (!base.CanFireNowSub(parms)) return false;

            // 2. 【核心修复】移除报错的旧代码，改为检测“0号原型体”是否存在
            // 逻辑：只有当玩家拥有 0 号原型体 (TITAN_ThrumboPrototype) 时，猎杀者才会出现
            bool hasZero = map.mapPawns.AllPawnsSpawned.Any(p =>
                p.Faction == Faction.OfPlayer &&
                p.kindDef.defName == "TITAN_ThrumboPrototype" &&
                !p.Dead);

            return hasZero;
        }

        // 执行事件逻辑
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // 1. 寻找生成点 (地图边缘)
            IntVec3 spawnCenter;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCenter, map, CellFinder.EdgeRoadChance_Animal))
            {
                return false;
            }

            // 2. 确定怪物种类
            PawnKindDef hunterDef = PawnKindDef.Named("TITAN_Hunter");

            // 3. 计算数量
            float points = parms.points;
            if (points <= 0) points = StorytellerUtility.DefaultThreatPointsNow(map);

            // 计算数量 (点数 / 战斗力)，至少生成1只，上限20只
            int count = Mathf.Max(1, GenMath.RoundRandom(points / hunterDef.combatPower));
            if (count > 20) count = 20;

            // 4. 生成怪物
            List<Pawn> hunters = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCenter, map, 5, null);
                Pawn hunter = PawnGenerator.GeneratePawn(hunterDef, null);
                GenSpawn.Spawn(hunter, loc, map, Rot4.Random);

                // 【关键】赋予永久猎杀人类状态 (红名敌对)
                hunter.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent, null, true, false, false);

                hunters.Add(hunter);
            }

            // 5. 发送事件信件
            string label = "泰坦计划：猎杀协议";
            string text = "帝国的侦测器锁定了0号原型体的能量信号。\n\n一群基因改造的“猎杀者”已经抵达该地区。它们的脑中被植入了绝对指令，会撕碎阻挡在它们和目标之间的一切生物。\n\n保护0号！";

            Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.ThreatBig, hunters, null, null);
            TitanEvents.FireTitanEventTriggered(def.defName);

            return true;
        }
    }
}