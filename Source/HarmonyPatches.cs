using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace ArtReroll
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("com.pipai.artreroll");

            PatchPostfix(harmony, typeof(ITab_Art), "FillTab", "FillTab");
        }

        private static void PatchPostfix(Harmony harmony, Type type, string target, string postfix)
        {
            MethodInfo targetMethod = AccessTools.Method(type, target);
            HarmonyMethod postfixMethod = new HarmonyMethod(typeof(HarmonyPatches).GetMethod(postfix));
            harmony.Patch(targetMethod, null, postfixMethod);
        }

        public static void FillTab(ITab_Art __instance)
        {
            Rect rect = new Rect(10f, 270f, 100f, 20f);
            if (Widgets.ButtonText(rect, "Reroll"))
            {
                Thing thing = Find.Selector.SingleSelectedThing;
                if (thing is MinifiedThing minifiedThing)
                {
                    thing = minifiedThing.InnerThing;
                }
                CompArt art = thing?.TryGetComp<CompArt>();
                TaleReference originalTale = art.TaleRef;
                art.InitializeArt(ArtGenerationContext.Colony);
                originalTale.ReferenceDestroyed();
            }
        }
    }
}
