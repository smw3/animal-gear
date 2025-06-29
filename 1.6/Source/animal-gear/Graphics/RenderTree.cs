using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
                    if (!apparel.def.IsWeapon) // Check if apparel is animal gear
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

        public class PawnRenderNode_Animal_Apparel : PawnRenderNode
        {
            public PawnRenderNode_Animal_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
                : base(pawn, props, tree)
            {
                this.apparel = apparel;
            }

            public override GraphicMeshSet MeshSetFor(Pawn pawn)
            {
                if (this.apparel == null)
                {
                    return base.MeshSetFor(pawn);
                }
                if (base.Props.overrideMeshSize != null)
                {
                    return MeshPool.GetMeshSetForSize(base.Props.overrideMeshSize.Value.x, base.Props.overrideMeshSize.Value.y);
                }

                return HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn, 1f, 1f);
            }
            protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
            {
                ApparelGraphicRecord apparelGraphicRecord;
                if (ApparelGraphicRecordGetter.TryGetGraphicApparel(this.apparel, BodyTypeDefOf.Male, false, out apparelGraphicRecord))
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
