using Verse;
using HarmonyLib;
using System;
using UnityEngine;
using static AnimalGear.AnimalGearSettings;

namespace AnimalGear
{
	internal class AnimalGearMod : Mod
	{
		public AnimalGearMod(ModContentPack content) : base(content)
		{
			base.GetSettings<AnimalGearSettings>();
			new Harmony("AnimalGear").PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
			options.Label("AnimalGearRenderMode_Title".Translate(), -1f, null);

			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Animal Gear";
		}
	}

	public class AnimalGearSettings : ModSettings
	{
		public override void ExposeData()
		{
			base.ExposeData();
		}
	}
}
