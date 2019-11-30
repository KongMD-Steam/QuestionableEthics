using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace QEthics
{
    /// <summary>
    /// Helps process orders for Things in recipes.
    /// </summary>
    public class ThingOrderProcessor : IExposable
    {
        /// <summary>
        /// Things we desire that can be anything.
        /// </summary>
        public List<ThingOrderRequest> desiredIngredients = new List<ThingOrderRequest>();

        /// <summary>
        /// Contains the list of cached requests.
        /// </summary>
        private List<ThingOrderRequest> cachedRequests = new List<ThingOrderRequest>();

        /// <summary>
        /// The holder object we are observing orders for.
        /// </summary>
        public IThingHolder observedThingHolder;

        public bool requestsLost = false;

        public ThingOwner ObservedThingOwner
        {
            get
            {
                return observedThingHolder.GetDirectlyHeldThings();
            }
        }

        public IEnumerable<ThingOrderRequest> PendingRequests
        {
            get
            {
                return cachedRequests.AsReadOnly();
            }
        }

        //Constructors.
        public ThingOrderProcessor()
        {
            this.observedThingHolder = null;
        }

        public ThingOrderProcessor(IThingHolder observedThingHolder)
        {
            this.observedThingHolder = observedThingHolder;
        }

        //Functions
        //this function checks if any Things are lost in the order processor
        public void Cleanup()
        {
            int elementsRemoved = 0;

            //expand the lamda from the original QE mod, to allow better debug logging
            foreach (ThingOrderRequest orderRequest in desiredIngredients)
            {
                if (orderRequest != null)
                {
                    if (orderRequest.HasThing)
                    { 
                        if(orderRequest.thing.Destroyed)
                        {
                            Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " was destroyed");
                            elementsRemoved++;
                            desiredIngredients.Remove(orderRequest);
                        }
                        else if (!(orderRequest.thing.ParentHolder is Pawn_CarryTracker ||
                      orderRequest.thing.ParentHolder is Map || orderRequest.thing.ParentHolder is IThingHolder))
                        {
                            Log.Message("QEE: ABORTING CLONE! " + orderRequest.Label + " did not spawn in valid container");
                            elementsRemoved++;
                            desiredIngredients.Remove(orderRequest);
                        }
                    }
                }
                else
                {
                    //Log.Message("QEE: orderRequest is null");
                }
            }
            /*
            int elementsRemoved = desiredIngredients.RemoveAll(orderRequest => orderRequest == null || 
               (orderRequest.HasThing && 
                  //                               Check if not spawned in a valid container.
                  (orderRequest.thing.Destroyed || !(orderRequest.thing.ParentHolder is Pawn_CarryTracker || 
                  orderRequest.thing.ParentHolder is Map || orderRequest.thing.ParentHolder is IThingHolder))
               )
            );*/

            if (elementsRemoved > 0)
            {
                requestsLost = true;
            }
        }

        public void Reset()
        {
            //Log.Message("QEE: Resetting cached and desired ingredients...");
            cachedRequests.Clear();
            desiredIngredients.Clear();
        }

        /// <summary>
        /// Call this whenever contents change or upon loading the game.
        /// </summary>
        public void Notify_ContentsChanged()
        {
            Cleanup();

            //Recache our requests
            cachedRequests.Clear();

            IEnumerable<ThingOrderRequest> desiredRequests = GetDesiredRequests();
            if(desiredRequests != null)
            {
                cachedRequests.AddRange(desiredRequests);
                //Log.Message("QEE: cachedRequest count:" + PendingRequests.Count());
            }
            else
            {
                //Log.Message("QEE: Notify_ContentsChanged() desiredRequests is null!");
            }
        }

        /// <summary>
        /// Returns a series of ThingOrderRequests.
        /// </summary>
        /// <returns>Thing order requests.</returns>
        public IEnumerable<ThingOrderRequest> GetDesiredRequests()
        {
            if (observedThingHolder != null)
            {
                //Desired other things.
                foreach (ThingOrderRequest desiredIngredient in desiredIngredients)
                {
                    int countDifference = (int)desiredIngredient.amount - desiredIngredient.TotalStackCountForOrderRequestInContainer(ObservedThingOwner);
                    //Log.Message("QEE: " + desiredIngredient.Label + "; countDifference: " + countDifference);
                    if (countDifference > 0)
                    {
                        yield return new ThingOrderRequest(desiredIngredient, countDifference);
                    }
                }
            }
            else
            {
                //Log.Message("QEE: GetDesiredRequests() observedThingHolder is null!");
            }
        }

        //Inherited
        public void ExposeData()
        {
            Scribe_Collections.Look(ref desiredIngredients, "desiredThings", LookMode.Deep);
            if(Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Notify_ContentsChanged();
            }
        }
    }
}
