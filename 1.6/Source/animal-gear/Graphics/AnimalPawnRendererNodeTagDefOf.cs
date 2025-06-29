using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AnimalGear.Graphics
{
    [DefOf]
    public static class AnimalPawnRenderNodeTagDefOf
    {
        public static PawnRenderNodeTagDef AnimalApparel;

        static AnimalPawnRenderNodeTagDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PawnRenderNodeTagDefOf));
        }
    }
}
