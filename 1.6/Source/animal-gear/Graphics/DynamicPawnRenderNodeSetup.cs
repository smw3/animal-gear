using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static AnimalGear.Graphics.RenderHelpers;

namespace AnimalGear.Graphics
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
                if (!apparel.def.IsWeapon && !AnimalGearHelper.InvisibleForAnimal(apparel.def))
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
}
