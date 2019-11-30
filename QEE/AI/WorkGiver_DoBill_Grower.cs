using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// Checks objects of type Building_GrowerBase_WorkTable for available bills and assigns jobs accordingly.
    /// Heavily-modified version of the WorkGiver_DoBill class in vanilla Rimworld (too many private members to sub-class).
    /// </summary>
    public class WorkGiver_DoBill_Grower : WorkGiver_Scanner
    {
        #region TranslatedStrings
        private static string NoBillsQueuedTrans,
            GrowerBusyTrans,
            NoIngredientsTrans,
            RecentIngSearchTrans,
            WorkTypeMismatchTrans,
            ShouldDoFalseTrans,
            GenericFailReasonTrans,
            IsBurningTrans,
            PawnRestrictionTrans,
            UnderAllowedSkillTrans,
            AboveAllowedSkillTrans,
            ReservedByTrans,
            ReservedTrans;
        #endregion

        #region Constructors
        public WorkGiver_DoBill_Grower()
        {
            //Initialized in this way, these strings will only be loaded once per Rimworld.exe load
            NoBillsQueuedTrans = "QE_JobFailReasonNoBillsQueued".Translate();
            GrowerBusyTrans = "QE_JobFailReasonGrowerBusy".Translate();
            NoIngredientsTrans = "QE_JobFailReasonNoIngredients".Translate();
            RecentIngSearchTrans = "QE_JobFailReasonRecentIngSearch".Translate();
            WorkTypeMismatchTrans = "QE_JobFailReasonWorkTypeMismatch".Translate();
            ShouldDoFalseTrans = "QE_JobFailReasonShouldDoFalse".Translate();
            GenericFailReasonTrans = "QE_JobFailReasonGeneric".Translate();
            IsBurningTrans = "BurningLower".Translate();
            PawnRestrictionTrans = "QE_JobFailReasonPawnRestriction".Translate();
            UnderAllowedSkillTrans = "UnderAllowedSkill".Translate();
            AboveAllowedSkillTrans = "AboveAllowedSkill".Translate();
            ReservedByTrans = "IsReservedBy".Translate();
            ReservedTrans = "Reserved".Translate();
        }
        #endregion Constructors

        /* return true if all conditions are met:
            a) any bills outstanding
            b) status is 'Idle'
            c) the usual reservation and forbidden checks pass 
        */
        public override bool HasJobOnThing(Pawn pawn, Thing aThing, bool forced = false)
        {
            Building_GrowerBase_WorkTable grower = aThing as Building_GrowerBase_WorkTable;
            IBillGiver billGiver = aThing as IBillGiver;
            BillProcessor processor = grower.billProc;
            LocalTargetInfo target = aThing;
            if (grower == null || billGiver == null)
            {
                return false;
            }

            if (grower.status == CrafterStatus.Crafting || grower.status == CrafterStatus.Finished)
            {
                JobFailReason.Is(GrowerBusyTrans);

                return false;
            }

            if (!processor.AnyPendingRequests)
            {
                JobFailReason.Is(NoBillsQueuedTrans);
                return false;
            }

            //check if the grower's cached ingredients search found ingredients for any bill
            if (grower.billProc.anyBillIngredientsAvailable == false)
            {
                JobFailReason.Is(NoIngredientsTrans);
                return false;
            }

            if (aThing.IsBurning())
            {
                JobFailReason.Is(IsBurningTrans);
                return false;
            }

            if (aThing.IsForbidden(pawn))
            {
                return false;
            }

            if (!ThingIsUsableBillGiver(aThing))
            {
                return false;
            }

            string reason = "";

            //if the grower isn't growing anything, loop through the billstack and check for any bill the pawn can do
            if (grower.billProc.ActiveBill == null)
            {
                if (GetBillPawnCanDo(pawn, grower, out reason) == null)
                {
                    JobFailReason.Is(reason);
                    return false;
                }
            }

            else if (!PawnCanDoThisBill(pawn, grower.billProc.ActiveBill, target, out reason))
            {
                JobFailReason.Is(reason);
                return false;
            }

            return true;
            
        }//end HasJobOnThing()

        public override Job JobOnThing(Pawn p, Thing t, bool forced = false)
        {
            IBillGiver billGiver = t as IBillGiver;
            Building_GrowerBase_WorkTable grower = t as Building_GrowerBase_WorkTable;
            BillProcessor processor = grower.billProc;
            
            //if null is returned from this function, the game will throw an error. Instead,
            //return this simple wait job to avoid unnecessary errors.
            Job returnJobOnFailure = new Job(JobDefOf.Wait, 5);

            //check if there are any pending requests (unfulfilled bills)
            if (!processor.AnyPendingRequests)
            {
                QEEMod.TryLog("AnyPendingRequests is false inside JobOnThing()!");
                return returnJobOnFailure;
            }

            //check if there are ingredients available for those bills
            if (processor.ingredientsAvailableNow.Count <= 0)
            {
                QEEMod.TryLog("ingredientsAvailableNow.Count is 0 inside JobOnThing()!");
                return returnJobOnFailure;
            }

            //get the reference to the currently active bill
            Bill theBill = processor.ActiveBill;
            if (theBill == null)
            {
                QEEMod.TryLog("Attempt to get ActiveBill failed inside JobOnThing()!");
                return returnJobOnFailure;
            }

            //get the cached ingredient that's available. This Thing is not always going to be used in the Job, 
            //but we know there's at least one stack of this Thing available on the map
            Thing cachedThing;
            grower.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out cachedThing);
            if (cachedThing == null)
            {
                QEEMod.TryLog("Attempt to retrieve cached ingredients failed for " + theBill.GetUniqueLoadID());
                return returnJobOnFailure;
            }

            //get the nearest Thing to the Pawn with the same ThingDef
            int countForVat = 0;
            Thing thingToFill = IngredientUtility.ThingPawnShouldRetrieveForBill(theBill, p, ref countForVat);
            if(thingToFill == null)
            {
                grower.billProc.anyBillIngredientsAvailable = false;
                QEEMod.TryLog("ThingPawnShouldRetrieveForBill() is null for " + theBill.GetUniqueLoadID());

                return returnJobOnFailure;
            }

            //all checks have passed! Return the Job and notify the grower that it's time to start Filling
            grower.Notify_FillingStarted(theBill);
                
            Job returnJob = new Job(QEJobDefOf.QE_LoadGrowerJob, t, thingToFill);
            returnJob.count = countForVat;
            returnJob.bill = theBill;
            return returnJob;

        }//end JobOnThing()

        /// <summary>
        /// Checks if a pawn can be assigned to a bill based on factors including PawnRestrictions, workSkills, etc.
        /// If a bill passes all the checks, it's assigned as the ActiveBill in the billProcessor.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="grower"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Bill GetBillPawnCanDo(Pawn p, Building_GrowerBase_WorkTable grower, out string reason)
        {
            reason = GenericFailReasonTrans;

            BillStack bills = grower.billStack;
            LocalTargetInfo target = grower;

            //loop through bills
            for (int i = 0; i < bills.Count; i++)
            {
                Bill_Production theBill = bills[i] as Bill_Production;

                if(!PawnCanDoThisBill(p, theBill, target, out reason))
                {
                    continue;
                }

                //check if cached ingredients search found ingredients for this bill
                Thing cachedThing;
                grower.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out cachedThing);
                if (cachedThing == null)
                {
                    QEEMod.TryLog("GetBillPawnCanDo - no ingredients available");
                    reason = NoIngredientsTrans;
                    continue;
                }

                grower.billProc.ActiveBill = theBill;
                QEEMod.TryLog(p.LabelShort + " can do bill " + theBill.GetUniqueLoadID());
                return theBill;
            }
            return null;

        } //end GetBillPawnCanDo()


        /// <summary>
        /// Checks if a pawn can be assigned to a bill based on factors including PawnRestrictions, workSkills, etc.
        /// Does not check if ingredients are nearby.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="theBill"></param>
        /// <param name="reason"></param>
        /// <returns></returns>
        public bool PawnCanDoThisBill(Pawn p, Bill theBill, LocalTargetInfo target, out string reason)
        {
            reason = GenericFailReasonTrans;
            if (Find.TickManager.TicksGame < theBill.lastIngredientSearchFailTicks + (QEESettings.instance.ingredientCheckIntervalSeconds * 60))
            {
                reason = RecentIngSearchTrans;
                return false;
            }

            if (theBill?.recipe?.requiredGiverWorkType != null && theBill.recipe.requiredGiverWorkType != def.workType)
            {
                reason = WorkTypeMismatchTrans;
                return false;
            }

            bool shouldDo = theBill.ShouldDoNow();

            if (!shouldDo)
            {
                reason = ShouldDoFalseTrans;
                return false;
            }

            if (theBill.pawnRestriction != null && theBill.pawnRestriction != p)
            {
                reason = string.Format(PawnRestrictionTrans, theBill.pawnRestriction.LabelShort);

                return false;
            }

            if (theBill.recipe.workSkill != null)
            {
                int level = p.skills.GetSkill(theBill.recipe.workSkill).Level;
                if (level < theBill.allowedSkillRange.min)
                {
                    reason = string.Format(UnderAllowedSkillTrans, theBill.allowedSkillRange.min);
                    return false;
                }
                if (level > theBill.allowedSkillRange.max)
                {
                    reason = string.Format(AboveAllowedSkillTrans, theBill.allowedSkillRange.max);
                    return false;
                }
            }

            if (!p.Map.reservationManager.CanReserve(p, target, 1, -1, null, false))
            {
                Pawn pawn2 = p.Map.reservationManager.FirstRespectedReserver(target, p);

                if (pawn2 != null)
                {
                    reason = string.Format(ReservedByTrans, theBill.billStack.billGiver.LabelShort, pawn2.LabelShort);
                }
                else
                {
                    reason = string.Format(ReservedTrans);
                }

                return false;
            }

            QEEMod.TryLog(p.LabelShort + " can do bill " + theBill.GetUniqueLoadID());
            return true;

        } //end PawnCanDoThisBill()

        public bool ThingIsUsableBillGiver(Thing thing)
        {
            if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Contains(thing.def))
            {
                return true;
            }

            return false;
        }

        // -------------------------------------------------------------------
        // Unchanged members and functions from Workgiver_DoBill class
        // -------------------------------------------------------------------

        //private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 600);

        public override ThingRequest PotentialWorkThingRequest
        {
            get
            {
                if (def.fixedBillGiverDefs != null && def.fixedBillGiverDefs.Count == 1)
                {
                    return ThingRequest.ForDef(def.fixedBillGiverDefs[0]);
                }
                return ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
            }
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Some;
        }

    }//end class WorkGiver_DoBill_Grower
}