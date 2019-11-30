using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// A modification of the JobDriver_DoBill class in vanilla Rimworld. Loads growers with ingredients from a Bill, completing when
    /// there are no more ingredients to load for the current bill.
    /// </summary>
    public class JobDriver_LoadGrower : JobDriver
    {
        public const PathEndMode GotoIngredientPathEndMode = PathEndMode.ClosestTouch;
        public const TargetIndex BillGiverInd = TargetIndex.A;
        public const TargetIndex IngredientInd = TargetIndex.B;
        int logCounter = 1;

        public IBillGiver BillGiver
        {
            get
            {
                IBillGiver billGiver = job.GetTarget(BillGiverInd).Thing as IBillGiver;
                if (billGiver == null)
                {
                    throw new InvalidOperationException("JobDriver_LoadGrower on non-Billgiver.");
                }
                return billGiver;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.CanReserve(TargetThingA))
            {
                return false;
            }

            if (!pawn.CanReserve(TargetThingB))
            {
                return false;
            }

            bool shouldStart = pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed) && pawn.Reserve(TargetThingB, job, errorOnFailed: errorOnFailed);

            //QEEMod.TryLog("TryMakePreToilRes for " + pawn.LabelShort + ": " + shouldStart);
            return shouldStart;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnBurningImmobile(BillGiverInd);
            this.FailOnDestroyedNullOrForbidden(BillGiverInd);
            this.FailOnForbidden(IngredientInd);
            this.FailOn(delegate
            {
                Thing building = job.GetTarget(BillGiverInd).Thing;
                IBillGiver billGiver = building as IBillGiver;
                if (building == null || billGiver == null || job.bill == null)
                {
                    QEEMod.TryLog("something is null, failing job");
                    return true;
                }

                Building_GrowerBase_WorkTable grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
                if (job.bill.DeletedOrDereferenced)
                {
                    QEEMod.TryLog("Bill deleted, failing job");

                    //refund ingredients if player cancels bill during Filling phase
                    if (grower != null && grower.status == CrafterStatus.Filling)
                    {
                        QEEMod.TryLog("Bill cancelled, refunding ingredients");
                        grower.StopCrafting(true);
                    }

                    return true;
                }

                if(grower.status != CrafterStatus.Filling)
                {
                    QEEMod.TryLog("Crafter is not 'filling', ending job");
                    return true;
                }

                return false;
            });


            Toil logToil = new Toil()
            {
                initAction = delegate ()
                {
                    QEEMod.TryLog("Pawn " + GetActor().LabelShort +" | Toil: " + logCounter + " | Job: " + job.GetUniqueLoadID());
                    logCounter++;
                }
            };
            ////yield return logToil;

            //travel to ingredient and carry it
            Toil reserveIng = Toils_Reserve.Reserve(IngredientInd);
            yield return reserveIng;
            //yield return logToil;

            Toil travelToil = Toils_Goto.GotoThing(IngredientInd, PathEndMode.OnCell);
            yield return travelToil;
            //yield return logToil;

            Toil carryThing = Toils_Haul.StartCarryThing(IngredientInd, subtractNumTakenFromJobCount: true);
            yield return carryThing;
            //yield return logToil;

            //Opportunistically haul a nearby ingredient of same ThingDef. Checks 8 square radius.
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIng, IngredientInd, TargetIndex.None, takeFromValidStorage: true);
            //yield return logToil;


            //head back to grower
            yield return Toils_Goto.GotoThing(BillGiverInd, PathEndMode.InteractionCell).FailOnDestroyedOrNull(IngredientInd);
            //yield return logToil;

            //deposit into grower
            Toil depositIntoGrower = new Toil()
            {
                initAction = delegate ()
                {
                    Building_GrowerBase_WorkTable grower = TargetThingA as Building_GrowerBase_WorkTable;
                    if (grower != null)
                    {
                        grower.FillThing(GetActor().carryTracker.CarriedThing);
                    }
                }
            };
            yield return depositIntoGrower;

            Toil tryStartCrafting = new Toil()
            {
                initAction = delegate ()
                {
                    Building_GrowerBase_WorkTable grower = job.GetTarget(BillGiverInd).Thing as Building_GrowerBase_WorkTable;
                    Pawn actor = GetActor();

                    //if all ingredients have been loaded, start crafting
                    if (grower != null && grower.RemainingCountForIngredient("all", true) == 0)
                    {
                        grower.Notify_CraftingStarted();
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            yield return tryStartCrafting;

        } //end function MakeNewToils()

    } //end class JobDriver_LoadGrower
}