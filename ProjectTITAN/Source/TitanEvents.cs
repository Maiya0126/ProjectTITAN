using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    public static class TitanEvents
    {
        public static event Action<string> OnCodexEntryDiscovered;
        public static event Action<string> OnTitanEventTriggered;
        public static event Action<string> OnSubjectJoinedColony;

        internal static void FireCodexDiscovered(string defName)
        {
            try { OnCodexEntryDiscovered?.Invoke(defName); } catch { }
        }

        internal static void FireTitanEventTriggered(string incidentDefName)
        {
            try { OnTitanEventTriggered?.Invoke(incidentDefName); } catch { }
        }

        internal static void FireSubjectJoinedColony(string pawnKindDefName)
        {
            try { OnSubjectJoinedColony?.Invoke(pawnKindDefName); } catch { }
        }

        public static void ClearAll()
        {
            OnCodexEntryDiscovered = null;
            OnTitanEventTriggered = null;
            OnSubjectJoinedColony = null;
        }
    }
}
