using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace AnimalGear.Debug
{
    public static class AnimalGearDebug
    {
        private static void SpawnArmoredAnimal(PawnKindDef animalDef, ThingDef armorDef)
        {
            IntVec3 intVec = CellFinder.RandomClosewalkCellNear(RCellFinder.RandomAnimalSpawnCell_MapGen(Find.CurrentMap), Find.CurrentMap, 5);
            Pawn pawn = PawnGenerator.GeneratePawn(animalDef);
            pawn.EnsureInitApparelTrackers();
            Thing armor = ThingMaker.MakeThing(armorDef, null);
            pawn.apparel.Wear((Apparel)armor);

            GenPlace.TryPlaceThing(SkyfallerMaker.MakeSkyfaller(ThingDefOf.FlyerArrival, pawn), intVec, Find.CurrentMap, ThingPlaceMode.Near);
        }

        [DebugAction("Animal Gear", "Spawn armored animal", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnArmoredAnimal()
        {
            ThingDef powerArmor = DefDatabase<ThingDef>.GetNamed("Apparel_ArmorCataphract_Animal");
            List<DebugActionNode> list = new List<DebugActionNode>();
            foreach (PawnKindDef pawnKindDef in from x in DefDatabase<PawnKindDef>.AllDefs
                                                where x.RaceProps.Animal
                                                select x into kd
                                                orderby kd.defName
                                                select kd)
            {
                if (AnimalGearHelper.RequiredThingDefFromTags(powerArmor.apparel).Contains(pawnKindDef.race))
                {
                    PawnKindDef localKindDef = pawnKindDef;
                    list.Add(new DebugActionNode(localKindDef.defName, DebugActionType.Action, null, null)
                    {
                        category = DebugToolsSpawning.GetCategoryForPawnKind(localKindDef),
                        action = delegate
                        {
                            SpawnArmoredAnimal(localKindDef, powerArmor);
                        }
                    });
                }
            }
            return list;
        }

        [DebugAction("Animal Gear", "Spawn armored animal x100", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static List<DebugActionNode> SpawnArmoredAnimal100()
        {
            ThingDef powerArmor = DefDatabase<ThingDef>.GetNamed("Apparel_ArmorCataphract_Animal");
            List<DebugActionNode> list = new List<DebugActionNode>();
            foreach (PawnKindDef pawnKindDef in from x in DefDatabase<PawnKindDef>.AllDefs
                                                where x.RaceProps.Animal
                                                select x into kd
                                                orderby kd.defName
                                                select kd)
            {
                if (AnimalGearHelper.RequiredThingDefFromTags(powerArmor.apparel).Contains(pawnKindDef.race))
                {
                    PawnKindDef localKindDef = pawnKindDef;
                    list.Add(new DebugActionNode(localKindDef.defName, DebugActionType.Action, null, null)
                    {
                        category = DebugToolsSpawning.GetCategoryForPawnKind(localKindDef),
                        action = delegate
                        {
                            for (int i = 0; i < 100; i++)
                            {
                                SpawnArmoredAnimal(localKindDef, powerArmor);
                            }
                        }
                    });
                }
            }
            return list;
        }
    }
}
