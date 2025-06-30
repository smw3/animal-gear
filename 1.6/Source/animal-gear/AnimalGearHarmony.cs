using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AnimalGear
{
    static class AnimalGearHarmony
    {
        [HarmonyPatch(typeof(TargetingParameters), nameof(TargetingParameters.ForForceWear))]
        public static class TargetingParameters_CanTarget
        {
            public static bool Prefix(ref TargetingParameters __result, Pawn selectedPawnForJob)
            {
                __result = new TargetingParameters
                {
                    canTargetPawns = true,
                    canTargetAnimals = true,
                    canTargetMechs = false,
                    canTargetItems = false,
                    canTargetBuildings = true,
                    mapObjectTargetsMustBeAutoAttackable = false,
                    validator = delegate (TargetInfo targ)
                    {
                        if (!targ.HasThing)
                        {
                            return false;
                        }
                        Pawn pawn = targ.Thing as Pawn;
                        if (pawn == null)
                        {
                            return ModsConfig.OdysseyActive && targ.Thing is Building_OutfitStand;
                        }
                        return pawn != selectedPawnForJob && pawn.kindDef.canStrip && ((pawn.Faction != null && pawn.Faction.IsPlayer) || (pawn.Downed) || (pawn.IsPrisonerOfColony && pawn.guest.PrisonerIsSecure) || (!pawn.IsQuestLodger() && pawn.IsColonist));
                    }
                };
                return false;
            }
        }

        [HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents))]
        public static class PawnComponentsUtility_CreateInitialComponents_Patch
        {
            public static void Postfix(Pawn pawn)
            {
                if (pawn.IsAnimalOfAFaction()) pawn.EnsureInitApparelTrackers();
            }
        }

        [HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents))]
        public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
        {
            public static void Postfix(Pawn pawn, bool actAsIfSpawned)
            {
                if (pawn.IsAnimalOfAFaction()) pawn.EnsureInitApparelTrackers();
            }
        }

        [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
        public static class Pawn_ApparelTracker_Notify_ApparelChanged_Patch
        {
            public static bool Prefix(Pawn_ApparelTracker __instance)
            {
                if (__instance.pawn.IsAnimal())
                {
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ThingDef), "get_DescriptionDetailed")]
        public static class ThingDef_get_DescriptionDetailed_Patch
        {
            public static bool Prefix(ThingDef __instance, ref string __result)
            {
                if (!__instance.HasModExtension<AnimalApparelDefExtension>()) { return true; }
                AnimalApparelDefExtension defExt = __instance.GetModExtension<AnimalApparelDefExtension>();

                var cachedDescriptionField = typeof(ThingDef).GetField("descriptionDetailedCached", BindingFlags.Instance | BindingFlags.NonPublic);
                if (cachedDescriptionField.GetValue(__instance) == null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(__instance.description);
                    if (__instance.IsApparel)
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(string.Format("{0}: {1}", "Layer".Translate(), __instance.apparel.GetLayersString()));
                        stringBuilder.AppendLine(string.Format("{0}", "ANG_RequireBodyType".Translate()));
                        stringBuilder.Append(string.Format("{0}: {1}", "Covers".Translate(), __instance.apparel.GetCoveredOuterPartsString(defExt.showCoverageForBodyType)));
                        if (__instance.equippedStatOffsets != null && __instance.equippedStatOffsets.Count > 0)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine();
                            for (int i = 0; i < __instance.equippedStatOffsets.Count; i++)
                            {
                                if (i > 0)
                                {
                                    stringBuilder.AppendLine();
                                }
                                StatModifier statModifier = __instance.equippedStatOffsets[i];
                                stringBuilder.Append(string.Format("{0}: {1}", statModifier.stat.LabelCap, statModifier.ValueToStringAsOffset));
                            }
                        }
                    }
                    cachedDescriptionField.SetValue(__instance, stringBuilder.ToString());
                }
                __result = (string)cachedDescriptionField.GetValue(__instance);
                return false;
            }
        }

        // Redirect CanControlColonist to CanControl and a spawned check. Most of the checks done are super redundant anyways
        // but the main purpose is to skip a test for humanlike
        // This method is exclusively used to decide whether or not to draw the "drop apparel" inventory gizmo, so should have
        // no side-effects
        [HarmonyPatch(typeof(ITab_Pawn_Gear), "get_CanControlColonist")]
        public static class ITab_Pawn_Gear_CanControl_Patch
        {
            public static bool Prefix(ref bool __result, ITab_Pawn_Gear __instance)
            {
                var SelPawnForGearField = typeof(ITab_Pawn_Gear).GetMethod("get_CanControl", BindingFlags.Instance | BindingFlags.NonPublic);
                Pawn selPawn = (Pawn)typeof(ITab_Pawn_Gear).GetMethod("get_SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
                __result = (bool)SelPawnForGearField.Invoke(__instance, new object[] { });
                __result = __result && selPawn.Spawned;
                return false;
            }
        }


        [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
        public static class ThingDef_SpecialDisplayStatsPatch
        {
            public static IEnumerable<StatDrawEntry> Postfix(IEnumerable<StatDrawEntry> values, StatRequest req, ThingDef __instance)
            {
                bool extraStatInserted = false;
                foreach (StatDrawEntry entry in values)
                {
                    // The "covers" entry, eat it and do our own
                    if (entry.DisplayPriorityWithinCategory == 2750)
                    {
                        ApparelProperties appProps = __instance.apparel;
                        BodyDef showCoverageFor = __instance.GetModExtension<AnimalApparelDefExtension>()?.showCoverageForBodyType ?? BodyDefOf.Human;
                        string coveredOuterPartsString = __instance.apparel.GetCoveredOuterPartsString(showCoverageFor);
                        yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "Covers".Translate(), coveredOuterPartsString, "Stat_Thing_Apparel_Covers_Desc".Translate(), 2750, null, null, false, false);
                    }

                    if (entry.category == StatCategoryDefOf.Apparel && !extraStatInserted)
                    {
                        ApparelProperties appProps = __instance.apparel;
                        if (!appProps.tags.NullOrEmpty() && appProps.tags.Any(x => x.StartsWith("defName")))
                        {
                            List<ThingDef> requiredDefs = AnimalGearHelper.RequiredThingDefFromTags(appProps);

                            yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "ANG_RequireDefName".Translate(),
                                    requiredDefs.Select((ThingDef def) => def.label.CapitalizeFirst())
                                    .ToCommaList(false, false),
                                "ANG_RequiresBodyTypeDesc".Translate(), 2750, null, null, false, false);
                        }

                        extraStatInserted = true;
                    }
                    if (entry.DisplayPriorityWithinCategory != 2750) yield return entry;
                }
            }
        }

        public static bool CanEquipThing(bool __result, ThingDef thing, Pawn pawn, ref string cantReason)
        {
            if (__result == false || thing == null || pawn == null)
            {
                return __result;
            }

            if (thing.IsApparel)
            {
                __result = __result && AnimalGearHelper.CanEquipApparel(thing, pawn, ref cantReason);
            }

            return __result;
        }

        // Gratefully borrowed from B&S Framework
        [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), new Type[]
        {
        typeof(Thing),
        typeof(Pawn),
        typeof(string),
        typeof(bool)
        }, new ArgumentType[]
        {
        ArgumentType.Normal,
        ArgumentType.Normal,
        ArgumentType.Out,
        ArgumentType.Normal
        })]
        public static class EquipmentUtility_CanEquip_Patch
        {
            public static void Postfix(ref bool __result, Thing thing, Pawn pawn, ref string cantReason, bool checkBonded = true)
            {
                __result = CanEquipThing(__result, thing.def, pawn, ref cantReason);
            }
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.AllowedForPawn))]
        public static class ApparelRequirement_AllowedForPawn_Patch
        {
            public static void Postfix(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
            {
                if (__result == true)
                {
                    string discard = "";
                    __result = CanEquipThing(__result, apparel, p, ref discard);
                }

            }
        }

        [HarmonyPatch(typeof(ApparelRequirement), nameof(ApparelRequirement.RequiredForPawn))]
        public static class ApparelRequirement_RequiredForPawn_Patch
        {
            public static void Postfix(ApparelRequirement __instance, ref bool __result, Pawn p, ThingDef apparel, bool ignoreGender)
            {
                if (__result == true)
                {
                    string discard = "";
                    __result = CanEquipThing(__result, apparel, p, ref discard);
                }
            }
        }

        [HarmonyPatch(typeof(Apparel), nameof(Apparel.PawnCanWear))]
        public static class Apparel_PawnCanWear_Patch
        {
            public static void Postfix(Apparel __instance, ref bool __result, Pawn pawn, bool ignoreGender)
            {
                if (__result == true)
                {
                    string discard = "";
                    __result = CanEquipThing(__result, __instance.def, pawn, ref discard);
                }
            }
        }
    }
}
