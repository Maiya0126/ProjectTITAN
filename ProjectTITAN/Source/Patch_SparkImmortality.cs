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
            Log.Message("[Project T.I.T.A.N.] Attribute patches will be applied by ProjectTitanMod.PatchAll.");
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
            if (pawn.health.hediffSet.HasHediff(HediffDef.Named("TITAN_PrototypeHibernation"))) return false;

            try
            {
                if (!pawn.Downed)
                {
                    HealthUtility.DamageUntilDowned(pawn, false);
                }

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int i = hediffs.Count - 1; i >= 0; i--)
                {
                    if (hediffs[i] is Hediff_Injury)
                    {
                        pawn.health.RemoveHediff(hediffs[i]);
                    }
                }

                Hediff hibernation = HediffMaker.MakeHediff(HediffDef.Named("TITAN_PrototypeHibernation"), pawn);
                pawn.health.AddHediff(hibernation);

                Messages.Message("Message_Prototype_HibernationStarted".Translate(), pawn, MessageTypeDefOf.NeutralEvent);
            }
            catch (Exception ex)
            {
                Log.Error("[ProjectTITAN] Prototype death handler error: " + ex.ToString());
            }

            return false;
        }

        private static bool HandleTitanDeath(Pawn pawn)
        {
            try
            {
                if (pawn.Map != null)
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TITAN_Mote_ReturnToVoid".Translate(), Color.cyan);
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
                else if (pawn.def.defName == "TITAN_VoidWalker" || pawn.def.defName == "TITAN_Matriarch")
                {
                    Messages.Message("Message_Titan_Vanished".Translate(pawn.LabelShort), MessageTypeDefOf.NegativeEvent);
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
