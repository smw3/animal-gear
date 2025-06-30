using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace AnimalGear.Graphics
{
    public class RenderHelpers
    {
        public static bool TryGetGraphicApparelForAnimal(Apparel apparel, Pawn pawn, out ApparelGraphicRecord rec)
        {
            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                rec = new ApparelGraphicRecord(null, null);
                return false;
            }

            string path =  apparel.WornGraphicPath;
            Shader shader = ShaderDatabase.Cutout;
            if (apparel.StyleDef?.graphicData.shaderType != null)
            {
                shader = apparel.StyleDef.graphicData.shaderType.Shader;
            } else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
            {
                shader = ShaderDatabase.CutoutComplex;
            }            

            Graphic graphic;
            ApparelProperties appProp = apparel.def.apparel;
            if (!appProp.tags.NullOrEmpty())
            {
                if (appProp.tags.Any(t => t.StartsWith("defName")))
                {
                    string pawnDefToUse = pawn.def.defName;
                    if (pawn.IsSapientAnimal()) pawnDefToUse = AnimalGearHelper.AnimalSourceFor(pawn).defName;

                    graphic = GraphicDatabase.Get<Graphic_Multi>($"{path}/{pawnDefToUse.CapitalizeFirst()}/{pawnDefToUse.CapitalizeFirst()}", shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
                    if (graphic != null)
                    {
                        rec = new ApparelGraphicRecord(graphic, apparel);
                        return true;
                    }
                }
            }

            graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
            rec = new ApparelGraphicRecord(graphic, apparel);
            return true;
        }
    }
}
