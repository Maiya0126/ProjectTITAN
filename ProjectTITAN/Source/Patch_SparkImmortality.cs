using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;
using UnityEngine;

namespace ProjectTITAN
{
    [StaticConstructorOnStartup]
    public static class TITAN_Main
    {
        static TITAN_Main()
        {
            try
            {
                var harmony = new Harmony("com.maiya.projecttitan");
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                Log.Error("[ProjectTITAN] Harmony init failed: " + ex.ToString());
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_Pawn_Kill
    {
        static bool Prefix(Pawn __instance)
        {
            if (__instance == null || __instance.def == null) return true;

            string defName = __instance.def.defName;

            if (defName == "TITAN_Spark")
            {
                return HandleSparkDeath(__instance);
            }
            else if (defName == "TITAN_ThrumboPrototype")
            {
                return HandlePrototypeDeath(__instance);
            }
            else if (defName == "TITAN_Warlord" || defName == "TITAN_VoidWalker" || defName == "TITAN_Matriarch")
            {
                return HandleTitanDeath(__instance);
            }

            return true;
        }

        private static bool HandleSparkDeath(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TITAN_SparkReboot"))) return false;

            try
            {
                if (pawn.Map != null)
                {
                    GenExplosion.DoExplosion(pawn.Position, pawn.Map, 2.9f, DamageDefOf.Flame, pawn);
                }

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffs[i] is Hediff_Injury)
                    {
                        pawn.health.RemoveHediff(hediffs[i]);
                    }
                }

                Hediff reboot = HediffMaker.MakeHediff(HediffDef.Named("TITAN_SparkReboot"), pawn);
                pawn.health.AddHediff(reboot);

                Messages.Message("Message_Spark_LethalStrikeDetected".Translate(), pawn, MessageTypeDefOf.NeutralEvent);
            }
            catch (Exception ex)
            {
                Log.Error("[ProjectTITAN] Spark death handler error: " + ex.ToString());
            }

            return false;
        }

        private static bool HandlePrototypeDeath(Pawn pawn)
        {
            if (!pawn.Downed)
            {
                HealthUtility.DamageUntilDowned(pawn, false);
                Messages.Message("Message_Spark_CoreDamaged".Translate(), pawn, MessageTypeDefOf.NeutralEvent);
            }
            return false;
        }

        private static bool HandleTitanDeath(Pawn pawn)
        {
            try
            {
                if (pawn.Map != null)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "回归虚空", Color.cyan);
                }

                if (pawn.def.defName == "TITAN_Warlord")
                {
                    ThingDef drop = DefDatabase<ThingDef>.GetNamedSilentFail("TITAN_Techprint_Warlord");
                    if (drop != null && pawn.Map != null)
                    {
                        GenSpawn.Spawn(drop, pawn.Position, pawn.Map);
                    }
                    Messages.Message("Message_Warlord_LeftData".Translate(), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message(string.Format("Message_Hunter_EntityCollapsed".Translate(), pawn.LabelShort), MessageTypeDefOf.NegativeEvent);
                }

                if (!pawn.Destroyed)
                {
                    pawn.DeSpawn();
                }
            }
            catch (Exception ex)
            {
                Log.Error("[ProjectTITAN] Titan death handler error: " + ex.ToString());
            }

            return false;
        }
    }
}
