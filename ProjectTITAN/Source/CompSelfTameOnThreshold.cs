using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI.Group;

namespace ProjectTITAN
{
    public class CompProperties_SelfTameOnThreshold : CompProperties
    {
        public float healthThreshold = 0.5f;
        public float prototypeProximity = 20f;

        public CompProperties_SelfTameOnThreshold()
        {
            this.compClass = typeof(CompSelfTameOnThreshold);
        }
    }

    public class CompSelfTameOnThreshold : ThingComp
    {
        public CompProperties_SelfTameOnThreshold Props => (CompProperties_SelfTameOnThreshold)this.props;
        public bool enabled = false;
        private bool selfTamed = false;

        public override void CompTick()
        {
            if (!enabled || selfTamed) return;
            Pawn me = this.parent as Pawn;
            if (me == null || me.Map == null || me.Dead || me.Faction == Faction.OfPlayer) return;

            if (me.health.summaryHealth.SummaryHealthPercent <= Props.healthThreshold)
            {
                DoSelfTame(me);
                return;
            }

            Pawn prototype0 = me.Map.mapPawns.SpawnedColonyAnimals
                .FirstOrDefault(p => p.kindDef?.defName == "TITAN_ThrumboPrototype");
            if (prototype0 != null && me.Position.InHorDistOf(prototype0.Position, Props.prototypeProximity))
            {
                DoSelfTame(me);
            }
        }

        private void DoSelfTame(Pawn me)
        {
            selfTamed = true;
            me.SetFaction(Faction.OfPlayer);
            if (me.mindState.mentalStateHandler.CurState != null)
                me.mindState.mentalStateHandler.Reset();
            Lord lord = me.GetLord();
            if (lord != null) me.Map.lordManager.RemoveLord(lord);
            Messages.Message("TITAN_Message_No7_SelfTamed".Translate(me.LabelShort), me, MessageTypeDefOf.PositiveEvent);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref enabled, "enabled", false);
            Scribe_Values.Look(ref selfTamed, "selfTamed", false);
        }
    }
}