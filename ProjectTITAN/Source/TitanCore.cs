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
        public int peakExperimentCount = 0;
        public bool resonanceTier1Notified = false;
        public bool resonanceTier2Notified = false;
        public bool resonanceTier3Notified = false;

        public TitanGameComponent(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref hasBefriendedWarlord, "hasBefriendedWarlord", false);
            Scribe_Values.Look(ref hasBefriendedVoidWalker, "hasBefriendedVoidWalker", false);
            Scribe_Values.Look(ref hasBefriendedMatriarch, "hasBefriendedMatriarch", false);
            Scribe_Values.Look(ref peakExperimentCount, "peakExperimentCount", 0);
            Scribe_Values.Look(ref resonanceTier1Notified, "resonanceTier1Notified", false);
            Scribe_Values.Look(ref resonanceTier2Notified, "resonanceTier2Notified", false);
            Scribe_Values.Look(ref resonanceTier3Notified, "resonanceTier3Notified", false);
        }

        public void UpdatePeakExperimentCount()
        {
            int current = CountColonyExperimentSubjects();
            if (current > peakExperimentCount)
            {
                int oldPeak = peakExperimentCount;
                peakExperimentCount = current;
                CheckResonanceTierNotification(oldPeak, peakExperimentCount);
            }
        }

        private void CheckResonanceTierNotification(int oldPeak, int newPeak)
        {
            int[] thresholds = { 5, 15, 30 };
            string[] notifyFields = { "resonanceTier1Notified", "resonanceTier2Notified", "resonanceTier3Notified" };
            string[] messageKeys = { "TITAN_Resonance_Tier1Unlocked", "TITAN_Resonance_Tier2Unlocked", "TITAN_Resonance_Tier3Unlocked" };
            bool[] notified = { resonanceTier1Notified, resonanceTier2Notified, resonanceTier3Notified };

            for (int i = 0; i < thresholds.Length; i++)
            {
                if (!notified[i] && newPeak >= thresholds[i])
                {
                    notified[i] = true;
                    Messages.Message(messageKeys[i].Translate(), MessageTypeDefOf.PositiveEvent);
                }
            }
            resonanceTier1Notified = notified[0];
            resonanceTier2Notified = notified[1];
            resonanceTier3Notified = notified[2];
        }

        private int CountColonyExperimentSubjects()
        {
            int count = 0;
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.SpawnedColonyAnimals)
                {
                    if (TitanImplantUtils.CountsTowardResonance(p))
                        count++;
                }
            }
            return count;
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
            bool isExperimentSubject = pawn.def.defName.StartsWith("TITAN_No");

            if (!isPrototype && !isThrumbo && !isExperimentSubject)
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

            if (isExperimentSubject)
            {
                if (!TitanImplantUtils.CanExperimentSubjectAcceptMutagen(pawn, hediffDef))
                {
                    Messages.Message(string.Format("TITAN_Message_ExperimentIncompatibleMutagen".Translate(), pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                    GenSpawn.Spawn(ingested.def, pawn.Position, pawn.Map);
                    return;
                }
                if (TitanImplantUtils.HasAnyTitanImplant(pawn))
                {
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
        public bool OnCooldown => Find.TickManager.TicksGame < lastUsedTick + (AdjustedCooldownDays * 60000);
        private int pulseTick = 0;
        private bool peakInitialized = false;
        private int herdCheckTick = 2501;

        public int CurrentResonanceTier
        {
            get
            {
                var comp = Current.Game?.GetComponent<TitanGameComponent>();
                if (comp == null) return 0;
                int peak = comp.peakExperimentCount;
                bool hasKing = (this.parent as Pawn)?.health?.hediffSet?.HasHediff(HediffDef.Named("TITAN_KingsBloodline")) == true;
                if (hasKing) return 4;
                if (peak >= 30) return 3;
                if (peak >= 15) return 2;
                if (peak >= 5) return 1;
                return 0;
            }
        }

        public float AdjustedRange => Props.range * (CurrentResonanceTier >= 1 ? 1.5f : 1f);
        public int AdjustedHealAmount => (int)(Props.healAmount * (CurrentResonanceTier >= 1 ? 1.5f : 1f));
        public float AdjustedCooldownDays => Props.cooldownDays * (CurrentResonanceTier >= 1 ? 0.75f : 1f);
        public float AdjustedPulseRange
        {
            get
            {
                switch (CurrentResonanceTier)
                {
                    case 1: return 8f;
                    case 2: return 12f;
                    case 3: return 15f;
                    case 4: return 20f;
                    default: return 0f;
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (this.parent?.Map == null || this.parent.DestroyedOrNull()) return;

            if (!peakInitialized)
            {
                peakInitialized = true;
                var titanComp = Current.Game?.GetComponent<TitanGameComponent>();
                if (titanComp != null) titanComp.UpdatePeakExperimentCount();
                var herdTracker = Current.Game?.GetComponent<GameComponent_ThrumboHerdTracker>();
                if (herdTracker != null) herdTracker.TickHerd();
            }

            if (herdCheckTick++ >= 2500)
            {
                herdCheckTick = 0;
                var herdTracker = Current.Game?.GetComponent<GameComponent_ThrumboHerdTracker>();
                if (herdTracker != null) herdTracker.TickHerd();
            }
            if (CurrentResonanceTier >= 1 && pulseTick++ >= 3000)
            {
                pulseTick = 0;
                DoSyncPulse();
            }
        }

        private void DoSyncPulse()
        {
            Pawn me = this.parent as Pawn;
            if (me == null || me.Dead || me.Downed || me.Map == null) return;

            float pulseRange = AdjustedPulseRange;
            if (pulseRange <= 0f) return;

            IReadOnlyList<Pawn> pawns = me.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == me || p.Dead || p.Downed) continue;
                if (p.Faction != Faction.OfPlayer) continue;
                if (!p.Position.InHorDistOf(me.Position, pulseRange)) continue;
                List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                p.health.hediffSet.GetHediffs(ref injuries);
                foreach (var injury in injuries)
                {
                    if (injury.Severity > 0)
                    {
                        injury.Heal(1);
                        break;
                    }
                }
            }
        }

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
                defaultLabel = "TITAN_Ability_Resonance".Translate(),
                defaultDesc = GetResonanceDesc(),
                icon = GetIcon(),
                targetingParams = new TargetingParameters { canTargetPawns = true, validator = (TargetInfo x) => x.Thing is Pawn },
                action = (LocalTargetInfo target) => UseAbility(target.Pawn)
            };

            if (OnCooldown)
            {
                float ticksLeft = (lastUsedTick + (AdjustedCooldownDays * 60000)) - Find.TickManager.TicksGame;
                cmd.Disable("Cooldown".Translate(ticksLeft / 2500f));
            }
            yield return cmd;

            if (CurrentResonanceTier >= 4)
            {
                Command_Target royalCmd = new Command_Target
                {
                    defaultLabel = "TITAN_Ability_RoyalMark".Translate(),
                    defaultDesc = "TITAN_Ability_RoyalMarkDesc".Translate(),
                    icon = GetIcon(),
                    targetingParams = new TargetingParameters { canTargetPawns = true, validator = (TargetInfo x) => x.Thing is Pawn },
                    action = (LocalTargetInfo target) => ApplyRoyalMark(target.Pawn)
                };
                yield return royalCmd;
            }
        }

        private string GetResonanceDesc()
        {
            int tier = CurrentResonanceTier;
            string desc = "TITAN_Ability_ResonanceDesc".Translate();
            desc += "\n\n" + string.Format("TITAN_Ability_CurrentStats".Translate(),
                (int)AdjustedRange, AdjustedHealAmount, (AdjustedCooldownDays * 60000f / 2500f).ToString("F1"));
            if (tier >= 1) desc += "\n" + "TITAN_Ability_Tier1Bonus".Translate();
            if (tier >= 2) desc += "\n" + "TITAN_Ability_Tier2Bonus".Translate();
            if (tier >= 3) desc += "\n" + "TITAN_Ability_Tier3Bonus".Translate();
            if (tier >= 4) desc += "\n" + "TITAN_Ability_Tier4Bonus".Translate();
            return desc;
        }

        private void UseAbility(Pawn target)
        {
            if (target == null) return;
            if (!target.Position.InHorDistOf(this.parent.Position, AdjustedRange))
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
            else if (target.def.defName == "TITAN_No42_AuroraThrumbo")
            {
                Hediff drift = target.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("TITAN_GeneticDrift"));
                if (drift != null)
                {
                    drift.Severity -= 0.35f;
                    if (drift.Severity <= 0.01f)
                    {
                        target.health.RemoveHediff(drift);
                        Messages.Message("TITAN_Message_No42_DriftHealed".Translate(), target, MessageTypeDefOf.PositiveEvent);
                    }
                    else Messages.Message(string.Format("TITAN_Message_No42_DriftHealing".Translate(), drift.Severity.ToString("P0")), target, MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    HealInjuries(target);
                    TryRecruitExperimentSubject(target);
                }
            }
            else if (target.def.defName.StartsWith("TITAN_No"))
            {
                HealInjuries(target);
                TryRecruitExperimentSubject(target);
            }
            else
            {
                HealInjuries(target);
            }

            if (CurrentResonanceTier >= 3)
            {
                DoProgenitorEcho(target);
            }
        }

        private void HealInjuries(Pawn target)
        {
            List<Hediff_Injury> injuries = new List<Hediff_Injury>();
            target.health.hediffSet.GetHediffs(ref injuries);
            int healLeft = AdjustedHealAmount;
            foreach (var injury in injuries)
            {
                if (healLeft <= 0) break;
                int heal = Mathf.Min(healLeft, (int)injury.Severity);
                injury.Heal(heal);
                healLeft -= heal;
            }
            Messages.Message(string.Format("Message_TITAN_Healed".Translate(), target.LabelShort), target, MessageTypeDefOf.PositiveEvent);
        }

        private void DoProgenitorEcho(Pawn primaryTarget)
        {
            Pawn me = this.parent as Pawn;
            if (me == null || me.Map == null) return;

            int healedCount = 0;
            IReadOnlyList<Pawn> pawns = me.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn p = pawns[i];
                if (p == primaryTarget || p == me) continue;
                if (p.Dead || p.Downed) continue;
                if (p.Faction != Faction.OfPlayer && p.Faction != me.Faction) continue;
                if (!TitanImplantUtils.IsThrumboKind(p)) continue;
                if (!p.Position.InHorDistOf(me.Position, 10f)) continue;

                List<Hediff_Injury> injuries = new List<Hediff_Injury>();
                p.health.hediffSet.GetHediffs(ref injuries);
                foreach (var injury in injuries)
                {
                    if (injury.Severity > 0)
                    {
                        injury.Heal(Mathf.Min(5, (int)injury.Severity));
                        healedCount++;
                        break;
                    }
                }
            }
            if (healedCount > 0)
                Messages.Message(string.Format("TITAN_Message_ProgenitorEcho".Translate(), healedCount), me, MessageTypeDefOf.PositiveEvent);
        }

        private void ApplyRoyalMark(Pawn target)
        {
            if (target == null) return;
            if (!TitanImplantUtils.IsThrumboKind(target))
            {
                Messages.Message("TITAN_Message_RoyalMark_NotThrumboKind".Translate(), target, MessageTypeDefOf.RejectInput);
                return;
            }
            HediffDef markDef = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_RoyalMark");
            if (markDef == null) return;
            if (target.health.hediffSet.HasHediff(markDef))
            {
                Messages.Message("TITAN_Message_RoyalMark_AlreadyMarked".Translate(), target, MessageTypeDefOf.RejectInput);
                return;
            }
            target.health.AddHediff(markDef);
            Messages.Message(string.Format("TITAN_Message_RoyalMark_Applied".Translate(), target.LabelShort), target, MessageTypeDefOf.PositiveEvent);
        }

        private void TryRecruitExperimentSubject(Pawn target)
        {
            if (target.Faction == Faction.OfPlayer) return;
            if (target.HostileTo(Faction.OfPlayer)) return;
            target.SetFaction(Faction.OfPlayer);
            if (target.kindDef != null)
                target.Name = new NameSingle(target.kindDef.label);
            Lord lord = target.GetLord();
            if (lord != null) target.Map.lordManager.RemoveLord(lord);
            var titanComp = Current.Game?.GetComponent<TitanGameComponent>();
            if (titanComp != null) titanComp.UpdatePeakExperimentCount();
            var herdTracker = Current.Game?.GetComponent<GameComponent_ThrumboHerdTracker>();
            if (herdTracker != null) herdTracker.TickHerd();
            Messages.Message(string.Format("Message_TITAN_SubjectRecruited".Translate(), target.LabelShort), target, MessageTypeDefOf.PositiveEvent);
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

            bool titanAlreadyOnMap = map.mapPawns.AllPawnsSpawned.Any(p =>
                !p.Dead && (p.def.defName == "TITAN_Warlord"
                         || p.def.defName == "TITAN_VoidWalker"
                         || p.def.defName == "TITAN_Matriarch"));
            if (titanAlreadyOnMap) return;

            Faction allyFaction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients);

            PawnKindDef leaderKind = candidates.RandomElement();
            IntVec3 spawnLoc;
            if (!RCellFinder.TryFindRandomPawnEntryCell(out spawnLoc, map, CellFinder.EdgeRoadChance_Friendly)) return;

            TitanPawnGuard.BeginAllowed();
            Pawn ally = PawnGenerator.GeneratePawn(leaderKind, null);
            TitanPawnGuard.EndAllowed();
            if (allyFaction != null) ally.SetFaction(allyFaction); else ally.SetFaction(null);

            HediffDef buffDef = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_TitanReinforcementBuff");
            if (buffDef != null)
            {
                Hediff buff = ally.health.GetOrAddHediff(buffDef);
                buff.Severity = 1.0f;
            }

            GenSpawn.Spawn(ally, spawnLoc, map, Rot4.Random);

            List<Thing> enemies = map.mapPawns.AllPawnsSpawned.Where(p => p.HostileTo(Faction.OfPlayer) && !p.Downed).Cast<Thing>().ToList();
            if (enemies.Count > 0 && ally.Faction != null)
            {
                LordMaker.MakeNewLord(ally.Faction, new LordJob_AssaultThings(Faction.OfPlayer, enemies), map, new List<Pawn> { ally });
            }

            Find.LetterStack.ReceiveLetter("Letter_TitanReinforcement".Translate(), string.Format("Letter_TitanReinforcement_Body".Translate(), ally.LabelShort), LetterDefOf.PositiveEvent, ally);
        }
    }

    // =============================================================
    // 5. 工具类与光环组件
    // =============================================================
    public static class TitanImplantUtils
    {
        private static readonly Dictionary<string, string> ExperimentMutagenMap = new Dictionary<string, string>
        {
            { "TITAN_No7_AcidThrumbo", "TITAN_Implant_CrimsonHeart" },
            { "TITAN_No13_SwampThrumbo", "TITAN_Implant_VoidMembrane" },
            { "TITAN_No26_ToyThrumbo", "TITAN_Implant_VerdantSpine" },
            { "TITAN_No42_AuroraThrumbo", "TITAN_Implant_VoidMembrane" },
            { "TITAN_No45_FireThrumbo", "TITAN_Implant_VerdantSpine" },
            { "TITAN_No50_DesertThrumbo", "TITAN_Implant_CrimsonHeart" },
            { "TITAN_No64_PrairieThrumbo", "TITAN_Implant_VoidMembrane" },
            { "TITAN_No88_JungleThrumbo", "TITAN_Implant_VerdantSpine" },
        };

        public static bool CanExperimentSubjectAcceptMutagen(Pawn pawn, HediffDef mutagenHediff)
        {
            string race = pawn.def.defName;
            if (!ExperimentMutagenMap.ContainsKey(race)) return false;
            return ExperimentMutagenMap[race] == mutagenHediff.defName;
        }

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

        public static bool CountsTowardResonance(Pawn p)
        {
            string kindDef = p.kindDef?.defName;
            if (string.IsNullOrEmpty(kindDef)) return false;
            if (kindDef == "TITAN_Spark" || kindDef == "TITAN_Warlord"
             || kindDef == "TITAN_VoidWalker" || kindDef == "TITAN_Matriarch"
             || kindDef == "TITAN_Hunter") return false;
            if (kindDef == "TITAN_ThrumboPrototype" || kindDef.StartsWith("TITAN_No")) return true;
            if (kindDef == "Thrumbo") return true;
            if (kindDef.StartsWith("NT_") || kindDef == "AlphaThrumbo") return true;
            return false;
        }

        public static bool IsThrumboKind(Pawn p)
        {
            string kindDef = p.kindDef?.defName;
            if (string.IsNullOrEmpty(kindDef)) return false;
            if (kindDef == "Thrumbo" || kindDef == "TITAN_ThrumboPrototype" || kindDef == "TITAN_Spark") return true;
            if (kindDef == "TITAN_Warlord" || kindDef == "TITAN_VoidWalker" || kindDef == "TITAN_Matriarch") return true;
            if (kindDef.StartsWith("TITAN_No")) return true;
            if (kindDef.StartsWith("NT_") || kindDef == "AlphaThrumbo") return true;
            return false;
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
            if (tickCounter++ % 300 == 0)
            {
                HediffDef buff = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_Buff_KingsGrace");
                if (buff != null)
                {
                    int radiusInt = (int)Props.radius;
                    CellRect rect = CellRect.CenteredOn(pawn.Position, radiusInt);
                    rect.ClipInsideMap(pawn.Map);
                    for (int z = rect.minZ; z <= rect.maxZ; z++)
                    {
                        for (int x = rect.minX; x <= rect.maxX; x++)
                        {
                            IntVec3 c = new IntVec3(x, 0, z);
                            if (!c.InHorDistOf(pawn.Position, Props.radius)) continue;
                            List<Thing> things = c.GetThingList(pawn.Map);
                            for (int i = 0; i < things.Count; i++)
                            {
                                Pawn p = things[i] as Pawn;
                                if (p != null && p != pawn && !p.Dead && p.Faction == pawn.Faction)
                                    p.health.GetOrAddHediff(buff).Severity = 1.0f;
                            }
                        }
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
            if (tickCounter++ % 300 == 0)
            {
                if (Props.selfBuffDef != null) owner.health.GetOrAddHediff(Props.selfBuffDef).Severity = 1.0f;
                if (Props.buffDef != null)
                {
                    int radiusInt = (int)Props.radius;
                    CellRect rect = CellRect.CenteredOn(owner.Position, radiusInt);
                    rect.ClipInsideMap(owner.Map);
                    for (int z = rect.minZ; z <= rect.maxZ; z++)
                    {
                        for (int x = rect.minX; x <= rect.maxX; x++)
                        {
                            IntVec3 c = new IntVec3(x, 0, z);
                            if (!c.InHorDistOf(owner.Position, Props.radius)) continue;
                            List<Thing> things = c.GetThingList(owner.Map);
                            for (int i = 0; i < things.Count; i++)
                            {
                                Pawn p = things[i] as Pawn;
                                if (p != null && p != owner && !p.Dead && p.Faction == owner.Faction)
                                    p.health.GetOrAddHediff(Props.buffDef).Severity = 1.0f;
                            }
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