using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ProjectTITAN
{
    [StaticConstructorOnStartup]
    public static class TitanPawnGuard
    {
        private static readonly HashSet<string> GuardedPawnKinds = new HashSet<string>
        {
            "TITAN_ThrumboPrototype",
            "TITAN_Spark",
            "TITAN_No7_AcidThrumbo",
            "TITAN_No13_SwampThrumbo",
            "TITAN_No26_ToyThrumbo",
            "TITAN_No42_AuroraThrumbo",
            "TITAN_No45_FireThrumbo",
            "TITAN_No50_DesertThrumbo",
            "TITAN_No64_PrairieThrumbo",
            "TITAN_No88_JungleThrumbo",
            "TITAN_Warlord",
            "TITAN_VoidWalker",
            "TITAN_Matriarch",
            "TITAN_Hunter"
        };

        [ThreadStatic]
        private static int allowDepth;

        public static void BeginAllowed()
        {
            allowDepth++;
        }

        public static void EndAllowed()
        {
            if (allowDepth > 0)
                allowDepth--;
        }

        public static bool IsAllowed => allowDepth > 0;

        static TitanPawnGuard()
        {
            var harmony = new Harmony("com.projecttitan.pawnguard");
            var original = AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) });
            var prefix = AccessTools.Method(typeof(TitanPawnGuard), "Prefix_GeneratePawn");
            if (original != null)
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));
            Log.Message("[Project T.I.T.A.N.] PawnGuard loaded — TITAN pawns can only spawn via mod events.");
        }

        static bool Prefix_GeneratePawn(PawnGenerationRequest request, ref Pawn __result)
        {
            if (IsAllowed) return true;
            PawnKindDef kind = request.KindDef;
            if (kind != null && GuardedPawnKinds.Contains(kind.defName))
            {
                Log.Warning($"[Project T.I.T.A.N.] Blocked unauthorized spawn of {kind.defName}. Only mod events can spawn TITAN experiment subjects.");
                __result = null;
                return false;
            }
            return true;
        }
    }
}
