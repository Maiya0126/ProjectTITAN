using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTITAN
{
    public class MainTabWindow_Codex : MainTabWindow
    {
        private Vector2 scrollPosition;
        private Vector2 detailScrollPosition;
        private CodexEntryDef selectedEntry;

        private static readonly Color ColorAlive = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color ColorDeceased = new Color(0.8f, 0.2f, 0.2f);
        private static readonly Color ColorClassified = new Color(0.5f, 0.5f, 0.5f);
        private static readonly Color ColorLocked = new Color(0.3f, 0.3f, 0.3f);
        private static readonly Color ColorDiscoveredLabel = new Color(1f, 0.9f, 0.5f);
        private static readonly Color ColorFeature = new Color(0.3f, 0.9f, 0.7f);
        private static readonly Color ColorRecruit = new Color(1f, 0.85f, 0.3f);
        private static readonly Color ColorQuote = new Color(0.75f, 0.8f, 0.85f);
        private static readonly Color ColorBody = new Color(0.88f, 0.88f, 0.88f);
        private static readonly Color ColorStatusAlive = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color ColorStatusDead = new Color(0.9f, 0.35f, 0.35f);
        private static readonly Color ColorStatusClassified = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color ColorTitle = new Color(1f, 0.92f, 0.55f);

        private const float SlotSize = 90f;
        private const float SlotPadding = 10f;
        private const float Columns = 5f;

        public override Vector2 InitialSize => new Vector2(900f, 650f);

        public override void DoWindowContents(Rect inRect)
        {
            var tracker = Current.Game.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null) return;
            tracker.RefreshDiscovery();

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "TITAN_Codex_Title".Translate());
            Text.Font = GameFont.Small;

            float contentY = inRect.y + 35f;
            float contentHeight = inRect.height - 35f;
            float gridWidth = inRect.width * 0.6f;
            float detailWidth = inRect.width * 0.4f - 10f;

            Rect leftRect = new Rect(inRect.x, contentY, gridWidth, contentHeight);
            Rect rightRect = new Rect(inRect.x + gridWidth + 10f, contentY, detailWidth, contentHeight);

            var allEntries = DefDatabase<CodexEntryDef>.AllDefs.OrderBy(d => d.order).ToList();
            int columns = (int)Columns;
            int rows = Mathf.CeilToInt((float)allEntries.Count / columns);

            float viewHeight = rows * (SlotSize + SlotPadding) + 20f;
            Rect scrollContentRect = new Rect(0f, 0f, leftRect.width - 16f, viewHeight);

            Widgets.BeginScrollView(leftRect, ref scrollPosition, scrollContentRect);
            try
            {
                float gridOffsetX = (scrollContentRect.width - columns * (SlotSize + SlotPadding)) / 2f;

                int index = 0;
                foreach (var entry in allEntries)
                {
                    int col = index % columns;
                    int row = index / columns;
                    float x = gridOffsetX + col * (SlotSize + SlotPadding);
                    float y = row * (SlotSize + SlotPadding);
                    Rect slotRect = new Rect(x, y, SlotSize, SlotSize);
                    DrawSlot(slotRect, entry, tracker);
                    index++;
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            if (selectedEntry != null)
                DrawDetailPanel(rightRect, selectedEntry, tracker);
            else
            {
                Widgets.DrawBoxSolid(rightRect, new Color(0.08f, 0.08f, 0.1f, 0.5f));
                Widgets.DrawBox(rightRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rightRect, "TITAN_Codex_DataUndecrypted".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private Texture2D TryLoadPawnTexture(string pawnKindDefName)
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            if (kindDef == null) return null;
            var lifeStage = kindDef.lifeStages?.LastOrDefault();
            if (lifeStage?.bodyGraphicData == null) return null;
            string basePath = lifeStage.bodyGraphicData.texPath;
            Texture2D tex = ContentFinder<Texture2D>.Get(basePath + "_east", false);
            if (tex != null) return tex;
            tex = ContentFinder<Texture2D>.Get(basePath + "_south", false);
            if (tex != null) return tex;
            tex = ContentFinder<Texture2D>.Get(basePath + "_north", false);
            if (tex != null) return tex;
            tex = ContentFinder<Texture2D>.Get(basePath, false);
            return tex;
        }

        private Material TryGetPawnMaterial(string pawnKindDefName)
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            if (kindDef == null) return null;
            var graphic = kindDef.lifeStages?.LastOrDefault()?.bodyGraphicData?.Graphic;
            if (graphic == null) return null;
            return graphic.MatAt(Rot4.South);
        }

        private float GetDrawScaleForPawnKind(string pawnKindDefName)
        {
            if (string.IsNullOrEmpty(pawnKindDefName)) return 1f;
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail(pawnKindDefName);
            if (kindDef == null) return 1f;
            var lifeStage = kindDef.lifeStages?.LastOrDefault();
            if (lifeStage?.bodyGraphicData == null) return 1f;
            float drawSize = lifeStage.bodyGraphicData.drawSize.x;
            if (drawSize <= 0f) return 1f;
            float reference = 4.8f;
            return Mathf.Clamp(drawSize / reference, 0.25f, 1f);
        }

        private void DrawSlot(Rect rect, CodexEntryDef entry, GameComponent_CodexTracker tracker)
        {
            bool discovered = tracker.IsDiscovered(entry.defName);
            bool visible = discovered || entry.status == CodexEntryStatus.Deceased;
            bool isSpark = entry.pawnKindDef == "TITAN_Spark";
            Color bgColor;

            if (!discovered && entry.status == CodexEntryStatus.Alive)
                bgColor = ColorLocked;
            else if (entry.status == CodexEntryStatus.Deceased)
                bgColor = ColorDeceased;
            else if (entry.status == CodexEntryStatus.Classified && !discovered)
                bgColor = ColorClassified;
            else
                bgColor = ColorAlive;

            Widgets.DrawBoxSolid(rect, bgColor * 0.3f);
            Widgets.DrawBox(rect);

            if (visible)
            {
                float drawScale = GetDrawScaleForPawnKind(entry.pawnKindDef);
                if (entry.status == CodexEntryStatus.Deceased)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Small;
                    Color prev = GUI.color;
                    GUI.color = new Color(0.95f, 0.4f, 0.4f);
                    Widgets.Label(rect, entry.displayNumber ?? entry.defName);
                    Text.Font = GameFont.Tiny;
                    GUI.color = new Color(0.7f, 0.25f, 0.25f);
                    Rect xRect = new Rect(rect.x, rect.yMax - 20f, rect.width, 20f);
                    Widgets.Label(xRect, "✗");
                    GUI.color = prev;
                    if (Widgets.ButtonInvisible(rect, false))
                        selectedEntry = entry;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else if (!string.IsNullOrEmpty(entry.pawnKindDef))
                {
                    Texture2D tex = TryLoadPawnTexture(entry.pawnKindDef);
                    bool drew = false;
                    if (tex != null)
                    {
                        Rect texRect = rect;
                        if (drawScale < 1f)
                        {
                            float sz = rect.width * drawScale;
                            float off = (rect.width - sz) / 2f;
                            texRect = new Rect(rect.x + off, rect.y + off, sz, sz);
                        }
                        if (isSpark)
                        {
                            Material mat = TryGetPawnMaterial(entry.pawnKindDef);
                            if (mat != null)
                            {
                                Graphics.DrawTexture(texRect, tex, mat);
                                drew = true;
                            }
                        }
                        if (!drew)
                        {
                            Widgets.DrawTextureFitted(texRect, tex, 1f);
                            drew = true;
                        }
                    }
                    if (drew)
                    {
                        Text.Anchor = TextAnchor.LowerCenter;
                        Text.Font = GameFont.Tiny;
                        Rect lRect = new Rect(rect.x, rect.yMax - 18f, rect.width, 18f);
                        Widgets.DrawBoxSolid(lRect, new Color(0, 0, 0, 0.65f));
                        Color prev = GUI.color;
                        GUI.color = new Color(1f, 0.95f, 0.7f, 0.9f);
                        Widgets.Label(lRect, entry.displayNumber ?? entry.defName);
                        GUI.color = prev;
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                    else
                    {
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Text.Font = GameFont.Small;
                        Color prev = GUI.color;
                        GUI.color = ColorDiscoveredLabel;
                        Widgets.Label(rect, entry.displayNumber ?? entry.defName);
                        GUI.color = prev;
                        Text.Font = GameFont.Small;
                        Text.Anchor = TextAnchor.UpperLeft;
                    }
                    if (Widgets.ButtonInvisible(rect, false))
                        selectedEntry = entry;
                }
                else
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Small;
                    Color prev = GUI.color;
                    GUI.color = ColorDiscoveredLabel;
                    Widgets.Label(rect, entry.displayNumber ?? entry.defName);
                    GUI.color = prev;
                    if (Widgets.ButtonInvisible(rect, false))
                        selectedEntry = entry;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
            else
            {
                bool drewSil = false;
                if (!string.IsNullOrEmpty(entry.pawnKindDef))
                {
                    Texture2D tex = TryLoadPawnTexture(entry.pawnKindDef);
                    if (tex != null)
                    {
                        float drawScale = GetDrawScaleForPawnKind(entry.pawnKindDef);
                        Rect silRect = rect;
                        if (drawScale < 1f)
                        {
                            float sz = rect.width * drawScale;
                            float silOff = (rect.width - sz) / 2f;
                            silRect = new Rect(rect.x + silOff, rect.y + silOff, sz, sz);
                        }
                        Color prev = GUI.color;
                        GUI.color = Color.black;
                        Widgets.DrawTextureFitted(silRect, tex, 1f);
                        Color glow = new Color(0.15f, 0.25f, 0.4f, 0.15f);
                        float off = 1.5f;
                        GUI.color = glow;
                        Widgets.DrawTextureFitted(new Rect(silRect.x - off, silRect.y, silRect.width, silRect.height), tex, 1f);
                        Widgets.DrawTextureFitted(new Rect(silRect.x + off, silRect.y, silRect.width, silRect.height), tex, 1f);
                        Widgets.DrawTextureFitted(new Rect(silRect.x, silRect.y - off, silRect.width, silRect.height), tex, 1f);
                        Widgets.DrawTextureFitted(new Rect(silRect.x, silRect.y + off, silRect.width, silRect.height), tex, 1f);
                        GUI.color = prev;
                        drewSil = true;
                    }
                }
                if (!drewSil)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    Color prev = GUI.color;
                    GUI.color = new Color(0.4f, 0.4f, 0.4f);
                    Widgets.Label(rect, "?");
                    GUI.color = prev;
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                if (Widgets.ButtonInvisible(rect, false))
                    selectedEntry = entry;
            }
        }

        private void DrawDetailPanel(Rect rect, CodexEntryDef entry, GameComponent_CodexTracker tracker)
        {
            bool discovered = tracker.IsDiscovered(entry.defName) || entry.status == CodexEntryStatus.Deceased;

            Widgets.DrawBoxSolid(rect, new Color(0.07f, 0.07f, 0.1f, 0.92f));
            Widgets.DrawBox(rect);

            float margin = 8f;
            float scrollViewWidth = rect.width - margin * 2f;
            float contentWidth = scrollViewWidth - 18f;

            float totalHeight = CalcDetailHeight(entry, discovered, contentWidth);
            Rect viewRect = new Rect(rect.x + margin, rect.y + margin, scrollViewWidth, rect.height - margin * 2f);
            Rect contentRect = new Rect(0f, 0f, contentWidth, totalHeight);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            Widgets.BeginScrollView(viewRect, ref detailScrollPosition, contentRect);
            try
            {
                float y = 4f;
                Text.Font = GameFont.Medium;
                GUI.color = ColorTitle;
                Widgets.Label(new Rect(0f, y, contentWidth, 28f), entry.displayNumber ?? entry.defName);
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
                Widgets.Label(new Rect(0f, y, contentWidth, 20f), statusKey.Translate());
                GUI.color = Color.white;
                y += 24f;

                if (discovered && !string.IsNullOrEmpty(entry.labelKey))
                {
                    GUI.color = new Color(0.7f, 0.7f, 0.75f);
                    Widgets.Label(new Rect(0f, y, contentWidth, 20f), entry.labelKey.Translate());
                    GUI.color = Color.white;
                    y += 22f;
                }

                y += 4f;
                Widgets.DrawLine(new Vector2(0f, y), new Vector2(contentWidth, y), new Color(0.4f, 0.4f, 0.45f, 0.5f), 1f);
                y += 6f;

                if (discovered && !string.IsNullOrEmpty(entry.descKey))
                {
                    string desc = entry.descKey.Translate();
                    y = DrawColoredDescription(0f, y, contentWidth, desc);
                }
                else
                {
                    string hintText;
                    if (!discovered && !string.IsNullOrEmpty(entry.hintKey))
                        hintText = entry.hintKey.Translate();
                    else if (!discovered && !string.IsNullOrEmpty(entry.pawnKindDef))
                        hintText = "TITAN_Codex_EventHint".Translate();
                    else
                        hintText = "TITAN_Codex_DataUndecrypted".Translate();

                    GUI.color = new Color(0.6f, 0.6f, 0.65f);
                    float h = Text.CalcHeight(hintText, contentWidth);
                    Widgets.Label(new Rect(0f, y, contentWidth, h), hintText);
                    GUI.color = Color.white;
                    y += h + 6f;
                }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }

        private float CalcDetailHeight(CodexEntryDef entry, bool discovered, float width)
        {
            float y = 4f + 32f + 24f;
            if (discovered && !string.IsNullOrEmpty(entry.labelKey))
                y += 22f;
            y += 10f;

            if (discovered && !string.IsNullOrEmpty(entry.descKey))
            {
                string desc = entry.descKey.Translate();
                y += CalcColoredDescHeight(desc, width);
            }
            else
            {
                string hintText = null;
                if (!discovered && !string.IsNullOrEmpty(entry.hintKey))
                    hintText = entry.hintKey.Translate();
                else if (!discovered && !string.IsNullOrEmpty(entry.pawnKindDef))
                    hintText = "TITAN_Codex_EventHint".Translate();
                else
                    hintText = "TITAN_Codex_DataUndecrypted".Translate();
                y += Text.CalcHeight(hintText, width) + 6f;
            }

            y += 10f;
            return y;
        }

        private float CalcColoredDescHeight(string desc, float width)
        {
            string[] lines = desc.Split('\n');
            float total = 0f;
            foreach (string raw in lines)
            {
                string line = raw;
                if (string.IsNullOrEmpty(line))
                {
                    total += 8f;
                    continue;
                }
                total += Text.CalcHeight(line, width);
            }
            return total;
        }

        private float DrawColoredDescription(float x, float y, float width, string desc)
        {
            string[] lines = desc.Split('\n');
            Color prevG = GUI.color;

            foreach (string raw in lines)
            {
                string line = raw;
                if (string.IsNullOrEmpty(line))
                {
                    y += 8f;
                    continue;
                }

                float h = Text.CalcHeight(line, width);
                Rect lineRect = new Rect(x, y, width, h);

                string trimmed = raw.TrimStart();
                if (trimmed.StartsWith(">"))
                {
                    GUI.color = ColorQuote;
                }
                else if (trimmed.Contains("特性：") || trimmed.Contains("特性:") || trimmed.Contains("Features:") || trimmed.Contains("特徴：") || trimmed.Contains("特徴:") || trimmed.Contains("特殊："))
                {
                    GUI.color = ColorFeature;
                }
                else if (trimmed.Contains("【加入方式】") || trimmed.Contains("[How to recruit]") || trimmed.Contains("[勧誘方法]"))
                {
                    GUI.color = ColorRecruit;
                }
                else
                {
                    GUI.color = ColorBody;
                }

                Widgets.Label(lineRect, line);
                y += h;
            }

            GUI.color = prevG;
            return y;
        }
    }
}
