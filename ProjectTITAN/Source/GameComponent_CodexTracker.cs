using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class GameComponent_CodexTracker : GameComponent
    {
        private HashSet<string> discoveredEntries = new HashSet<string>();
        private HashSet<string> achievedTiers = new HashSet<string>();

        public GameComponent_CodexTracker(Game game) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref discoveredEntries, "discoveredEntries", LookMode.Value);
            Scribe_Collections.Look(ref achievedTiers, "achievedTiers", LookMode.Value);
            if (discoveredEntries == null) discoveredEntries = new HashSet<string>();
            if (achievedTiers == null) achievedTiers = new HashSet<string>();
        }

        private static readonly HashSet<string> TitanPawnKinds = new HashSet<string>
        {
            "TITAN_ThrumboPrototype", "TITAN_Spark",
            "TITAN_No7_AcidThrumbo", "TITAN_No13_SwampThrumbo", "TITAN_No26_ToyThrumbo",
            "TITAN_No42_AuroraThrumbo", "TITAN_No45_FireThrumbo", "TITAN_No50_DesertThrumbo",
            "TITAN_No64_PrairieThrumbo", "TITAN_No88_JungleThrumbo"
        };

        public void RefreshDiscovery()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                {
                    if (p.Dead || p.kindDef == null) continue;
                    string kind = p.kindDef.defName;

                    if (TitanPawnKinds.Contains(kind))
                    {
                        EnsureName(p, kind);
                        if (p.Faction == Faction.OfPlayer)
                            DiscoverForKind(kind);
                    }
                }
            }
            var titanComp = Current.Game.GetComponent<TitanGameComponent>();
            if (titanComp != null)
            {
                if (titanComp.hasBefriendedWarlord) Discover("TITAN_Codex_Warlord");
                if (titanComp.hasBefriendedVoidWalker) Discover("TITAN_Codex_VoidWalker");
                if (titanComp.hasBefriendedMatriarch) Discover("TITAN_Codex_Matriarch");
            }
        }

        private void DiscoverForKind(string kind)
        {
            switch (kind)
            {
                case "TITAN_ThrumboPrototype": Discover("TITAN_Codex_No0"); break;
                case "TITAN_Spark": Discover("TITAN_Codex_Spark"); break;
                case "TITAN_No7_AcidThrumbo": Discover("TITAN_Codex_No7"); break;
                case "TITAN_No13_SwampThrumbo": Discover("TITAN_Codex_No13"); break;
                case "TITAN_No26_ToyThrumbo": Discover("TITAN_Codex_No26"); break;
                case "TITAN_No42_AuroraThrumbo": Discover("TITAN_Codex_No42"); break;
                case "TITAN_No45_FireThrumbo": Discover("TITAN_Codex_No45"); break;
                case "TITAN_No50_DesertThrumbo": Discover("TITAN_Codex_No50"); break;
                case "TITAN_No64_PrairieThrumbo": Discover("TITAN_Codex_No64"); break;
                case "TITAN_No88_JungleThrumbo": Discover("TITAN_Codex_No88"); break;
            }
        }

        public bool IsDiscovered(string defName)
        {
            return discoveredEntries.Contains(defName);
        }

        public void Discover(string defName)
        {
            if (discoveredEntries.Add(defName))
                TitanEvents.FireCodexDiscovered(defName);
        }

        public void DiscoverAll()
        {
            foreach (var def in DefDatabase<CodexEntryDef>.AllDefs)
            {
                if (def.status == CodexEntryStatus.Alive || def.status == CodexEntryStatus.Classified)
                    discoveredEntries.Add(def.defName);
            }
        }

        public void ResetAll()
        {
            discoveredEntries.Clear();
            achievedTiers.Clear();
        }

        public void ResetEntry(string defName)
        {
            discoveredEntries.Remove(defName);
        }

        public bool IsTierAchieved(string tierDefName)
        {
            return achievedTiers.Contains(tierDefName);
        }

        public void AchieveTier(string tierDefName)
        {
            achievedTiers.Add(tierDefName);
        }

        public IEnumerable<CodexEntryDef> GetAliveEntries()
        {
            return DefDatabase<CodexEntryDef>.AllDefs.Where(d => d.status == CodexEntryStatus.Alive);
        }

        public IEnumerable<CodexEntryDef> GetUndiscoveredAliveEntries()
        {
            return GetAliveEntries().Where(d => !IsDiscovered(d.defName));
        }

        public int DiscoveredCount => discoveredEntries.Count;
        public int TotalCount => DefDatabase<CodexEntryDef>.AllDefsListForReading.Count;

        private void EnsureName(Pawn p, string kindDefName)
        {
            if (!(p.Name is NameSingle ns)) return;
            bool needsFix = ns.Numerical;
            if (!needsFix)
            {
                string nameStr = ns.Name;
                if (Regex.IsMatch(nameStr, @"\d+$") && nameStr != GetProperName(kindDefName))
                    needsFix = true;
            }
            if (needsFix)
                p.Name = new NameSingle(GetProperName(kindDefName));
        }

        private string GetProperName(string kindDefName)
        {
            if (kindDefName == "TITAN_ThrumboPrototype") return "TITAN_Name_Prototype".Translate();
            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName);
            return kind != null ? kind.label : kindDefName;
        }
    }
}
