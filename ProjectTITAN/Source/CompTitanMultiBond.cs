using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class BondRule
    {
        public string targetPawnKind;
        public string positiveHediff;
        public string negativeHediff;
        public float range = 15f;
    }

    public class CompProperties_TitanMultiBond : CompProperties
    {
        public List<BondRule> bondRules = new List<BondRule>();

        public CompProperties_TitanMultiBond()
        {
            this.compClass = typeof(CompTitanMultiBond);
        }
    }

    public class CompTitanMultiBond : ThingComp
    {
        public CompProperties_TitanMultiBond Props => (CompProperties_TitanMultiBond)this.props;
        private Dictionary<string, bool> activeBonds = new Dictionary<string, bool>();

        public override void CompTickRare()
        {
            base.CompTickRare();
            Pawn me = this.parent as Pawn;
            if (me == null || me.Map == null || me.Dead || me.Downed) return;

            IReadOnlyList<Pawn> mapPawns = me.Map.mapPawns.AllPawnsSpawned;

            foreach (var rule in Props.bondRules)
            {
                bool found = false;
                for (int i = 0; i < mapPawns.Count; i++)
                {
                    Pawn p = mapPawns[i];
                    if (p.kindDef?.defName == rule.targetPawnKind && !p.Dead && p.Faction == Faction.OfPlayer)
                    {
                        if (p.Position.InHorDistOf(me.Position, rule.range))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                string hediffDefName = found ? (rule.positiveHediff ?? rule.negativeHediff) : null;

                if (!activeBonds.ContainsKey(rule.positiveHediff ?? rule.negativeHediff))
                    activeBonds[rule.positiveHediff ?? rule.negativeHediff] = false;

                if (found && !activeBonds[rule.positiveHediff ?? rule.negativeHediff])
                {
                    string toApply = rule.positiveHediff ?? rule.negativeHediff;
                    HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(toApply);
                    if (def != null) me.health.GetOrAddHediff(def).Severity = 1.0f;
                    activeBonds[rule.positiveHediff ?? rule.negativeHediff] = true;
                }
                else if (found && activeBonds[rule.positiveHediff ?? rule.negativeHediff])
                {
                    string toRefresh = rule.positiveHediff ?? rule.negativeHediff;
                    HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(toRefresh);
                    if (def != null)
                    {
                        var existing = me.health.hediffSet.GetFirstHediffOfDef(def);
                        if (existing != null)
                        {
                            var disappears = existing.TryGetComp<HediffComp_Disappears>();
                            if (disappears != null) disappears.ResetElapsedTicks();
                        }
                    }
                }
                else if (!found && activeBonds[rule.positiveHediff ?? rule.negativeHediff])
                {
                    string toRemove = rule.positiveHediff ?? rule.negativeHediff;
                    HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail(toRemove);
                    if (def != null)
                    {
                        var existing = me.health.hediffSet.GetFirstHediffOfDef(def);
                        if (existing != null) me.health.RemoveHediff(existing);
                    }

                    if (!string.IsNullOrEmpty(rule.negativeHediff) && rule.negativeHediff != rule.positiveHediff)
                    {
                        HediffDef negDef = DefDatabase<HediffDef>.GetNamedSilentFail(rule.negativeHediff);
                        if (negDef != null)
                        {
                            var existing = me.health.hediffSet.GetFirstHediffOfDef(negDef);
                            if (existing != null) me.health.RemoveHediff(existing);
                        }
                        activeBonds[rule.negativeHediff] = false;
                    }

                    activeBonds[rule.positiveHediff ?? rule.negativeHediff] = false;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (activeBonds == null) activeBonds = new Dictionary<string, bool>();
        }
    }
}