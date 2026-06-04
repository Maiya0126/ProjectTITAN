using System;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class CompThrumboMagnet : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();

            Pawn p = this.parent as Pawn;
            if (p == null || p.Map == null || p.Dead || p.ageTracker.AgeBiologicalYearsFloat < 0.5f) return;

            if (p.IsHashIntervalTick(60000))
            {
                float attractChance = TITAN_CodexMod.Settings?.thrumboAttractChance ?? 0.2f;
                if (Rand.Chance(attractChance))
                {
                    TriggerThrumboEvent(p);
                }
            }
        }

        private void TriggerThrumboEvent(Pawn p)
        {
            Map map = p.Map;
            if (map == null) return;

            float alphaChance = TITAN_CodexMod.Settings?.alphaThrumboReplaceChance ?? 0.15f;
            bool odysseyActive = ModsConfig.IsActive("Ludeon.RimWorld.Odyssey");

            if (odysseyActive && Rand.Chance(alphaChance))
            {
                SpawnAlphaThrumbo(map, p);
            }
            else
            {
                IncidentDef def = IncidentDef.Named("ThrumboPasses");
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
                if (def.Worker.CanFireNow(parms))
                {
                    def.Worker.TryExecute(parms);
                    Messages.Message("Message_ThrumboMagnet_Attracted".Translate(), p, MessageTypeDefOf.PositiveEvent, false);
                }
            }
        }

        private void SpawnAlphaThrumbo(Map map, Pawn prototype)
        {
            PawnKindDef alphaKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("AlphaThrumbo");
            if (alphaKind == null) return;

            IntVec3 spawnCell;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnCell, map, CellFinder.EdgeRoadChance_Animal)) return;

            IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnCell, map, 10);
            Pawn alpha = PawnGenerator.GeneratePawn(alphaKind);
            GenSpawn.Spawn(alpha, loc, map, Rot4.Random);

            alpha.mindState.exitMapAfterTick = Find.TickManager.TicksGame + Rand.RangeInclusive(90000, 150000);

            IntVec3 dest = IntVec3.Invalid;
            if (RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(spawnCell, map, 10f, out dest))
            {
                alpha.mindState.forcedGotoPosition = CellFinder.RandomClosewalkCellNear(dest, map, 10);
            }

            Messages.Message("Message_ThrumboMagnet_AlphaAttracted".Translate(), prototype, MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
