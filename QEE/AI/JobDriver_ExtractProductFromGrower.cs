using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// Extracts the product out of a Grower.
    /// </summary>
    public class JobDriver_ExtractProductFromGrower : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if(!pawn.CanReserve(TargetThingA))
            {
                return false;
            }

            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            AddEndCondition(delegate
            {
                Thing thing = GetActor().jobs.curJob.GetTarget(TargetIndex.A).Thing;
                if (thing is Building && !thing.Spawned)
                {
                    return JobCondition.Incompletable;
                }
                return JobCondition.Ongoing;
            });
            this.FailOnBurningImmobile(TargetIndex.A);

            //send pawn to grower
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, 200, true);

            //Try to drop the product on the floor, if specified by the active Bill. If successful, end Job.
            yield return QEEToils.TryDropProductOnFloor(TargetThingA as Building_GrowerBase_WorkTable);

            //The product is going to a stockpile. Create product, decrement bill counter, and start carrying it.
            //If target stockpile full, drop on ground and end Job, as a fallback.
            yield return QEEToils.StartCarryProductToStockpile(TargetThingA as Building_GrowerBase_WorkTable);
            
            //if we've reached this far, an output stockpile was specified in the Bill options and a valid cell was found
            //  in the toil above
            yield return Toils_Reserve.Reserve(TargetIndex.B);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(TargetIndex.B);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.B, carryToCell, storageMode: true);
            Toil recount = new Toil();
            recount.initAction = delegate
            {
                Bill_Production bill_Production = recount.actor.jobs.curJob.bill as Bill_Production;
                if (bill_Production != null && bill_Production.repeatMode == BillRepeatModeDefOf.TargetCount)
                {
                    Map.resourceCounter.UpdateResourceCounts();
                }
            };
            yield return recount;
        }
    }
}
