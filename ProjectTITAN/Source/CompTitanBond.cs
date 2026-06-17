using System;
using System.Collections.Generic; // 必须有这个
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class CompProperties_TitanBond : CompProperties
    {
        public string targetPawnKind; // 寻找的目标是谁
        public HediffDef bondHediff;  // 激活什么Buff
        public float range = 15f;     // 距离多少生效

        public CompProperties_TitanBond()
        {
            this.compClass = typeof(CompTitanBond);
        }
    }

    public class CompTitanBond : ThingComp
    {
        public CompProperties_TitanBond Props => (CompProperties_TitanBond)this.props;

        // 每 250 tick (约4秒) 检查一次，节省性能
        public override void CompTickRare()
        {
            base.CompTickRare();

            Pawn me = this.parent as Pawn;
            if (me == null || me.Map == null || me.Dead || me.Downed) return;

            // 寻找目标
            bool targetFound = false;

            // 【关键修复】这里把 List<Pawn> 改成了 IReadOnlyList<Pawn>
            // 或者你也可以写 var mapPawns = ...
            IReadOnlyList<Pawn> mapPawns = me.Map.mapPawns.AllPawnsSpawned;

            // IReadOnlyList 依然支持 Count 和 [i] 索引，所以下面的循环不用改
            for (int i = 0; i < mapPawns.Count; i++)
            {
                Pawn p = mapPawns[i];
                // 必须是目标种类，且活着，且属于玩家
                if (p.kindDef?.defName == Props.targetPawnKind && !p.Dead && p.Faction == Faction.OfPlayer)
                {
                    // 检查距离
                    if (p.Position.InHorDistOf(me.Position, Props.range))
                    {
                        targetFound = true;
                        break;
                    }
                }
            }

            // 处理 Buff
            Hediff existing = me.health.hediffSet.GetFirstHediffOfDef(Props.bondHediff);

            if (targetFound)
            {
                if (existing == null)
                {
                    me.health.AddHediff(Props.bondHediff);
                }
                else
                {
                    var disappears = existing.TryGetComp<HediffComp_Disappears>();
                    if (disappears != null) disappears.ResetElapsedTicks();
                }
            }
            else
            {
                if (existing != null)
                {
                    me.health.RemoveHediff(existing);
                }
            }
        }
    }
}