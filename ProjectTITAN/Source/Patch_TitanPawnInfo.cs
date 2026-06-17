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
    [StaticConstructorOnStartup]
    public static class TitanDescriptionInjector
    {
        static TitanDescriptionInjector()
        {
            foreach (var codexDef in DefDatabase<CodexEntryDef>.AllDefs)
            {
                if (string.IsNullOrEmpty(codexDef.pawnKindDef)) continue;
                if (string.IsNullOrEmpty(codexDef.descKey)) continue;

                PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(codexDef.pawnKindDef);
                if (kindDef?.race == null) continue;

                ThingDef thingDef = kindDef.race;
                thingDef.description = codexDef.descKey.Translate();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Patch_TitanGizmos
    {
        private static Texture2D cachedIcon;

        private static Texture2D GetCodexIcon()
        {
            if (cachedIcon == null)
            {
                ThingDef novel = DefDatabase<ThingDef>.GetNamedSilentFail("Schematic");
                if (novel != null) cachedIcon = novel.uiIcon;
                if (cachedIcon == null) cachedIcon = BaseContent.BadTex;
            }
            return cachedIcon;
        }

        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (Gizmo g in __result) yield return g;

            Pawn pawn = __instance;
            if (pawn.kindDef == null) yield break;

            // 支援神兽：返回虚空按钮
            HediffDef reinforcementBuff = DefDatabase<HediffDef>.GetNamedSilentFail("TITAN_TitanReinforcementBuff");
            if (reinforcementBuff != null && pawn.health?.hediffSet?.HasHediff(reinforcementBuff) == true && pawn.Map != null && !pawn.Dead)
            {
                Command_Action dismissCmd = new Command_Action
                {
                    defaultLabel = "TITAN_Gizmo_DismissTitan".Translate(),
                    defaultDesc = "TITAN_Gizmo_DismissTitanDesc".Translate(),
                    icon = TexCommand.RemoveRoutePlannerWaypoint,
                    action = () =>
                    {
                        if (pawn.Map == null || pawn.Dead) return;
                        Lord lord = pawn.GetLord();
                        if (lord != null) pawn.Map.lordManager.RemoveLord(lord);
                        LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Jog, true, true), pawn.Map, new List<Pawn> { pawn });
                        Messages.Message("Message_Titan_Dismissed".Translate(pawn.LabelShort), MessageTypeDefOf.NeutralEvent);
                    }
                };
                yield return dismissCmd;
            }

            // 图鉴按钮：仅对玩家阵营的已发现实验体
            if (pawn.Faction != Faction.OfPlayer) yield break;

            CodexEntryDef codexDef = DefDatabase<CodexEntryDef>.AllDefs
                .FirstOrDefault(d => d.pawnKindDef == pawn.kindDef.defName);
            if (codexDef == null) yield break;

            var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null || !tracker.IsDiscovered(codexDef.defName)) yield break;

            Command_Action cmd = new Command_Action
            {
                defaultLabel = "TITAN_Gizmo_ViewCodex".Translate(),
                defaultDesc = "TITAN_Gizmo_ViewCodexDesc".Translate(),
                icon = GetCodexIcon(),
                action = () => Find.WindowStack.Add(new Dialog_TitanCodexEntry(codexDef))
            };
            yield return cmd;
        }
    }

    public class Dialog_TitanCodexEntry : Window
    {
        private CodexEntryDef entry;
        private Vector2 scrollPosition;

        private static readonly Color ColorTitle = new Color(1f, 0.92f, 0.55f);
        private static readonly Color ColorStatusAlive = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color ColorStatusDead = new Color(0.9f, 0.35f, 0.35f);
        private static readonly Color ColorStatusClassified = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorQuote = new Color(0.75f, 0.8f, 0.85f);
        private static readonly Color ColorFeature = new Color(0.3f, 0.9f, 0.7f);
        private static readonly Color ColorRecruit = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ColorBody = new Color(0.88f, 0.88f, 0.88f);

        public override Vector2 InitialSize => new Vector2(520f, 640f);

        public Dialog_TitanCodexEntry(CodexEntryDef entry)
        {
            this.entry = entry;
            this.closeOnClickedOutside = true;
            this.doCloseX = true;
            this.draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float margin = 10f;
            float contentWidth = inRect.width - margin * 2f;
            float y = margin;

            Text.Font = GameFont.Medium;
            GUI.color = ColorTitle;
            Widgets.Label(new Rect(margin, y, contentWidth, 28f), entry.displayNumber ?? entry.defName);
            GUI.color = Color.white;
            y += 32f;

            Text.Font = GameFont.Small;
            string statusKey = entry.status == CodexEntryStatus.Alive ? "TITAN_Codex_Status_Alive"
                : entry.status == CodexEntryStatus.Deceased ? "TITAN_Codex_Status_Deceased"
                : "TITAN_Codex_Status_Classified";
            Color statColor = entry.status == CodexEntryStatus.Alive ? ColorStatusAlive
                : entry.status == CodexEntryStatus.Deceased ? ColorStatusDead
                : ColorStatusClassified;
            GUI.color = statColor;
            Widgets.Label(new Rect(margin, y, contentWidth, 20f), statusKey.Translate());
            GUI.color = Color.white;
            y += 24f;

            if (!string.IsNullOrEmpty(entry.labelKey))
            {
                GUI.color = new Color(0.7f, 0.7f, 0.75f);
                Widgets.Label(new Rect(margin, y, contentWidth, 20f), entry.labelKey.Translate());
                GUI.color = Color.white;
                y += 22f;
            }

            y += 4f;
            Widgets.DrawLine(new Vector2(margin, y), new Vector2(inRect.width - margin, y), new Color(0.4f, 0.4f, 0.45f, 0.5f), 1f);
            y += 6f;

            if (!string.IsNullOrEmpty(entry.descKey))
            {
                string desc = entry.descKey.Translate();
                float totalHeight = CalcDescHeight(desc, contentWidth - 18f);
                Rect viewRect = new Rect(margin, y, contentWidth, inRect.height - y - margin);
                Rect contentRect = new Rect(0f, 0f, contentWidth - 18f, totalHeight);

                Widgets.BeginScrollView(viewRect, ref scrollPosition, contentRect);
                try
                {
                    DrawColoredDescription(0f, 0f, contentWidth - 18f, desc);
                }
                finally
                {
                    Widgets.EndScrollView();
                }
            }

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        private float CalcDescHeight(string desc, float width)
        {
            float total = 0f;
            string[] lines = desc.Split('\n');
            foreach (string raw in lines)
            {
                string line = raw;
                if (string.IsNullOrEmpty(line)) { total += 8f; continue; }
                total += Text.CalcHeight(line, width);
            }
            return total + 10f;
        }

        private float DrawColoredDescription(float x, float y, float width, string desc)
        {
            string[] lines = desc.Split('\n');
            Color prevG = GUI.color;

            foreach (string raw in lines)
            {
                string line = raw;
                if (string.IsNullOrEmpty(line)) { y += 8f; continue; }

                float h = Text.CalcHeight(line, width);
                Rect lineRect = new Rect(x, y, width, h);

                string trimmed = raw.TrimStart();
                if (trimmed.StartsWith(">"))
                    GUI.color = ColorQuote;
                else if (trimmed.Contains("特性：") || trimmed.Contains("特性:") || trimmed.Contains("Features:") || trimmed.Contains("特徴：") || trimmed.Contains("特徴:") || trimmed.Contains("特殊："))
                    GUI.color = ColorFeature;
                else if (trimmed.Contains("【加入方式】") || trimmed.Contains("[How to recruit]") || trimmed.Contains("[勧誘方法]"))
                    GUI.color = ColorRecruit;
                else
                    GUI.color = ColorBody;

                Widgets.Label(lineRect, line);
                y += h;
            }

            GUI.color = prevG;
            return y;
        }
    }
}
