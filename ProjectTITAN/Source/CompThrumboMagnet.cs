using System;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class CompThrumboMagnet : ThingComp
    {
        // 改用 CompTick，因为我们需要精确控制时间间隔
        public override void CompTick()
        {
            base.CompTick();

            Pawn p = this.parent as Pawn;
            // 1. 基础检查
            if (p == null || p.Map == null || p.Dead || p.ageTracker.AgeBiologicalYearsFloat < 0.5f) return;

            // 2. 【关键修改】每 60000 tick (游戏内 1 天) 检查一次
            // IsHashIntervalTick 可以大大减少性能消耗，且频率稳定
            if (p.IsHashIntervalTick(60000))
            {
                // 3. 每天有 5% 的概率触发 (期望值：20天触发一次)
                // 你觉得太少可以改成 0.1f (10天)
                if (Rand.Chance(0.05f))
                {
                    TriggerThrumboEvent(p);
                }
            }
        }

        private void TriggerThrumboEvent(Pawn p)
        {
            Map map = p.Map;
            if (map == null) return;

            IncidentDef def = IncidentDef.Named("ThrumboPasses");
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);

            if (def.Worker.CanFireNow(parms))
            {
                def.Worker.TryExecute(parms);
                Messages.Message("Message_ThrumboMagnet_Attracted".Translate(), p, MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}