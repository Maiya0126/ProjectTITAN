using System;
using System.Reflection;
using Verse;
using RimWorld;
using HarmonyLib;

namespace ProjectTITAN
{
    public class ProjectTitanMod : Mod
    {
        public ProjectTitanMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("com.projecttitan.core");

            // 1. 拦截尸体生成崩溃 (已生效)
            var originalCorpse = AccessTools.Method(typeof(ThingDefGenerator_Corpses), "CalculateMarketValue");
            var finalizerCorpse = AccessTools.Method(typeof(ProjectTitanMod), "CorpseCrashFinalizer");
            if (originalCorpse != null) harmony.Patch(originalCorpse, finalizer: new HarmonyMethod(finalizerCorpse));

            // 2. 【新增】拦截物品价值计算崩溃 (解决本次诱变剂引发的崩溃)
            var originalMarketValue = AccessTools.PropertyGetter(typeof(ThingDef), "MarketValue");
            var finalizerMarketValue = AccessTools.Method(typeof(ProjectTitanMod), "MarketValueCrashFinalizer");
            if (originalMarketValue != null) harmony.Patch(originalMarketValue, finalizer: new HarmonyMethod(finalizerMarketValue));

            // 3. 【新增】拦截药物政策排序崩溃 (保障地图强行生成)
            var originalDrugSort = AccessTools.Method(typeof(DrugPolicy), "InitializeIfNeeded");
            var finalizerDrugSort = AccessTools.Method(typeof(ProjectTitanMod), "DrugSortFinalizer");
            if (originalDrugSort != null) harmony.Patch(originalDrugSort, finalizer: new HarmonyMethod(finalizerDrugSort));

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[Project T.I.T.A.N.] 核心程序集加载成功，全面防御护盾(V2)已启动。");
        }

        public static Exception CorpseCrashFinalizer(Exception __exception, ThingDef raceDef, ref float __result)
        {
            if (__exception != null)
            {
                string badDef = raceDef != null ? raceDef.defName : "未知";
                Log.Warning($"[泰坦护盾] 拦截尸体崩溃: {badDef}。已强行赋予价值 50。");
                __result = 50f;
                return null;
            }
            return null;
        }

        public static Exception MarketValueCrashFinalizer(Exception __exception, ThingDef __instance, ref float __result)
        {
            if (__exception != null)
            {
                string badDef = __instance != null ? __instance.defName : "未知";
                Log.Warning($"[泰坦护盾] 成功拦截【物品价值计算】崩溃！罪魁祸首是: {badDef}。该物品的配方(costList)中包含的材料可能没有 MarketValue。已强行赋予它价值 50。");
                __result = 50f;
                return null; // 吞噬报错，继续运行
            }
            return null;
        }

        public static Exception DrugSortFinalizer(Exception __exception)
        {
            if (__exception != null)
            {
                Log.Warning("[泰坦护盾] 成功拦截【地图生成/药物排序】崩溃！已强制跳过排序步骤，保障地图正常生成！");
                return null; // 吞噬报错，地图将正常加载
            }
            return null;
        }
    }
}