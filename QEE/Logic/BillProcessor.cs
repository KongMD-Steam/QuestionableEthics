using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace QEthics
{
    /// <summary>
    /// Helps process orders for Things in bills.
    /// </summary>
    public class BillProcessor : IExposable
    {

        #region Members
        /// <summary>
        /// Things we desire that can be anything.
        /// </summary>
        public Dictionary<string, ThingOrderRequest> desiredRequests = new Dictionary<string, ThingOrderRequest>();

        /// <summary>
        /// Cached value that is updated after a function checks for recipe ingredients on a interval. Designed to be checked 
        /// in a WorkGiver HasJobOnThing() function, or when the ingredientContainer contents change.
        /// </summary>
        public bool anyBillIngredientsAvailable = false;

        /// <summary>
        /// The holder object we are observing orders for.
        /// </summary>
        public IThingHolder observedThingHolder;

        public bool requestsLost = false;

        public Dictionary<string, Thing> ingredientsAvailableNow = new Dictionary<string, Thing>();

        //private static readonly IntRange ReCheckFailedBillTicksRange = new IntRange(500, 598);

        /// <summary>
        /// Unique LoadID of the bill being worked on. This value is saved in ExposeData().
        /// </summary>
        private string _activeBillID;

        /// <summary>
        /// Current bill being worked on. If null, crafter is not working on a bill. It is not saved in ExposeData().
        /// </summary>
        private Bill_Production _activeBill;

        #endregion Members

        #region Constructors

        public BillProcessor(IThingHolder observedThingHolder)
        {
            this.observedThingHolder = observedThingHolder;
            Notify_ContentsChanged();
        }

        public BillProcessor()
        {
            
        }

        #endregion Constructors

        #region Properties
        public ThingOwner ObservedThingOwner
        {
            get
            {
                return observedThingHolder.GetDirectlyHeldThings();
            }
        }

        public bool AnyPendingRequests
        {
            get
            {
                return desiredRequests.Count > 0;
            }
        }

        public Bill_Production ActiveBill
        {
            get
            {
                return _activeBill;
            }
            set
            {
                _activeBill = value;
                _activeBillID = value.GetUniqueLoadID();
            }
        }

        #endregion Properties

        #region Methods

        public bool UpdateAvailIngredientsCache()
        {
            anyBillIngredientsAvailable = false;
            ingredientsAvailableNow.Clear();

            if (AnyPendingRequests == false)
            {
                return false;
            }

            //Thing giver = (observedThingHolder as Thing);
            //QEEMod.TryLog("Updating available ingredients cache for " + giver.ThingID);

            List<Bill> bills = new List<Bill>();
            if (_activeBill != null)
            {
                bills.Add(_activeBill);
            }
            else
            {
                bills = (observedThingHolder as IBillGiver)?.BillStack.Bills;
            }

            Dictionary<string, Thing> ingsFoundOnMap = new Dictionary<string, Thing>();
            int checkIntervalTicks = QEESettings.instance.ingredientCheckIntervalSeconds * 60;
            for (int i = 0; i < bills.Count; i++)
            {
                Bill_Production curBill = bills[i] as Bill_Production;

                if (Find.TickManager.TicksGame < curBill.lastIngredientSearchFailTicks + checkIntervalTicks)
                {
                    QEEMod.TryLog("checked " + curBill.GetUniqueLoadID() + " for avail. ingredients recently, skipping");
                    continue;
                }
                curBill.lastIngredientSearchFailTicks = 0;

                if (curBill.ShouldDoNow())
                {

                    //loop through ingredients
                    foreach (IngredientCount curIng in curBill.recipe.ingredients)
                    {
                        Thing ing;

                        //if same ingredient was found on map in previous loop iteration, don't search map again
                        ingsFoundOnMap.TryGetValue(curIng.FixedIngredient.defName, out ing);
                        if (ing != null)
                        {
                            ingredientsAvailableNow[curBill.GetUniqueLoadID()] = ing;
                            //QEEMod.TryLog("Already found " + ing.Label + " on previous iteration. Skipping");
                            break;
                        }

                        //check if this ingredient is on the map
                        ing = IngredientUtility.FindClosestIngToBillGiver(curBill, curIng);
                        if (ing != null)
                        {
                            QEEMod.TryLog("Found " + ing.Label + " on map, adding to ingredient cache");
                            ingsFoundOnMap[curIng.FixedIngredient.defName] = ing;
                            ingredientsAvailableNow[curBill.GetUniqueLoadID()] = ing;
                            anyBillIngredientsAvailable = true;
                            break;
                        }
                        else
                        {
                            //QEEMod.TryLog("no ingredients available");
                        }
                    }

                    Thing dummy;
                    if(!ingredientsAvailableNow.TryGetValue(curBill.GetUniqueLoadID(), out dummy))
                    {
                        curBill.lastIngredientSearchFailTicks = Find.TickManager.TicksGame;
                    }
                }
            }

            return anyBillIngredientsAvailable;
        }

        /// <summary>
        /// Checks if any Things were lost in the bill processor.
        /// </summary>
        public void ValidateDesiredRequests()
        {
            int elementsRemoved = 0;

            foreach (KeyValuePair<string, ThingOrderRequest> entry in desiredRequests)
            {
                ThingOrderRequest orderRequest = entry.Value;
                if (orderRequest != null)
                {
                    if (orderRequest.HasThing)
                    { 
                        if(orderRequest.thing.Destroyed)
                        {
                            //Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " was destroyed");
                            elementsRemoved++;
                            desiredRequests.Remove(entry.Key);
                        }
                        else if (!(orderRequest.thing.ParentHolder is Pawn_CarryTracker ||
                      orderRequest.thing.ParentHolder is Map || orderRequest.thing.ParentHolder is IThingHolder))
                        {
                            //Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " did not spawn in valid container");
                            elementsRemoved++;
                            desiredRequests.Remove(entry.Key);
                        }
                    }
                }
                else
                {
                    //Log.Message("QEE: orderRequest is null");
                }
            }

            if (elementsRemoved > 0)
            {
                requestsLost = true;
            }
        }

        public void Reset()
        {
            _activeBillID = null;
            _activeBill = null;

            //recache desired requests and available ingredients
            UpdateDesiredRequests();
            UpdateAvailIngredientsCache();
        }

        /// <summary>
        /// Call this whenever contents change
        /// </summary>
        public void Notify_ContentsChanged()
        {
            ValidateDesiredRequests();

            //Recache our requests
            UpdateDesiredRequests();
        }

        public void UpdateDesiredRequests()
        {
            desiredRequests.Clear();

            List<Bill> bills = new List<Bill>();
            if (_activeBill != null)
            {
                bills.Add(_activeBill);
            }
            else
            {
                bills = (observedThingHolder as IBillGiver)?.BillStack.Bills;
            }

            for (int i = 0; i < bills.Count; i++)
            {
                Bill_Production theBill = bills[i] as Bill_Production;

                if(theBill?.recipe?.ingredients == null)
                {
                    continue;
                }

                foreach (IngredientCount curIng in theBill.recipe.ingredients)
                {
                    ThingFilter filterCopy = new ThingFilter();
                    filterCopy.CopyAllowancesFrom(curIng.filter);

                    ThingOrderRequest request = new ThingOrderRequest(filterCopy);

                    int storedCount = request.TotalStackCountForOrderRequestInContainer(ObservedThingOwner);
                    int countNeededFromRecipe = (int)(curIng.CountRequiredOfFor(curIng.FixedIngredient, theBill.recipe) *
                        QEESettings.instance.organTotalResourcesFloat);

                    int countNeededForCrafting = countNeededFromRecipe - storedCount;
                    countNeededForCrafting = countNeededForCrafting < 0 ? 0 : countNeededForCrafting;

                    if (countNeededForCrafting > 0)
                    {
                        //QEEMod.TryLog("Adding " + curIng.FixedIngredient.label + " amount: " + countNeededForCrafting);

                        request.amount = countNeededForCrafting;

                        desiredRequests[curIng.FixedIngredient.defName] = request;
                    }
                } //end foreach loop
            }//end for loop
        }//end UpdateDesiredRequests

        /// <summary>
        /// Updates the _activeBill variable from the billID. Should only need to be called when loading a save.
        /// </summary>
        public void UpdateActiveBill()
        {
            BillStack bills = (observedThingHolder as IBillGiver)?.BillStack;
            if (bills == null)
            {
                return;
            }

            foreach (Bill_Production curBill in bills)
            {
                if (curBill.GetUniqueLoadID() == _activeBillID)
                {
                    _activeBill = curBill;
                }
            }
        }

        //Inherited
        public void ExposeData()
        {
            Scribe_Values.Look(ref anyBillIngredientsAvailable, "anyBillIngredientsAvailable");
            Scribe_Values.Look(ref _activeBillID, "_activeBillID");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if(_activeBillID != null)
                {
                    UpdateActiveBill();
                }

                //Notify_ContentsChanged();
            }
        }

        #endregion Methods
    }
}
