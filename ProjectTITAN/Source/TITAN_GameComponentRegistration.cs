using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace ProjectTITAN
{
    [StaticConstructorOnStartup]
    public static class TITAN_GameComponentRegistration
    {
        private static bool registered = false;

        static TITAN_GameComponentRegistration()
        {
            var harmony = new Harmony("com.projecttitan.gamecomponents");

            harmony.Patch(AccessTools.Method(typeof(Game), "FinalizeInit"),
                postfix: new HarmonyMethod(typeof(TITAN_GameComponentRegistration), "Postfix_FinalizeInit"));

            harmony.Patch(AccessTools.Method(typeof(Game), "LoadGame"),
                postfix: new HarmonyMethod(typeof(TITAN_GameComponentRegistration), "Postfix_LoadGame"));

            harmony.Patch(AccessTools.Method(typeof(Game), "UpdatePlay"),
                prefix: new HarmonyMethod(typeof(TITAN_GameComponentRegistration), "Prefix_UpdatePlay"));
        }

        static void Postfix_FinalizeInit(Game __instance)
        {
            RegisterAll(__instance);
        }

        static void Postfix_LoadGame(Game __instance)
        {
            RegisterAll(__instance);
        }

        static void Prefix_UpdatePlay(Game __instance)
        {
            if (registered) return;
            RegisterAll(__instance);
        }

        static void RegisterAll(Game game)
        {
            if (game == null || registered) return;
            registered = true;
            bool anyAdded = false;
            anyAdded |= EnsureComponent<GameComponent_CodexTracker>(game);
            anyAdded |= EnsureComponent<GameComponent_ThrumboHerdTracker>(game);
            anyAdded |= EnsureComponent<TitanGameComponent>(game);
            if (anyAdded)
            {
                TITAN_CodexMod.ApplySettingsToIncidentDefs();
            }
        }

        static bool EnsureComponent<T>(Game game) where T : GameComponent
        {
            if (game.GetComponent<T>() != null) return false;
            var ctor = typeof(T).GetConstructor(new[] { typeof(Game) });
            if (ctor != null)
            {
                var comp = (T)ctor.Invoke(new object[] { game });
                game.components.Add(comp);
                return true;
            }
            return false;
        }
    }
}
