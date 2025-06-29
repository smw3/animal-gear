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
    public class RenderTree
    {
        public class DynamicPawnRenderNodeSetup_Animal_Apparel : DynamicPawnRenderNodeSetup
        {
            public override bool HumanlikeOnly => false;

            public override IEnumerable<(PawnRenderNode node, PawnRenderNode parent)> GetDynamicNodes(Pawn pawn, PawnRenderTree tree)
            {
                if (pawn.apparel == null || pawn.apparel.WornApparelCount == 0)
                {
                    yield break;
                }

                PawnRenderNode animalApparelNode = (tree.TryGetNodeByTag(AnimalPawnRenderNodeTagDefOf.AnimalApparel, out animalApparelNode) ? animalApparelNode : null);
                if (animalApparelNode == null) yield break;

                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    if (!apparel.def.IsWeapon)
                    {
                        DrawData drawData = apparel.def.apparel.drawData;
                        PawnRenderNodeProperties pawnRenderNodeProperties = new PawnRenderNodeProperties
                        {
                            debugLabel = apparel.def.defName,
                            workerClass = typeof(PawnRenderNodeWorker_Animal_Apparel),
                            baseLayer = animalApparelNode.Props.baseLayer,
                            drawData = drawData
                        };

                        yield return new ValueTuple<PawnRenderNode, PawnRenderNode>(new PawnRenderNode_Animal_Apparel(pawn, pawnRenderNodeProperties, tree, apparel), animalApparelNode);
                    }
                }
            }
        }
        public static bool TryGetGraphicApparelForAnimal(Apparel apparel, Pawn pawn, bool forStatue, out ApparelGraphicRecord rec)
        {
            if (apparel.WornGraphicPath.NullOrEmpty())
            {
                rec = new ApparelGraphicRecord(null, null);
                return false;
            }

            string path =  apparel.WornGraphicPath;
            Shader shader = ShaderDatabase.Cutout;
            if (!forStatue)
            {
                if (apparel.StyleDef?.graphicData.shaderType != null)
                {
                    shader = apparel.StyleDef.graphicData.shaderType.Shader;
                }
                else if ((apparel.StyleDef == null && apparel.def.apparel.useWornGraphicMask) || (apparel.StyleDef != null && apparel.StyleDef.UseWornGraphicMask))
                {
                    shader = ShaderDatabase.CutoutComplex;
                }
            }

            Graphic graphic;
            ApparelProperties appProp = apparel.def.apparel;
            if (!appProp.tags.NullOrEmpty())
            {
                if (appProp.tags.Any(t => t.StartsWith("defName")))
                {
                    graphic = GraphicDatabase.Get<Graphic_Multi>($"{path}/{pawn.def.defName.CapitalizeFirst()}/{pawn.def.defName.CapitalizeFirst()}", shader, apparel.def.graphicData.drawSize, apparel.DrawColor);
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

        public class PawnRenderNode_Animal_Apparel : PawnRenderNode
        {
            public PawnRenderNode_Animal_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
                : base(pawn, props, tree)
            {
                this.apparel = apparel;
            }

            public override GraphicMeshSet MeshSetFor(Pawn pawn)
            {
                float drawSize = pawn.ageTracker.CurKindLifeStage.bodyGraphicData?.drawSize.x ?? 1f;
                return MeshPool.GetMeshSetForSize(drawSize, drawSize);
            }
            protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
            {
                ApparelGraphicRecord apparelGraphicRecord;
                if (TryGetGraphicApparelForAnimal(this.apparel, pawn, false, out apparelGraphicRecord))
                {
                    yield return apparelGraphicRecord.graphic;
                }
                yield break;
            }
        }

        public class PawnRenderNodeWorker_Animal_Apparel : PawnRenderNodeWorker
        {
            public override bool CanDrawNow(PawnRenderNode n, PawnDrawParms parms)
            {
                return base.CanDrawNow(n, parms);
            }
        }
    }
}
