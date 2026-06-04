using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class CompProperties_HearthWarmth : CompProperties
    {
        public float range = 10f;
        public string hediffDefName = "TITAN_Bond_No45_HearthWarmth";

        public CompProperties_HearthWarmth()
        {
            this.compClass = typeof(CompHearthWarmth);
        }
    }

    public class CompHearthWarmth : ThingComp
    {
        public CompProperties_HearthWarmth Props => (CompProperties_HearthWarmth)this.props;
        private HashSet<int> warmedPawns = new HashSet<int>();

        public override void CompTickRare()
        {
            base.CompTickRare();
            Pawn me = this.parent as Pawn;
            if (me == null || me.Map == null || me.Dead || me.Downed) return;

            HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
            if (hediff == null) return;

            IReadOnlyList<Pawn> mapPawns = me.Map.mapPawns.AllPawnsSpawned;
            HashSet<int> currentInRange = new HashSet<int>();

            for (int i = 0; i < mapPawns.Count; i++)
            {
                Pawn p = mapPawns[i];
                if (p == me || p.Dead || p.Downed) continue;
                if (p.Faction != Faction.OfPlayer && p.Faction != me.Faction) continue;
                if (!p.Position.InHorDistOf(me.Position, Props.range)) continue;

                currentInRange.Add(p.thingIDNumber);
                if (!warmedPawns.Contains(p.thingIDNumber))
                {
                    p.health.GetOrAddHediff(hediff).Severity = 1.0f;
                }
                else
                {
                    var existing = p.health.hediffSet.GetFirstHediffOfDef(hediff);
                    if (existing != null)
                    {
                        var disappears = existing.TryGetComp<HediffComp_Disappears>();
                        if (disappears != null) disappears.ResetElapsedTicks();
                    }
                }
            }

            warmedPawns.RemoveWhere(id =>
            {
                if (currentInRange.Contains(id)) return false;
                Pawn p = me.Map.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.thingIDNumber == id);
                if (p != null)
                {
                    var existing = p.health.hediffSet.GetFirstHediffOfDef(hediff);
                    if (existing != null) p.health.RemoveHediff(existing);
                }
                return true;
            });

            warmedPawns = currentInRange;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (previousMap != null)
            {
                HediffDef hediff = DefDatabase<HediffDef>.GetNamedSilentFail(Props.hediffDefName);
                if (hediff != null)
                {
                    foreach (int id in warmedPawns)
                    {
                        Pawn p = previousMap.mapPawns.AllPawnsSpawned.FirstOrDefault(pawn => pawn.thingIDNumber == id);
                        if (p != null)
                        {
                            var existing = p.health.hediffSet.GetFirstHediffOfDef(hediff);
                            if (existing != null) p.health.RemoveHediff(existing);
                        }
                    }
                }
            }
            warmedPawns.Clear();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (warmedPawns == null) warmedPawns = new HashSet<int>();
        }
    }
}
