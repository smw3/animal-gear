using System;
using System.Reflection;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

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

        public static List<BodyDef> RequiredBodyDefFromTags(ApparelProperties apparelProperties)
        {
            List<string> requiredBodyTypeDefName = [.. apparelProperties.tags.Where(x => x.StartsWith("bodyType")).Select(x => x.ReplaceFirst("bodyType", ""))];

            return [.. requiredBodyTypeDefName.Select(defName => DefDatabase<BodyDef>.GetNamed(defName))];
        }

        public static List<ThingDef> RequiredThingDefFromTags(ApparelProperties apparelProperties)
        {
            List<string> requiredThingDef = [.. apparelProperties.tags.Where(x => x.StartsWith("defName")).Select(x => x.ReplaceFirst("defName", ""))];

            return [.. requiredThingDef.Select(defName => DefDatabase<ThingDef>.GetNamed(defName))];
        }
    }
}
