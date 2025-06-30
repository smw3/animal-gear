using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Verse;

namespace AnimalGear
{
	public static class AnimalGearHelper
	{
		public static bool IsAnimal(this Pawn pawn)
		{
			return (pawn.def.race != null && pawn.def.race.intelligence == Intelligence.Animal);
		}

		public static bool IsAnimalOfColony(this Pawn pawn)
		{
			var race = pawn.def.race;
			return (pawn.Faction != null && pawn.Faction.IsPlayer && race != null && race.intelligence == Intelligence.Animal && race.FleshType != FleshTypeDefOf.Mechanoid);
		}

		public static bool IsAnimalOfAFaction(this Pawn pawn)
		{
			var race = pawn.def.race;
			return (pawn.Faction != null && pawn.Faction.IsPlayer && race.intelligence == Intelligence.Animal && race.FleshType != FleshTypeDefOf.Mechanoid);
		}

        public static void EnsureInitApparelTrackers(this Pawn pawn)
        {
            if (pawn.outfits == null)
            {
                pawn.outfits = new Pawn_OutfitTracker(pawn);
            }
            if (pawn.equipment == null)
            {
                pawn.equipment = new Pawn_EquipmentTracker(pawn);
            }
            if (pawn.apparel == null)
            {
                pawn.apparel = new Pawn_ApparelTracker(pawn);
            }
        }

        public static List<ThingDef> RequiredThingDefFromTags(ApparelProperties apparelProperties)
        {
            List<string> requiredThingDef = [.. apparelProperties.tags.Where(x => x.StartsWith("defName")).Select(x => x.ReplaceFirst("defName", ""))];

            return [.. requiredThingDef.Select(defName => DefDatabase<ThingDef>.GetNamed(defName))];
        }

        private static MethodInfo IsSapientAnimalMethod = null;
        public static bool IsSapientAnimal(this Pawn pawn)
        {
            if (!ModsConfig.IsActive("RedMattis.BetterPrerequisites")) return false;
            IsSapientAnimalMethod ??= AccessTools.Method(AccessTools.TypeByName("AnimalGenes.Helpers.ExtensionMethods"), "IsSapientAnimal");

            return (bool)IsSapientAnimalMethod.Invoke(null, new object[] { pawn });
        }

        private static MethodInfo AnimalSourceForMethod = null;
        public static ThingDef AnimalSourceFor(this Pawn pawn)
        {
            if (!ModsConfig.IsActive("RedMattis.BetterPrerequisites")) return null;
            AnimalSourceForMethod ??= AccessTools.Method(AccessTools.TypeByName("BigAndSmall.HumanlikeAnimals"), "AnimalSourceFor");

            return (ThingDef)AnimalSourceForMethod.Invoke(null, new object[] { pawn.def });
        }

        private static MethodInfo GetCacheMethod = null;
        public static float GetCachedSapientAnimalSize(this Pawn pawn)
        {
            if (!ModsConfig.IsActive("RedMattis.BetterPrerequisites")) return 1.0f;
            GetCacheMethod ??= AccessTools.Method(AccessTools.TypeByName("BigAndSmall.HumanoidPawnScaler"), "GetCacheUltraSpeed");

            object cache = GetCacheMethod.Invoke(null, new object[] { pawn, false });
            Type cacheType = AccessTools.TypeByName("BigAndSmall.BSCache");

            return (float)cacheType.GetField("totalCosmeticSize").GetValue(cache);
        }

        public static bool CanEquipApparel(ThingDef thing, Pawn pawn, ref string cantReason)
        {
            ApparelProperties appProps = thing.apparel;
            if (!pawn.IsAnimal() && !pawn.IsSapientAnimal()) return true;
            if (appProps.tags.Any(x => x.StartsWith("defName")))
            {
                bool defAllowed = false;
                if (RequiredThingDefFromTags(appProps).Contains(pawn.def))
                {
                    defAllowed = true;
                } else {
                    if (ModsConfig.IsActive("RedMattis.BetterPrerequisites"))
                    {
                        if (pawn.IsSapientAnimal() && RequiredThingDefFromTags(appProps).Contains(AnimalSourceFor(pawn)))
                        {
                            defAllowed = true;
                        }
                    }
                }

                if (!defAllowed)
                {
                    cantReason = "ANG_WrongBodyType".Translate();
                    return false;
                } else
                {
                    return true;
                }
            }

            cantReason = "ANG_WrongBodyType".Translate();
            return false;
        }
    }
}
