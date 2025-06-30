using KTrie;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AnimalGear.Graphics
{
    public class PawnRenderNode_Animal_Apparel : PawnRenderNode
    {
        public PawnRenderNode_Animal_Apparel(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree, Apparel apparel)
            : base(pawn, props, tree)
        {
            this.apparel = apparel;
        }

        public override GraphicMeshSet MeshSetFor(Pawn pawn)
        {
            tree.TryGetNodeByTag(PawnRenderNodeTagDefOf.Body, out PawnRenderNode bodyNode);
            if (bodyNode != null)
            {
                return bodyNode.MeshSetFor(pawn);
            }

            float drawSize = pawn.ageTracker.CurKindLifeStage.bodyGraphicData?.drawSize.x ?? 1f;
            return MeshPool.GetMeshSetForSize(drawSize, drawSize);
        }
        protected override IEnumerable<Graphic> GraphicsFor(Pawn pawn)
        {
            ApparelGraphicRecord apparelGraphicRecord;
            if (RenderHelpers.TryGetGraphicApparelForAnimal(this.apparel, pawn, out apparelGraphicRecord))
            {
                yield return apparelGraphicRecord.graphic;
            }
            yield break;
        }
    }
}
