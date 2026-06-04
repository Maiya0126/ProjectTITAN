using System;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public class MainButtonWorker_Codex : MainButtonWorker
    {
        public override bool Visible
        {
            get
            {
                var settings = TITAN_CodexMod.Settings;
                if (settings != null && !settings.showCodexButton) return false;
                return MapHasTamedPrototype();
            }
        }

        public override void Activate()
        {
            Find.MainTabsRoot.SetCurrentTab(def);
        }

        private bool MapHasTamedPrototype()
        {
            if (Find.CurrentMap == null) return false;
            foreach (var pawn in Find.CurrentMap.mapPawns.SpawnedColonyAnimals)
            {
                if (pawn.kindDef?.defName == "TITAN_ThrumboPrototype") return true;
            }
            return false;
        }
    }
}