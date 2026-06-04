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
        static TITAN_GameComponentRegistration()
        {
            var harmony = new Harmony("com.projecttitan.gamecomponents");
            var original = AccessTools.Method(typeof(Game), "FinalizeInit");
            var postfix = AccessTools.Method(typeof(TITAN_GameComponentRegistration), "Postfix_FinalizeInit");
            if (original != null) harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Log.Message("[Project T.I.T.A.N.] GameComponent注册补丁已加载。");
        }

        static void Postfix_FinalizeInit(Game __instance)
        {
            EnsureComponent<GameComponent_CodexTracker>(__instance);
            EnsureComponent<GameComponent_ThrumboHerdTracker>(__instance);
        }

        static void EnsureComponent<T>(Game game) where T : GameComponent
        {
            if (game.GetComponent<T>() != null) return;
            var ctor = typeof(T).GetConstructor(new[] { typeof(Game) });
            if (ctor != null)
            {
                var comp = (T)ctor.Invoke(new object[] { game });
                game.components.Add(comp);
            }
        }
    }
}