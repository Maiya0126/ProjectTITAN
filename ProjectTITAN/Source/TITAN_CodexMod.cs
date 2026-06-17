using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace ProjectTITAN
{
    public class TITAN_CodexSettings : ModSettings
    {
        public bool showCodexButton = true;
        public float companionChance = 0.5f;
        public float thrumboAttractChance = 0.2f;
        public float alphaThrumboReplaceChance = 0.15f;

        public float incidentChance_Matriarch = 0.30f;
        public float incidentChance_Warlord = 0.30f;
        public float incidentChance_VoidWalker = 0.30f;
        public float incidentChance_No7 = 0.30f;
        public float incidentChance_No13 = 0.30f;
        public float incidentChance_No26 = 1.0f;
        public float incidentChance_No42 = 0.30f;
        public float incidentChance_No45 = 0.30f;
        public float incidentChance_No50 = 0.30f;
        public float incidentChance_No64 = 0.30f;
        public float incidentChance_No88 = 0.30f;
        public float incidentChance_HunterAttack = 0.20f;

        public int incidentCooldown_Matriarch = 30;
        public int incidentCooldown_Warlord = 30;
        public int incidentCooldown_VoidWalker = 30;
        public int incidentCooldown_No7 = 15;
        public int incidentCooldown_No13 = 15;
        public int incidentCooldown_No26 = 5;
        public int incidentCooldown_No42 = 15;
        public int incidentCooldown_No45 = 15;
        public int incidentCooldown_No50 = 15;
        public int incidentCooldown_No64 = 15;
        public int incidentCooldown_No88 = 15;
        public int incidentCooldown_HunterAttack = 25;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref showCodexButton, "showCodexButton", true);
            Scribe_Values.Look(ref companionChance, "companionChance", 0.5f);
            Scribe_Values.Look(ref thrumboAttractChance, "thrumboAttractChance", 0.2f);
            Scribe_Values.Look(ref alphaThrumboReplaceChance, "alphaThrumboReplaceChance", 0.15f);
            Scribe_Values.Look(ref incidentChance_Matriarch, "incidentChance_Matriarch", 0.30f);
            Scribe_Values.Look(ref incidentChance_Warlord, "incidentChance_Warlord", 0.30f);
            Scribe_Values.Look(ref incidentChance_VoidWalker, "incidentChance_VoidWalker", 0.30f);
            Scribe_Values.Look(ref incidentChance_No7, "incidentChance_No7", 0.30f);
            Scribe_Values.Look(ref incidentChance_No13, "incidentChance_No13", 0.30f);
            Scribe_Values.Look(ref incidentChance_No26, "incidentChance_No26", 1.0f);
            Scribe_Values.Look(ref incidentChance_No42, "incidentChance_No42", 0.30f);
            Scribe_Values.Look(ref incidentChance_No45, "incidentChance_No45", 0.30f);
            Scribe_Values.Look(ref incidentChance_No50, "incidentChance_No50", 0.30f);
            Scribe_Values.Look(ref incidentChance_No64, "incidentChance_No64", 0.30f);
            Scribe_Values.Look(ref incidentChance_No88, "incidentChance_No88", 0.30f);
            Scribe_Values.Look(ref incidentChance_HunterAttack, "incidentChance_HunterAttack", 0.20f);
            Scribe_Values.Look(ref incidentCooldown_Matriarch, "incidentCooldown_Matriarch", 30);
            Scribe_Values.Look(ref incidentCooldown_Warlord, "incidentCooldown_Warlord", 30);
            Scribe_Values.Look(ref incidentCooldown_VoidWalker, "incidentCooldown_VoidWalker", 30);
            Scribe_Values.Look(ref incidentCooldown_No7, "incidentCooldown_No7", 15);
            Scribe_Values.Look(ref incidentCooldown_No13, "incidentCooldown_No13", 15);
            Scribe_Values.Look(ref incidentCooldown_No26, "incidentCooldown_No26", 5);
            Scribe_Values.Look(ref incidentCooldown_No42, "incidentCooldown_No42", 15);
            Scribe_Values.Look(ref incidentCooldown_No45, "incidentCooldown_No45", 15);
            Scribe_Values.Look(ref incidentCooldown_No50, "incidentCooldown_No50", 15);
            Scribe_Values.Look(ref incidentCooldown_No64, "incidentCooldown_No64", 15);
            Scribe_Values.Look(ref incidentCooldown_No88, "incidentCooldown_No88", 15);
            Scribe_Values.Look(ref incidentCooldown_HunterAttack, "incidentCooldown_HunterAttack", 25);
        }
    }

    public class TITAN_CodexMod : Mod
    {
        public static TITAN_CodexSettings Settings;

        public TITAN_CodexMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TITAN_CodexSettings>();
        }

        public override string SettingsCategory() => "TITAN_Settings_Category".Translate();

        private Vector2 settingsScrollPos;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            Rect scrollRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            float contentHeight = 1400f;
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, contentHeight);
            Widgets.BeginScrollView(scrollRect, ref settingsScrollPos, viewRect);
            listing.Begin(viewRect);

            listing.Label("TITAN_Settings_General".Translate());
            listing.CheckboxLabeled("TITAN_Settings_ShowCodexButton".Translate(), ref Settings.showCodexButton);

            bool ntLoaded = IsNewThrumboLoaded();
            if (ntLoaded)
            {
                listing.Label("TITAN_Settings_CompanionChance".Translate(Settings.companionChance.ToString("P0")));
                Settings.companionChance = listing.Slider(Settings.companionChance, 0f, 1f);
            }
            else
            {
                listing.Label("TITAN_Settings_CompanionChance_NTRequired".Translate());
                GUI.enabled = false;
                listing.Slider(0f, 0f, 1f);
                GUI.enabled = true;
            }

            listing.Label("TITAN_Settings_ThrumboAttractChance".Translate(Settings.thrumboAttractChance.ToString("P0")));
            Settings.thrumboAttractChance = listing.Slider(Settings.thrumboAttractChance, 0f, 1f);

            bool odysseyActive = ModsConfig.IsActive("Ludeon.RimWorld.Odyssey");
            if (odysseyActive)
            {
                listing.Label("TITAN_Settings_AlphaReplaceChance".Translate(Settings.alphaThrumboReplaceChance.ToString("P0")));
                Settings.alphaThrumboReplaceChance = listing.Slider(Settings.alphaThrumboReplaceChance, 0f, 1f);
            }
            else
            {
                listing.Label("TITAN_Settings_AlphaReplaceChance_OdysseyRequired".Translate());
                GUI.enabled = false;
                listing.Slider(0f, 0f, 1f);
                GUI.enabled = true;
            }

            listing.GapLine();
            listing.Label("TITAN_Settings_Events".Translate());

            float headerH = 20f;
            float rowH = 32f;
            int totalRows = 12;
            float tableHeight = headerH + totalRows * (rowH + 2f);
            Rect tableRect = listing.GetRect(tableHeight);

            float col0 = tableRect.width * 0.30f;
            float col1 = tableRect.width * 0.25f;
            float col2 = tableRect.width * 0.25f;
            float col3 = tableRect.width * 0.20f;

            float ty = tableRect.y;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(tableRect.x, ty, col0, headerH), "TITAN_Settings_EventName".Translate());
            Widgets.Label(new Rect(tableRect.x + col0, ty, col1, headerH), "TITAN_Settings_Probability".Translate());
            Widgets.Label(new Rect(tableRect.x + col0 + col1, ty, col2, headerH), "TITAN_Settings_CooldownDays".Translate());
            if (Prefs.DevMode)
                Widgets.Label(new Rect(tableRect.x + col0 + col1 + col2, ty, col3, headerH), "TITAN_Settings_Trigger".Translate());
            ty += headerH;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "Matriarch", ref Settings.incidentChance_Matriarch, ref Settings.incidentCooldown_Matriarch, "TITAN_Incident_MatriarchCrash");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "Warlord", ref Settings.incidentChance_Warlord, ref Settings.incidentCooldown_Warlord, "TITAN_Incident_WarlordRaid");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "VoidWalker", ref Settings.incidentChance_VoidWalker, ref Settings.incidentCooldown_VoidWalker, "TITAN_Incident_VoidWalkerRescue");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No7", ref Settings.incidentChance_No7, ref Settings.incidentCooldown_No7, "TITAN_Incident_No7");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No13", ref Settings.incidentChance_No13, ref Settings.incidentCooldown_No13, "TITAN_Incident_No13");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No26", ref Settings.incidentChance_No26, ref Settings.incidentCooldown_No26, "TITAN_Incident_No26_FirstEncounter");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No42", ref Settings.incidentChance_No42, ref Settings.incidentCooldown_No42, "TITAN_Incident_No42");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No45", ref Settings.incidentChance_No45, ref Settings.incidentCooldown_No45, "TITAN_Incident_No45");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No50", ref Settings.incidentChance_No50, ref Settings.incidentCooldown_No50, "TITAN_Incident_No50");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No64", ref Settings.incidentChance_No64, ref Settings.incidentCooldown_No64, "TITAN_Incident_No64");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "No88", ref Settings.incidentChance_No88, ref Settings.incidentCooldown_No88, "TITAN_Incident_No88");
            DrawEventTableRow(ref ty, tableRect.x, col0, col1, col2, col3, rowH,
                "HunterAttack", ref Settings.incidentChance_HunterAttack, ref Settings.incidentCooldown_HunterAttack, "TITAN_Incident_HunterAttack");

            if (Prefs.DevMode)
            {
            listing.GapLine();
                listing.Label("TITAN_Settings_CodexManagement".Translate());
                if (listing.ButtonText("TITAN_Settings_UnlockAllCodex".Translate()))
                {
                    var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
                    if (tracker != null)
                    {
                        tracker.DiscoverAll();
                        Messages.Message("TITAN_Codex_AllUnlocked".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                }
                GUI.color = new Color(1f, 0.8f, 0.2f);
                listing.Label("TITAN_Codex_ResetWarning".Translate());
                GUI.color = Color.white;
                if (listing.ButtonText("TITAN_Settings_ResetAllCodex".Translate()))
                {
                    var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
                    if (tracker != null)
                    {
                        tracker.ResetAll();
                        Messages.Message("TITAN_Codex_AllReset".Translate(), MessageTypeDefOf.NeutralEvent);
                    }
                }
            }

            listing.End();
            Settings.Write();
            ApplySettingsToIncidentDefs();
            Widgets.EndScrollView();
        }

        private void DrawEventTableRow(ref float y, float x, float col0, float col1, float col2, float col3, float rowH,
            string eventKey, ref float chance, ref int cooldown, string incidentDefName)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(x, y + 4f, col0, rowH - 4f), eventKey.Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            Rect chanceRect = new Rect(x + col0, y + 2f, col1, rowH * 0.45f);
            chance = Widgets.HorizontalSlider(chanceRect, chance, 0f, 1f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x + col0, y + rowH * 0.5f, col1, rowH * 0.5f), chance.ToString("P0"));
            Text.Anchor = TextAnchor.UpperLeft;

            Rect coolRect = new Rect(x + col0 + col1, y + 2f, col2, rowH * 0.45f);
            cooldown = (int)Widgets.HorizontalSlider(coolRect, cooldown, 5, 120);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(x + col0 + col1, y + rowH * 0.5f, col2, rowH * 0.5f), cooldown + " d");
            Text.Anchor = TextAnchor.UpperLeft;

            if (Prefs.DevMode)
            {
                float btnW = col3 * 0.55f;
                float rstW = col3 * 0.4f;
                float btnX = x + col0 + col1 + col2;
                if (Widgets.ButtonText(new Rect(btnX, y + 6f, btnW, rowH - 12f), "TITAN_Settings_Trigger".Translate()))
                    DevTriggerIncident(incidentDefName, false);
                if (Widgets.ButtonText(new Rect(btnX + btnW + 2f, y + 6f, rstW, rowH - 12f), "TITAN_Settings_ResetTrigger".Translate()))
                    DevTriggerIncident(incidentDefName, true);
            }

            y += rowH + 2f;
        }

        private void DevTriggerIncident(string incidentDefName, bool resetFirst = false)
        {
            if (string.IsNullOrEmpty(incidentDefName)) return;

            var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();

            if (resetFirst && tracker != null)
            {
                string codexDefName = IncidentToCodexDef(incidentDefName);
                if (!string.IsNullOrEmpty(codexDefName))
                {
                    tracker.ResetEntry(codexDefName);
                    Messages.Message("TITAN_Settings_CodexResetForReentry".Translate(), MessageTypeDefOf.NeutralEvent);
                }
            }

            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Messages.Message($"Incident {incidentDefName} not found", MessageTypeDefOf.RejectInput);
                return;
            }
            var parms2 = StorytellerUtility.DefaultParmsNow(incidentDef.category, Find.CurrentMap);
            if (incidentDef.Worker.TryExecute(parms2))
                Messages.Message($"Triggered: {incidentDefName}", MessageTypeDefOf.PositiveEvent);
            else
                Messages.Message($"Failed to trigger: {incidentDefName}", MessageTypeDefOf.RejectInput);
        }

        private static readonly Dictionary<string, string> IncidentCodexMap = new Dictionary<string, string>
        {
            { "TITAN_Incident_No7", "TITAN_Codex_No7" },
            { "TITAN_Incident_No13", "TITAN_Codex_No13" },
            { "TITAN_Incident_No26_FirstEncounter", "TITAN_Codex_No26" },
            { "TITAN_Incident_No42", "TITAN_Codex_No42" },
            { "TITAN_Incident_No45", "TITAN_Codex_No45" },
            { "TITAN_Incident_No50", "TITAN_Codex_No50" },
            { "TITAN_Incident_No64", "TITAN_Codex_No64" },
            { "TITAN_Incident_No88", "TITAN_Codex_No88" },
            { "TITAN_Incident_MatriarchCrash", "TITAN_Codex_Matriarch" },
            { "TITAN_Incident_WarlordRaid", "TITAN_Codex_Warlord" },
            { "TITAN_Incident_VoidWalkerRescue", "TITAN_Codex_VoidWalker" },
        };

        private string IncidentToCodexDef(string incidentDefName)
        {
            if (string.IsNullOrEmpty(incidentDefName)) return null;
            string result;
            IncidentCodexMap.TryGetValue(incidentDefName, out result);
            return result;
        }

        public static bool IsNewThrumboLoaded()
        {
            return LoadedModManager.RunningMods.Any(m => m.PackageIdPlayerFacing == "BeckeSteamID.NewThrumbo");
        }

        private static readonly Dictionary<string, Func<float>> IncidentChanceMap = new Dictionary<string, Func<float>>
        {
            { "TITAN_Incident_MatriarchCrash", () => Settings.incidentChance_Matriarch },
            { "TITAN_Incident_WarlordRaid", () => Settings.incidentChance_Warlord },
            { "TITAN_Incident_VoidWalkerRescue", () => Settings.incidentChance_VoidWalker },
            { "TITAN_Incident_No7", () => Settings.incidentChance_No7 },
            { "TITAN_Incident_No13", () => Settings.incidentChance_No13 },
            { "TITAN_Incident_No26_FirstEncounter", () => Settings.incidentChance_No26 },
            { "TITAN_Incident_No42", () => Settings.incidentChance_No42 },
            { "TITAN_Incident_No45", () => Settings.incidentChance_No45 },
            { "TITAN_Incident_No50", () => Settings.incidentChance_No50 },
            { "TITAN_Incident_No64", () => Settings.incidentChance_No64 },
            { "TITAN_Incident_No88", () => Settings.incidentChance_No88 },
            { "TITAN_Incident_HunterAttack", () => Settings.incidentChance_HunterAttack },
        };

        private static readonly Dictionary<string, Func<int>> IncidentCooldownMap = new Dictionary<string, Func<int>>
        {
            { "TITAN_Incident_MatriarchCrash", () => Settings.incidentCooldown_Matriarch },
            { "TITAN_Incident_WarlordRaid", () => Settings.incidentCooldown_Warlord },
            { "TITAN_Incident_VoidWalkerRescue", () => Settings.incidentCooldown_VoidWalker },
            { "TITAN_Incident_No7", () => Settings.incidentCooldown_No7 },
            { "TITAN_Incident_No13", () => Settings.incidentCooldown_No13 },
            { "TITAN_Incident_No26_FirstEncounter", () => Settings.incidentCooldown_No26 },
            { "TITAN_Incident_No42", () => Settings.incidentCooldown_No42 },
            { "TITAN_Incident_No45", () => Settings.incidentCooldown_No45 },
            { "TITAN_Incident_No50", () => Settings.incidentCooldown_No50 },
            { "TITAN_Incident_No64", () => Settings.incidentCooldown_No64 },
            { "TITAN_Incident_No88", () => Settings.incidentCooldown_No88 },
            { "TITAN_Incident_HunterAttack", () => Settings.incidentCooldown_HunterAttack },
        };

        public static void ApplySettingsToIncidentDefs()
        {
            if (Settings == null) return;
            foreach (var kv in IncidentChanceMap)
            {
                IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(kv.Key);
                if (def != null) def.baseChance = kv.Value();
            }
            foreach (var kv in IncidentCooldownMap)
            {
                IncidentDef def = DefDatabase<IncidentDef>.GetNamedSilentFail(kv.Key);
                if (def != null) def.minRefireDays = kv.Value();
            }
        }
    }
}