using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                try
                {
                    if (pawn.IsAnimalOfAFaction())
                    {
                        pawn.EnsureInitApparelTrackers();
                    }
                }
                catch (Exception ex)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Error("PawnComponentsUtility_CreateInitialComponents_Patch: error: " + ex.Message);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.AddAndRemoveDynamicComponents))]
        public static class PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch
        {
            public static void Postfix(Pawn pawn, bool actAsIfSpawned)
            {
                try
                {
                    if (pawn.IsAnimalOfAFaction())
                    {
                        pawn.EnsureInitApparelTrackers();
                    }
                }
                catch (Exception ex)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Error("PawnComponentsUtility_AddAndRemoveDynamicComponents_Patch: error: " + ex.Message);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelChanged))]
        public static class Pawn_ApparelTracker_Notify_ApparelChanged_Patch
        {
            public static bool Prefix(Pawn_ApparelTracker __instance)
            {
                if (__instance.pawn.IsAnimalOfAFaction())
                {
                    __instance.pawn.Drawer.renderer.SetAllGraphicsDirty();
                    return false;
                }
                return true;
            }
        }
    }
}
