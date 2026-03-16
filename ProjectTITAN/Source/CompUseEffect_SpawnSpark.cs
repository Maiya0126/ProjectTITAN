using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace ProjectTITAN
{
    public class CompUseEffect_SpawnSpark : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            // 1. 唯一性检测
            IEnumerable<Pawn> mapPawns = PawnsFinder.AllMaps_Spawned;
            IEnumerable<Pawn> worldPawns = Find.World.worldPawns.AllPawnsAlive;

            bool alreadyExists = mapPawns.Concat(worldPawns)
                .Any(p => p.Faction == Faction.OfPlayer &&
                          p.kindDef != null &&
                          p.kindDef.defName == "TITAN_Spark" &&
                          !p.Dead);

            if (alreadyExists)
            {
                Messages.Message("Message_SpawnSpark_AlreadyHas".Translate(), usedBy, MessageTypeDefOf.RejectInput, false);
                return;
            }

            // 2. 召唤逻辑
            try
            {
                Map map = usedBy.Map;
                if (map == null) return;

                IntVec3 spawnLoc;
                bool foundEdge = RCellFinder.TryFindRandomPawnEntryCell(out spawnLoc, map, CellFinder.EdgeRoadChance_Animal);
                if (!foundEdge) spawnLoc = usedBy.Position;

                PawnKindDef pawnKind = PawnKindDef.Named("TITAN_Spark");

                // 【修复】随机年龄 3-5 岁
                float randomAge = Rand.Range(3f, 5f);

                PawnGenerationRequest request = new PawnGenerationRequest(
                    kind: pawnKind,
                    faction: Faction.OfPlayer,
                    context: PawnGenerationContext.NonPlayer,
                    tile: -1,
                    forceGenerateNewPawn: true,
                    fixedBiologicalAge: randomAge, // 应用年龄
                    fixedGender: Gender.Male
                );

                Pawn newPawn = PawnGenerator.GeneratePawn(request);
                newPawn.Name = new NameTriple("花火", "花火", "Spark");

                GenSpawn.Spawn(newPawn, spawnLoc, map, WipeMode.Vanish);
                this.parent.Destroy();

                // 【修复】修正信件文案，符合鳞片设定
                string title = "不灭的火种";
                string text = "随着鳞片散发出特殊的费洛蒙信号，一只浑身散发着高热的变异爆炸鼠冲进了地图。\n\n它就是“花火”，0号原型体的挚友。它闻到了那熟悉的、只有它能识别的气味，义无反顾地追随而来。\n\n(已获得：花火 - 能够无限重生的爆炸专家)";

                Find.LetterStack.ReceiveLetter(title, text, LetterDefOf.PositiveEvent, newPawn);
                Find.CameraDriver.JumpToCurrentMapLoc(spawnLoc);
            }
            catch (Exception ex)
            {
                Log.Error("召唤花火出错: " + ex.ToString());
            }
        }
    }
}