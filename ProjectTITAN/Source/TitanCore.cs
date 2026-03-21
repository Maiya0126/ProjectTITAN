using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ProjectTITAN
{
    // =============================================================
    // 1. 数据存储组件 (TitanGameComponent)
    // =============================================================
    public class TitanGameComponent : GameComponent
    {
        public bool hasBefriendedWarlord = false;
        public bool hasBefriendedVoidWalker = false;
        public bool hasBefriendedMatriarch = false;

        public TitanGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasBefriendedWarlord, "hasBefriendedWarlord", false);
            Scribe_Values.Look(ref hasBefriendedVoidWalker, "hasBefriendedVoidWalker", false);
            Scribe_Values.Look(ref hasBefriendedMatriarch, "hasBefriendedMatriarch", false);
        }
    }

    // =============================================================
    // 2. 食用效果逻辑 (无冲突版)
    // =============================================================
    public class IngestionOutcomeDoer_TitanMutation : IngestionOutcomeDoer
    {
        public HediffDef hediffDef;

        protected override void DoIngestionOutcomeSpecial(Pawn pawn, Thing ingested, int ingestedCount)
        {
            if (pawn == null || hediffDef == null) return;

            // 种族检查
            bool isPrototype = pawn.def.defName == "TITAN_ThrumboPrototype";
            bool isThrumbo = pawn.def.defName == "Thrumbo";

            if (!isPrototype && !isThrumbo)
            {
                Messages.Message(string.Format("Message_TITAN_CannotHandleGenes".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                GenSpawn.Spawn(ingested.def, pawn.Position, pawn.Map);
                return;
            }

            // 冲突检查
            if (isThrumbo)
            {
                if (TitanImplantUtils.HasAnyTitanImplant(pawn))
                {
                    if (pawn.health.hediffSet.HasHediff(hediffDef))
                        Messages.Message(string.Format("Message_TITAN_AlreadyHasAbility".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.NeutralEvent, false);
                    else
                        Messages.Message(string.Format("Message_TITAN_GenomeSaturated".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);

                    GenSpawn.Spawn(ingested.def, pawn.Position, pawn.Map);
                    return;
                }
            }

            // 成功融合
            pawn.health.AddHediff(hediffDef);
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Message_GeneFusion".Translate().ToString(), Color.green);
            Messages.Message(string.Format("Message_TITAN_FusionSuccess".Translate(), pawn.LabelShort, ingested.Label), pawn, MessageTypeDefOf.PositiveEvent);

            // 0号觉醒检查
            if (isPrototype) CheckAwakening(pawn);
        }

        private void CheckAwakening(Pawn pawn)
        {
            var set = pawn.health.hediffSet;
            HediffDef kingsBlood = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_KingsBloodline");
            if (kingsBlood == null) return;

            if (TitanImplantUtils.HasAllImplants(pawn))
            {
                if (!set.HasHediff(kingsBlood))
                {
                    pawn.health.AddHediff(kingsBlood);
                    Messages.Message("Message_TITAN_KingsAwakening".Translate(), pawn, MessageTypeDefOf.PositiveEvent);
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "Message_KingsAwakeningMote".Translate().ToString(), Color.yellow);
                }
            }
        }
    }

    // =============================================================
    // 3. 0号技能组件 (CompTitanAbility)
    // =============================================================
    public class CompProperties_TitanAbility : CompProperties
    {
        public float cooldownDays = 0.04f;
        public int healAmount = 20;
        public float range = 40f;
        public CompProperties_TitanAbility() { this.compClass = typeof(CompTitanAbility); }
    }

    public class CompTitanAbility : ThingComp
    {
        public CompProperties_TitanAbility Props => (CompProperties_TitanAbility)this.props;
        private int lastUsedTick = -999999;
        public bool OnCooldown => Find.TickManager.TicksGame < lastUsedTick + (Props.cooldownDays * 60000);

        private Texture2D cachedIcon;
        private Texture2D GetIcon()
        {
            if (cachedIcon == null)
            {
                ThingDef horn = DefDatabase<ThingDef>.GetNamedSilentFail("ThrumboHorn");
                if (horn != null) cachedIcon = horn.uiIcon;
                else cachedIcon = BaseContent.BadTex;
            }
            return cachedIcon;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Faction != Faction.OfPlayer) yield break;

            Command_Target cmd = new Command_Target
            {
                defaultLabel = "泰坦共鸣",
                defaultDesc = "利用0号的皇室基因释放共鸣波。【冷却：1小时】- 任务神兽：执行特定的治疗/安抚操作，完成后给予奖励并离开。- 支援友军：感谢支援并指引离开。- 普通友军：治疗伤口。",
                icon = GetIcon(),
                targetingParams = new TargetingParameters { canTargetPawns = true, validator = (TargetInfo x) => x.Thing is Pawn },
                action = (LocalTargetInfo target) => UseAbility(target.Pawn)
            };

            if (OnCooldown)
            {
                float ticksLeft = (lastUsedTick + (Props.cooldownDays * 60000)) - Find.TickManager.TicksGame;
                cmd.Disable("Cooldown".Translate(ticksLeft / 2500f));
            }
            yield return cmd;
        }

        private void UseAbility(Pawn target)
        {
            if (target == null) return;
            if (!target.Position.InHorDistOf(this.parent.Position, Props.range))
            {
                Messages.Message("Message_TITAN_TargetTooFar".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            lastUsedTick = Find.TickManager.TicksGame;
            MoteMaker.MakeInteractionBubble(this.parent as Pawn, target, ThingDefOf.Mote_ThoughtGood, GetIcon());

            // 分支逻辑
            if (target.def.defName == "TITAN_Matriarch")
            {
                Hediff sickness = target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TITAN_GeneticCollapse"));
                if (sickness != null)
                {
                    sickness.Severity -= 0.25f;
                    if (sickness.Severity <= 0.01f)
                    {
                        target.health.RemoveHediff(sickness);
                        Messages.Message("Message_TITAN_MatriarchHealed".Translate(), target, MessageTypeDefOf.PositiveEvent);
                        UnlockAndReward(target, "TITAN_Techprint_Matriarch", "hasBefriendedMatriarch");
                    }
                    else Messages.Message(string.Format("Message_TITAN_MatriarchHealing".Translate(), sickness.Severity.ToString("P0")), target, MessageTypeDefOf.PositiveEvent);
                }
                else SendTitanAway(target);
            }
            else if (target.def.defName == "TITAN_Warlord")
            {
                bool isQuestVersion = target.HostileTo(Faction.OfPlayer);
                if (isQuestVersion)
                {
                    Hediff soothing = target.health.GetOrAddHediff(HediffDef.Named("TITAN_WarlordSoothing"));
                    soothing.Severity += 0.34f;
                    target.TakeDamage(new DamageInfo(DamageDefOf.Stun, 30f));

                    if (soothing.Severity >= 1.0f)
                    {
                        target.mindState.mentalStateHandler.Reset();
                        Messages.Message("Message_TITAN_WarlordCalmed".Translate(), target, MessageTypeDefOf.PositiveEvent);
                        UnlockAndReward(target, "TITAN_Techprint_Warlord", "hasBefriendedWarlord");
                    }
                    else Messages.Message("Message_TITAN_WarlordSoothing".Translate(), target, MessageTypeDefOf.NeutralEvent);
                }
                else SendTitanAway(target);
            }
            else if (target.def.defName == "TITAN_VoidWalker")
            {
                bool enemiesNearby = target.Map.mapPawns.AllPawnsSpawned.Any(p => p.HostileTo(Faction.OfPlayer) && !p.Downed && !p.Dead && p.Position.InHorDistOf(target.Position, 30f));
                if (enemiesNearby)
                {
                    target.health.AddHediff(HediffDef.Named("TITAN_VoidAnchor"));
                    Messages.Message("Message_TITAN_VoidWalkerThreat".Translate(), target, MessageTypeDefOf.RejectInput);
                }
                else
                {
                    var comp = Current.Game.GetComponent<TitanGameComponent>();
                    if (comp != null && !comp.hasBefriendedVoidWalker)
                    {
                        Messages.Message("Message_TITAN_VoidWalkerSafe".Translate(), target, MessageTypeDefOf.PositiveEvent);
                        UnlockAndReward(target, "TITAN_Techprint_VoidWalker", "hasBefriendedVoidWalker");
                    }
                    else SendTitanAway(target);
                }
            }
            else
            {
                List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                target.health.hediffSet.GetHediffs(ref injuries);
                int healLeft = Props.healAmount;
                foreach (var injury in injuries)
                {
                    if (healLeft <= 0) break;
                    int heal = Mathf.Min(healLeft, (int)injury.Severity);
                    injury.Heal(heal);
                    healLeft -= heal;
                }
                Messages.Message(string.Format("Message_TITAN_Healed".Translate(), target.LabelShort), target, MessageTypeDefOf.PositiveEvent);
            }
        }

        private void UnlockAndReward(Pawn titan, string itemDefName, string flagName)
        {
            if (titan.Map == null) return;
            var comp = Current.Game.GetComponent<TitanGameComponent>();
            if (comp != null)
            {
                if (flagName == "hasBefriendedWarlord") comp.hasBefriendedWarlord = true;
                if (flagName == "hasBefriendedVoidWalker") comp.hasBefriendedVoidWalker = true;
                if (flagName == "hasBefriendedMatriarch") comp.hasBefriendedMatriarch = true;
            }
            ThingDef itemDef = DefDatabase<ThingDef>.GetNamedSilentFail(itemDefName);
            if (itemDef != null) GenSpawn.Spawn(itemDef, titan.Position, titan.Map);
            SendTitanAway(titan);
        }

        private void SendTitanAway(Pawn titan)
        {
            if (titan.Map == null) return;
            Lord lord = titan.GetLord();
            if (lord != null) titan.Map.lordManager.RemoveLord(lord);
            LordMaker.MakeNewLord(titan.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, true, true), titan.Map, new List<Pawn> { titan });
            Messages.Message(string.Format("Message_TITAN_Leaving".Translate(), titan.LabelShort), MessageTypeDefOf.NeutralEvent);
        }

        public override void PostExposeData() { base.PostExposeData(); Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", -999999); }
    }

    // =============================================================
    // 4. 袭击支援补丁 (TitanAllySystem)
    // =============================================================
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker")]
    public static class Patch_RaidReinforcement
    {
        static void Postfix(IncidentWorker_RaidEnemy __instance, IncidentParms parms, bool __result)
        {
            if (!__result || parms.target == null || !(parms.target is Map map)) return;
            var comp = Current.Game.GetComponent<TitanGameComponent>();
            if (comp == null || !Rand.Chance(0.5f)) return;

            List<PawnKindDef> candidates = new List<PawnKindDef>();
            if (comp.hasBefriendedWarlord) candidates.Add(DefDatabase<PawnKindDef>.GetNamedSilentFail("TITAN_Warlord"));
            if (comp.hasBefriendedVoidWalker) candidates.Add(DefDatabase<PawnKindDef>.GetNamedSilentFail("TITAN_VoidWalker"));
            if (comp.hasBefriendedMatriarch) candidates.Add(DefDatabase<PawnKindDef>.GetNamedSilentFail("TITAN_Matriarch"));
            candidates.RemoveAll(x => x == null);

            if (candidates.Count == 0) return;

            int spawnCount = Rand.Range(1, 4);
            List<Pawn> spawnedAllies = new List<Pawn>();
            Faction allyFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients);

            for (int i = 0; i < spawnCount; i++)
            {
                PawnKindDef leaderKind = candidates.RandomElement();
                IntVec3 spawnLoc;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnLoc, map, CellFinder.EdgeRoadChance_Friendly)) continue;

                Pawn ally = PawnGenerator.GeneratePawn(leaderKind, null);
                if (allyFaction != null) ally.SetFaction(allyFaction); else ally.SetFaction(null);

                // 3倍血量和攻击力
                if (ally.def.race != null)
                {
                    ally.def.race.baseHealthScale *= 3f;
                    ally.def.race.baseBodySize *= 3f;
                }
                // 增强工具攻击力
                var tools = ally.def.tools;
                if (tools != null)
                {
                    for (int t = 0; t < tools.Count; t++)
                    {
                        tools[t].power *= 3f;
                    }
                }

                GenSpawn.Spawn(ally, spawnLoc, map, Rot4.Random);
                spawnedAllies.Add(ally);
            }

            if (spawnedAllies.Count == 0) return;

            List<Thing> enemies = map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer) && !p.Downed).Cast<Thing>().ToList();
            if (enemies.Count > 0 && spawnedAllies[0].Faction != null)
            {
                LordMaker.MakeNewLord(spawnedAllies[0].Faction, new LordJob_AssaultThings(Faction.OfPlayer, enemies), map, spawnedAllies);
            }

            Pawn mainAlly = spawnedAllies[0];
            string beastNames = string.Join("、", spawnedAllies.Select(p => p.LabelShort));
            Find.LetterStack.ReceiveLetter("Letter_TitanReinforcement".Translate(), string.Format("Letter_TitanReinforcement_Body".Translate(), beastNames), LetterDefOf.PositiveEvent, mainAlly);
        }
    }

    // =============================================================
    // 5. 工具类与光环组件
    // =============================================================
    public static class TitanImplantUtils
    {
        public static bool HasAnyTitanImplant(Pawn p)
        {
            var set = p.health.hediffSet;
            return HasImp(set, "TITAN_Implant_CrimsonHeart") || HasImp(set, "TITAN_Implant_VoidMembrane") || HasImp(set, "TITAN_Implant_VerdantSpine");
        }
        public static bool HasAllImplants(Pawn p)
        {
            var set = p.health.hediffSet;
            return HasImp(set, "TITAN_Implant_CrimsonHeart") && HasImp(set, "TITAN_Implant_VoidMembrane") && HasImp(set, "TITAN_Implant_VerdantSpine");
        }
        private static bool HasImp(HediffSet set, string name)
        {
            HediffDef d = DefDatabase<HediffDef>.GetNamedSilentFail(name);
            return d != null && set.HasHediff(d);
        }
    }

    public class HediffCompProperties_KingsAura : HediffCompProperties
    {
        public float radius = 15f;
        public HediffCompProperties_KingsAura() { this.compClass = typeof(HediffComp_KingsAura); }
    }
    public class HediffComp_KingsAura : HediffComp
    {
        public HediffCompProperties_KingsAura Props => (HediffCompProperties_KingsAura)this.props;
        private int tickCounter = 0;
        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = this.Pawn;
            if (pawn.Map == null || !pawn.Spawned) return;
            if (tickCounter++ % 180 == 0)
            {
                HediffDef buff = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_Buff_KingsGrace");
                if (buff != null)
                {
                    IReadOnlyList<Pawn> allies = pawn.Map.mapPawns.AllPawnsSpawned;
                    foreach (Pawn p in allies)
                    {
                        if (p != pawn && !p.Dead && p.Faction == pawn.Faction && p.Position.InHorDistOf(pawn.Position, Props.radius))
                            p.health.GetOrAddHediff(buff).Severity = 1.0f;
                    }
                }
            }
        }
    }

    public class CompProperties_TitanAura : CompProperties
    {
        public float radius = 10f;
        public HediffDef buffDef;
        public HediffDef selfBuffDef;
        public bool selfDamage = false;
        public CompProperties_TitanAura() { this.compClass = typeof(CompTitanAura); }
    }
    public class CompTitanAura : ThingComp
    {
        public CompProperties_TitanAura Props => (CompProperties_TitanAura)this.props;
        private int tickCounter = 0;
        public override void CompTick()
        {
            Pawn owner = this.parent as Pawn;
            if (owner == null || owner.Map == null || owner.Dead || owner.Downed) return;
            if (tickCounter++ % 60 == 0)
            {
                if (Props.selfBuffDef != null) owner.health.GetOrAddHediff(Props.selfBuffDef).Severity = 1.0f;
                if (Props.buffDef != null)
                {
                    IReadOnlyList<Pawn> allies = owner.Map.mapPawns.AllPawnsSpawned;
                    foreach (Pawn p in allies)
                    {
                        if (p != owner && !p.Dead && p.Faction == owner.Faction && p.Position.InHorDistOf(owner.Position, Props.radius))
                        {
                            p.health.GetOrAddHediff(Props.buffDef).Severity = 1.0f;
                        }
                    }
                }
                if (Props.selfDamage && Rand.Chance(0.05f))
                {
                    owner.TakeDamage(new DamageInfo(DamageDefOf.Blunt, 5));
                    MoteMaker.ThrowText(owner.DrawPos, owner.Map, "Message_LifeDrain".Translate().ToString(), Color.green);
                }
            }
        }
    }

    public class HediffCompProperties_HealthDrain : HediffCompProperties
    {
        public int damageAmount = 1;
        public float intervalSeconds = 15.0f;
        public HediffCompProperties_HealthDrain() { this.compClass = typeof(HediffComp_HealthDrain); }
    }
    public class HediffComp_HealthDrain : HediffComp
    {
        public HediffCompProperties_HealthDrain Props => (HediffCompProperties_HealthDrain)this.props;
        private int ticks = 0;
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (ticks++ > (Props.intervalSeconds * 60))
            {
                ticks = 0;
                this.Pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, Props.damageAmount));
            }
        }
    }
}