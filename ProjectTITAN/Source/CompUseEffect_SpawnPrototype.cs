using System;
using System.Collections.Generic;
using System.Linq; // 【关键】必须引用，用于拼接列表和筛选
using Verse;
using RimWorld;
using RimWorld.Planet; // 【关键】引用星球命名空间，用于查找世界生物
using UnityEngine;

namespace ProjectTITAN
{
    public class CompUseEffect_SpawnPrototype : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            // 1. 【唯一性检测 - 1.5版本通用修复版】
            // 既然 PawnsFinder 那个属性没了，我们就手动拼一个“全宇宙玩家生物名单”

            // 获取地图上所有活着的生物
            IEnumerable<Pawn> mapPawns = PawnsFinder.AllMaps_Spawned;

            // 获取世界中所有活着的生物 (包括远征队、胶囊里的、由于各种原因不在地图上的)
            IEnumerable<Pawn> worldPawns = Find.World.worldPawns.AllPawnsAlive;

            // 拼在一起 -> 筛选出属于玩家的 -> 筛选出是0号原型体的
            bool alreadyExists = mapPawns.Concat(worldPawns)
                .Any(p => p.Faction == Faction.OfPlayer &&
                          p.kindDef != null &&
                          p.kindDef.defName == "TITAN_ThrumboPrototype" &&
                          !p.Dead); // 双重保险，确保没死

            if (alreadyExists)
            {
                // 如果已存在，弹出拒绝提示
                Messages.Message("Message_SpawnPrototype_AlreadyExists".Translate(), usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 2. 正常召唤逻辑
            try
            {
                Map map = usedBy.Map;
                if (map == null) return;

                // --- 寻找生成位置 ---
                IntVec3 spawnLoc;
                bool foundEdge = RCellFinder.TryFindRandomPawnEntryCell(out spawnLoc, map, CellFinder.EdgeRoadChance_Animal);
                if (!foundEdge) spawnLoc = usedBy.Position;

                // --- 准备数据 ---
                PawnKindDef pawnKind = PawnKindDef.Named("TITAN_ThrumboPrototype");
                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: pawnKind,
                    faction: Faction.OfPlayer,
                    context: PawnGenerationContext.NonPlayer,
                    tile: -1,
                    forceGenerateNewPawn: true,
                    fixedBiologicalAge: 0.5f,
                    fixedGender: Gender.Female
                );

                // --- 生成并投放 ---
                Pawn newPawn = PawnGenerator.GeneratePawn(request);
                if (newPawn.Name is NameSingle ns && ns.Numerical)
                    newPawn.Name = new NameSingle("0号原型体·曙光");
                GenSpawn.Spawn(newPawn, spawnLoc, map, WipeMode.Vanish);

                // --- 添加状态 ---
                HediffDef dragonBloodDef = HediffDef.Named("TITAN_LatentDragonblood");
                if (dragonBloodDef != null)
                {
                    newPawn.health.AddHediff(dragonBloodDef);
                }

                // --- 销毁香薰 ---
                this.parent.Destroy();

                // --- 发信 ---
                string title = "迷雾中的回应";
                string text = "香薰燃尽，一股能够安抚狂暴基因的宁静气息飘向了荒野深处。\n\n不久后，地图边缘出现了一个金色的身影——那是帝国丢失的实验体“0号”。\n\n她看起来疲惫不堪，长期忍受着基因改造带来的剧痛。但此刻，你的香薰让她第一次感受到了久违的安宁。\n\n她怯生生地向殖民地走来，不是作为兵器，而是作为一个寻求庇护的流浪者。\n\n(已获得：【0号原型体】 - 她信任你，并加入了殖民地)";

                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, newPawn);
                Find.CameraDriver.JumpToCurrentMapLoc(spawnLoc);
            }
            catch (Exception ex)
            {
                Log.Error("【泰坦计划】生成逻辑异常: " + ex.ToString());
            }
        }
    }
}