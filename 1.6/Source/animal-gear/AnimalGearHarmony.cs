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
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using static UnityEngine.Scripting.GarbageCollector;

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
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // For BodyDef replacement
                var staticField = AccessTools.Field(typeof(BodyDefOf), nameof(BodyDefOf.Human));
                var replacementMethod = AccessTools.Method(typeof(AnimalGearHelper), nameof(AnimalGearHelper.GetBodyDefForCoverageInfo));

                // For extra line insertion
                var appendMethod = AccessTools.Method(typeof(StringBuilder), nameof(StringBuilder.Append), new[] { typeof(string) });
                var newLineProperty = AccessTools.Property(typeof(Environment), nameof(Environment.NewLine));
                var getNewLineMethod = newLineProperty.GetGetMethod();
                var equippableByMethod = AccessTools.Method(typeof(AnimalGearHelper), nameof(AnimalGearHelper.EquippableByStringFull));
                int appendCount = 0;

                for (var i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];

                    // Replace the reference to BodyDefOf.Human with a method call
                    if (code.opcode == OpCodes.Ldsfld && code.operand as FieldInfo == staticField)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // Load 'this' (first argument in instance methods)
                        yield return new CodeInstruction(OpCodes.Call, replacementMethod);
                    } else if (code.opcode == OpCodes.Callvirt && code.operand as MethodInfo == appendMethod) 
                    {
                        appendCount++;

                        yield return code;
                        // After "Covers: "...
                        if (appendCount == 2)
                        {
                            // Append new line
                            yield return new CodeInstruction(OpCodes.Call, getNewLineMethod);
                            yield return new CodeInstruction(OpCodes.Callvirt, appendMethod);

                            // Append AnimalGearHelper.EquippableByStringFull(this)
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Call, equippableByMethod);
                            yield return new CodeInstruction(OpCodes.Callvirt, appendMethod);
                        }
                    } else {
                        yield return code;
                    }
                }
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

        // Stop the normal render node setup from setting up apparel nodes for animals, sapient and otherwise
        [HarmonyPatch(typeof(DynamicPawnRenderNodeSetup_Apparel), "GetDynamicNodes")]
        public static class DynamicPawnRenderNodeSetup_Apparel_GetDynamicNodes_Patch
        {
            public static bool Prefix(ref IEnumerable<ValueTuple<PawnRenderNode, PawnRenderNode>> __result, DynamicPawnRenderNodeSetup_Apparel __instance, Pawn pawn, PawnRenderTree tree)
            {
                if (pawn.IsAnimal() || pawn.IsSapientAnimal())
                {
                    __result = Enumerable.Empty<ValueTuple<PawnRenderNode, PawnRenderNode>>();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch]
        public static class ThingDef_SpecialDisplayStats_MoveNext_Patch
        {
            static MethodBase TargetMethod()
            {
                var iteratorType = typeof(ThingDef).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(t => t.Name.Contains("SpecialDisplayStats"));
                return AccessTools.Method(iteratorType, "MoveNext");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                // For BodyDef replacement
                var staticField = AccessTools.Field(typeof(BodyDefOf), nameof(BodyDefOf.Human));
                var replacementMethod = AccessTools.Method(typeof(AnimalGearHelper), nameof(AnimalGearHelper.GetBodyDefForCoverageInfo));
                for (var i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];

                    // Replace the reference to BodyDefOf.Human with a method call
                    if (code.opcode == OpCodes.Ldsfld && code.operand as FieldInfo == staticField)
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc_2); // Load 'this' (first argument in instance methods)
                        yield return new CodeInstruction(OpCodes.Call, replacementMethod);
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
        }

        [HarmonyPatch]
        public static class CompApparelVerbOwner_CompGetWornGizmosExtra_Patch
        {
            static MethodBase TargetMethod()
            {
                var iteratorType = typeof(CompApparelVerbOwner).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(t => t.Name.Contains("CompGetWornGizmosExtra"));
                return AccessTools.Method(iteratorType, "MoveNext");
            }

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var isAnimalColony = AccessTools.Method(typeof(AnimalGearHelper), nameof(AnimalGearHelper.IsAnimalOfColony));
                var get_Wearer = AccessTools.Method(typeof(CompApparelVerbOwner), "get_Wearer");

                for (var i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];

                    // Extend drafted with another or
                    if (code.opcode == OpCodes.Stloc_3)
                    {
                        yield return code;

                        // drafted = drafted || IsAnimalOfColony(this.Wearer)
                        yield return new CodeInstruction(OpCodes.Ldloc_3);
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                        yield return new CodeInstruction(OpCodes.Callvirt, get_Wearer);
                        yield return new CodeInstruction(OpCodes.Call, isAnimalColony);
                        yield return new CodeInstruction(OpCodes.Or);
                        yield return new CodeInstruction(OpCodes.Stloc_3);
                    }
                    else
                    {
                        yield return code;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CompApparelVerbOwner), "CreateVerbTargetCommand")]
        public static class CompApparelVerbOwner_CreateVerbTargetCommand_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var isColonistPlayerControlled = AccessTools.Method(typeof(Pawn), "get_IsColonistPlayerControlled");
                var get_Wearer = AccessTools.Method(typeof(CompApparelVerbOwner), "get_Wearer");
                var isAnimalColony = AccessTools.Method(typeof(AnimalGearHelper), nameof(AnimalGearHelper.IsAnimalOfColony));

                for (var i = 0; i < codes.Count; i++)
                {
                    var code = codes[i];
                    if (code.opcode == OpCodes.Callvirt && code.operand as MethodInfo == isColonistPlayerControlled)
                    {
                        yield return code;

                        // effectively: && !IsAnimalOfColony(this.Wearer)
                        // but it's not negated and an or, because if true it skips the if block.
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, get_Wearer);
                        yield return new CodeInstruction(OpCodes.Call, isAnimalColony);

                        yield return new CodeInstruction(OpCodes.Or);
                    } else
                    {
                        yield return code;
                    }
                }
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
                    if (entry.category == StatCategoryDefOf.Apparel && !extraStatInserted)
                    {
                        ApparelProperties appProps = __instance.apparel;
                        yield return new StatDrawEntry(StatCategoryDefOf.Apparel, "ANG_SuitableFor".Translate(), AnimalGearHelper.EquippableByString(__instance),
                                AnimalGearHelper.EquippableByStringFull(__instance), 2750, null, null, false, false);

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

                    yield return entry;
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

        public static bool CanEquipThing(bool __result, ApparelProperties properties, Pawn pawn, ref string cantReason)
        {
            if (__result == false || properties == null || pawn == null)
            {
                return __result;
            }

            __result = __result && AnimalGearHelper.CanEquipApparel(properties, pawn, ref cantReason);

            return __result;
        }

        // Gratefully borrowed from B&S Framework
        [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), [ typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) ], [ ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal ])]
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

        [HarmonyPatch(typeof(ApparelProperties), nameof(ApparelProperties.PawnCanWear), [typeof(Pawn), typeof(bool)])]
        public static class ApparelProperties_PawnCanWear_Patch
        {
            public static void Postfix(ApparelProperties __instance, ref bool __result, Pawn pawn, bool ignoreGender)
            {
                if (__result == true)
                {
                    string discard = "";
                    __result = CanEquipThing(__result, __instance, pawn, ref discard);
                }
            }
        }
    }
}
