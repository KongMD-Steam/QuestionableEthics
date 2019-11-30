using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
    /// WorkGiver for order processors.
    /// </summary>
    public class WorkGiver_GrowerOrderProcessor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

        //JobDriver_HaulToContainer is Job 'HaulToContainer'
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_GrowerBase grower = t as Building_GrowerBase;
            if(grower == null)
            {
                return false;
            }

            if(t.IsForbidden(pawn))
            {
                return false;
            }

            if(!pawn.CanReserve(t))
            {
                return false;
            }

            if(grower.status != CrafterStatus.Filling)
            {
                return false;
            }

            if(grower.orderProcessor.PendingRequests.Count() <= 0)
            {
                return false;
            }

            return IngredientUtility.FindClosestRequestForThingOrderProcessor(grower.orderProcessor, pawn) is Thing cloestThing && pawn.CanReserve(cloestThing);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_GrowerBase grower = t as Building_GrowerBase;
            Thing fillThing = IngredientUtility.FindClosestRequestForThingOrderProcessor(grower.orderProcessor, pawn);
            int fillCount = grower.orderProcessor.PendingRequests.First(req => req.ThingMatches(fillThing)).amount;

            Job job = new Job(QEJobDefOf.QE_DepositIntoGrowerJob, t, fillThing);
            job.count = fillCount;
            return job;
        }
    }
}
