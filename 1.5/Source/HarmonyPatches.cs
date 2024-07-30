using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
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
            Thing thing = Find.Selector.SingleSelectedThing;
            if (thing is MinifiedThing minifiedThing)
            {
                thing = minifiedThing.InnerThing;
            }
            CompArt art = thing?.TryGetComp<CompArt>();


            Rect titleRect = new Rect(10f, 250f, 100f, 20f);
            if (Widgets.ButtonText(titleRect, "Reroll Title"))
            {
                InitTitle(art);
            }

            Rect randomRect = new Rect(10f, 270f, 100f, 20f);
            if (Widgets.ButtonText(randomRect, "Reroll"))
            {
                art.InitializeArt(ArtGenerationContext.Colony);
            }

            // The following are much more involved and cannot use the default behavior
            Rect spColonistRect = new Rect(110f, 250f, 240f, 20f);
            if (Widgets.ButtonText(spColonistRect, "Pick Colonist for Current Tale"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (Pawn pawn in from p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists orderby p.Name.ToStringShort select p)
                {
                    FloatMenuOption option = new FloatMenuOption(pawn.Name.ToStringShort, delegate
                    {
                        var traverse = Traverse.Create(art.TaleRef);
                        var taleDef = traverse.Field("tale").GetValue<Tale>().def;
                        var tales = Find.TaleManager.AllTalesListForReading
                            .Where(it => it.Concerns(pawn) && it.def.defName == taleDef.defName);
                        var selected = tales.RandomElementByWeightWithFallback(it => it.InterestLevel + (1 / (1 + it.Uses)));
                        InitializeArt(art, selected);
                    });
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect specificRect = new Rect(110f, 270f, 120f, 20f);
            if (Widgets.ButtonText(specificRect, "Reroll Specific"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (TaleDef taleDef in from def in DefDatabase<TaleDef>.AllDefs orderby def.defName select def)
                {
                    FloatMenuOption option = new FloatMenuOption(taleDef.defName, delegate
                    {
                        var tales = Find.TaleManager.AllTalesListForReading.Where(it => it.def.defName == taleDef.defName);
                        var selected = tales.RandomElementByWeightWithFallback(it => it.InterestLevel + (1 / (1 + it.Uses)));
                        InitializeArt(art, selected);
                    });
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }


            Rect rect3 = new Rect(230f, 270f, 120f, 20f);
            if (Widgets.ButtonText(rect3, "Reroll Colonist"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (Pawn pawn in from p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists orderby p.Name.ToStringShort select p)
                {
                    FloatMenuOption option = new FloatMenuOption(pawn.Name.ToStringShort, delegate
                    {
                        var tales = Find.TaleManager.AllTalesListForReading.Where(it => it.Concerns(pawn));
                        var selected = tales.RandomElementByWeightWithFallback(it => it.InterestLevel + (1 / (1 + it.Uses)));
                        InitializeArt(art, selected);
                    });
                    options.Add(option);
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        /// <summary>
        /// A reimplementation of CompArt.InitializeArt() for specific tales.
        /// </summary>
        private static void InitializeArt(CompArt comp, Tale tale)
        {
            if (comp.TaleRef != null)
            {
                comp.TaleRef.ReferenceDestroyed();
            }
            TaleReference taleRef;
            if (tale == null)
            {
                taleRef = TaleReference.Taleless;
            }
            else
            {
                tale.Notify_NewlyUsed();
                taleRef = new TaleReference(tale);
            }
            var traverse = Traverse.Create(comp);
            traverse.Field("taleRef").SetValue(taleRef);
            traverse.Field("titleInt").SetValue(new TaggedString(traverse.Method("GenerateTitle", ArtGenerationContext.Colony).GetValue<string>()));
        }

        private static void InitTitle(CompArt comp)
        {
            var traverseComp = Traverse.Create(comp);
            var traverseTaleRef = Traverse.Create(comp.TaleRef);
            traverseTaleRef.Field("seed").SetValue(traverseTaleRef.Field("seed").GetValue<int>() + 1);
            traverseComp.Field("titleInt").SetValue(new TaggedString(traverseComp.Method("GenerateTitle", ArtGenerationContext.Colony).GetValue<string>()));
        }
    }
}
