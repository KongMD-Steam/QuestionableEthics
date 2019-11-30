using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QEthics
{
    [DefOf]
    public static class QEThingDefOf
    {
        public static ThingDef QE_NutrientSolution;
        public static ThingDef QE_ProteinMash;
        public static ThingDef QE_Organ_Arm;
        public static ThingDef QE_GenomeSequencerFilled;

        static QEThingDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(QEThingDefOf));
        }
    }
}
