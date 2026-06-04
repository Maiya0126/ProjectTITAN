using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public static class ProjectTITAN_API
    {
        public static bool IsSubjectDiscovered(string defName)
        {
            var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
            return tracker != null && tracker.IsDiscovered(defName);
        }

        public static List<string> GetDiscoveredSubjects()
        {
            var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
            if (tracker == null) return new List<string>();
            return DefDatabase<CodexEntryDef>.AllDefs
                .Where(d => tracker.IsDiscovered(d.defName))
                .Select(d => d.defName)
                .ToList();
        }

        public static bool IsTitanOnMap()
        {
            if (Find.CurrentMap == null) return false;
            return Find.CurrentMap.mapPawns.SpawnedColonyAnimals
                .Any(p => p.kindDef?.defName == "TITAN_ThrumboPrototype");
        }

        public static int GetActiveTitanCount()
        {
            if (Find.CurrentMap == null) return 0;
            return Find.CurrentMap.mapPawns.SpawnedColonyAnimals
                .Count(p => p.kindDef?.defName?.StartsWith("TITAN_") == true);
        }

        public static bool IsGameComponentRegistered()
        {
            return Current.Game?.GetComponent<GameComponent_CodexTracker>() != null;
        }

        public static string GetSubjectStatus(string defName)
        {
            var def = DefDatabase<CodexEntryDef>.GetNamedSilentFail(defName);
            if (def == null) return "NotFound";
            var tracker = Current.Game?.GetComponent<GameComponent_CodexTracker>();
            bool discovered = tracker != null && tracker.IsDiscovered(defName);
            if (def.status == CodexEntryStatus.Deceased) return "Deceased";
            if (def.status == CodexEntryStatus.Classified && !discovered) return "Classified";
            if (discovered) return "Discovered";
            return "Undiscovered";
        }

        public static List<string> GetAllSubjectDefNames()
        {
            return DefDatabase<CodexEntryDef>.AllDefs
                .OrderBy(d => d.order)
                .Select(d => d.defName)
                .ToList();
        }

        public static bool IsNewThrumboLoaded()
        {
            return LoadedModManager.RunningMods
                .Any(m => m.PackageIdPlayerFacing == "BeckeSteamID.NewThrumbo");
        }
    }
}
