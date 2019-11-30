using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace QEthics
{
    public static class QEEToils
    {

        /// <summary>
        /// Checks if Bill specifies to drop product on ground, and if so, creates the product and drops it.
        /// The current grower's active Bill is retrieved programmatically via grower members.
        /// Heavily modified version of the Toils_Recipe.FinishRecipeAndStartStoringProduct() toil in vanilla.
        /// </summary>
        /// <returns></returns>
        public static Toil TryDropProductOnFloor(Building_GrowerBase_WorkTable vat)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Bill_Production activeBill = vat.billProc.ActiveBill;

                if (vat?.activeRecipe?.products[0]?.thingDef == null)
                {
                    if (vat?.activeRecipe?.label != null)
                    {
                        QEEMod.TryLog("No product found in recipe " + vat.activeRecipe.label + ".Ending extract Job.");
                    }
                    else
                    {
                        QEEMod.TryLog("No product found in recipe. Ending extract Job.");
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
                else if (activeBill == null || activeBill.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
                {
                    //true if no output stockpile specified in bill options

                    Thing product = ThingMaker.MakeThing(vat.activeRecipe.products[0].thingDef);
                    product.stackCount = vat.activeRecipe.products[0].count;

                    if (activeBill?.GetUniqueLoadID() != null)
                    {
                        QEEMod.TryLog("activeBill: " + activeBill?.GetUniqueLoadID() + " specifies dropping " + product.Label + " on floor.");
                    }
                    else
                        QEEMod.TryLog("activeBill is null, dropping " + product.Label + " on floor.");

                    //put the product on the ground on the same cell as the pawn
                    if (GenPlace.TryPlaceThing(product, actor.Position, actor.Map, ThingPlaceMode.Near))
                    {
                        vat.Notify_ProductExtracted(actor);
                    }
                    else
                    {
                        QEEMod.TryLog(actor + " could not drop recipe product " + product + " near " + actor.Position + ". Ending extract job.");
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            return toil;
        } // end TryDropProductOnFloor

        /// <summary>
        /// Generates the recipe product, then looks for a valid cell in the Bill's output stockpile.
        /// If a valid cell is found, puts the product into the pawn's arms for transport in a future Toil.
        /// Uses code from the Toils_Recipe.FinishRecipeAndStartStoringProduct() toil in vanilla.
        /// </summary>
        /// <param name="vat"></param>
        /// <returns></returns>
        public static Toil StartCarryProductToStockpile(Building_GrowerBase_WorkTable vat)
        {
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Pawn actor = toil.actor;
                Job curJob = actor.jobs.curJob;

                //Skip null checking, as it was done in previous toil

                Thing product = ThingMaker.MakeThing(vat.activeRecipe.products[0].thingDef);
                product.stackCount = vat.activeRecipe.products[0].count;
                Bill_Production activeBill = vat.billProc.ActiveBill;

                //decrement billstack count
                if (activeBill.repeatMode == BillRepeatModeDefOf.RepeatCount)
                {
                    if (activeBill.repeatCount > 0)
                    {
                        activeBill.repeatCount--;
                    }
                }

                IntVec3 foundCell = IntVec3.Invalid;

                //find the best cell to put the product in
                if (activeBill.GetStoreMode() == BillStoreModeDefOf.BestStockpile)
                {
                    StoreUtility.TryFindBestBetterStoreCellFor(product, actor, actor.Map, StoragePriority.Unstored, actor.Faction, out foundCell);
                }
                else if (activeBill.GetStoreMode() == BillStoreModeDefOf.SpecificStockpile)
                {
                    StoreUtility.TryFindBestBetterStoreCellForIn(product, actor, actor.Map, StoragePriority.Unstored, actor.Faction,
                        activeBill.GetStoreZone().slotGroup, out foundCell);
                }
                else
                {
                    Log.ErrorOnce("Unknown bill store mode", 9158246);
                }

                //if a cell was found in a stockpile, start a hauling job to move the product from the ground to that cell
                if (foundCell.IsValid)
                {
                    bool tryCarrySuccess = actor.carryTracker.TryStartCarry(product);
                    QEEMod.TryLog("Valid stockpile found - haul product to cell " + foundCell + ". TryStartCarry() result for " 
                        + actor.LabelShort + ": " + tryCarrySuccess);

                    curJob.targetB = foundCell;
                    curJob.targetA = product;
                    curJob.count = 99999;

                    vat.Notify_ProductExtracted(actor);

                    //the next toil in the JobDriver will now haul the carried object to the stockpile
                }
                else
                {
                    QEEMod.TryLog("No stockpile found to haul " + product.Label + " to. Dropping product on ground.");
                    if (GenPlace.TryPlaceThing(product, actor.Position, actor.Map, ThingPlaceMode.Near))
                    {
                        vat.Notify_ProductExtracted(actor);
                    }
                    else
                    {
                        QEEMod.TryLog(actor + " could not drop recipe product " + product + " near " + actor.Position + ". Ending extract job.");
                    }

                    actor.jobs.EndCurrentJob(JobCondition.Succeeded);
                }
            };
            return toil;
        } // end StartCarryProductToStockpile

    }
}
