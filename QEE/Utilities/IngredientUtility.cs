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
    /// Utility functions for dealing with ingredients.
    /// </summary>
    public static class IngredientUtility
    {
        public static Thing FindClosestRequestForThingOrderProcessor(ThingOrderProcessor orderProcessor, Pawn finder)
        {
            Thing result = GenClosest.ClosestThingReachable(finder.Position, finder.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.OnCell, TraverseParms.For(finder),
                    validator: 
                    delegate(Thing thing)
                    {
                        if(thing.IsForbidden(finder))
                        {
                            return false;
                        }

                        foreach (ThingOrderRequest request in orderProcessor.PendingRequests)
                        {
                            if(request.ThingMatches(thing))
                            {
                                return true;
                            }
                        }

                        return false;
                    });

            return result;
        }

        public static string FormatIngredientsInThingOrderProcessor(this ThingOrderProcessor orderProcessor, string format = "{0} x {1}", char delimiter = ',')
        {
            StringBuilder builder = new StringBuilder();

            //Ingredients Wanted
            foreach (ThingOrderRequest ingredient in orderProcessor.desiredIngredients)
            {
                builder.Append(string.Format(format, ingredient.LabelCap, ingredient.amount));
                if (orderProcessor.desiredIngredients.Count > 1 && ingredient != orderProcessor.desiredIngredients.Last())
                {
                    builder.Append(delimiter);
                    builder.Append(' ');
                }
            }

            return builder.ToString().TrimEndNewlines();
        }

        public static string FormatCachedIngredientsInThingOrderProcessor(this ThingOrderProcessor orderProcessor, string format = "{0} x {1}", char delimiter = ',')
        {
            StringBuilder builder = new StringBuilder();

            //Ingredients Requested
            foreach (ThingOrderRequest request in orderProcessor.PendingRequests)
            {
                builder.Append(string.Format(format, request.LabelCap, request.amount));
                if (orderProcessor.PendingRequests.Count() > 1 && request != orderProcessor.PendingRequests.Last())
                {
                    builder.Append(delimiter);
                    builder.Append(' ');
                }
            }

            return builder.ToString().TrimEndNewlines();
        }

        public static string FormatIngredientsInThingOwner(this ThingOwner thingOwner, string format = "{0} x {1}", char delimiter = ',')
        {
            StringBuilder builder = new StringBuilder();

            //Ingredients Wanted
            foreach (Thing ingredient in thingOwner)
            {
                builder.Append(string.Format(format, ingredient.LabelCapNoCount, ingredient.stackCount));
                if (thingOwner.Count > 1 && ingredient != thingOwner.Last())
                {
                    builder.Append(delimiter);
                    builder.Append(' ');
                }
            }

            return builder.ToString().TrimEndNewlines();
        }

        public static void FillOrderProcessorFromVatGrowerRecipe(ThingOrderProcessor orderProcessor, GrowerRecipeDef recipeDef)
        {
            foreach (IngredientCount ingredientCount in recipeDef.ingredients)
            {
                ThingFilter filterCopy = new ThingFilter();
                filterCopy.CopyAllowancesFrom(ingredientCount.filter);

                ThingOrderRequest copy = new ThingOrderRequest(filterCopy);
                copy.amount = (int)(ingredientCount.GetBaseCount() * QEESettings.instance.organTotalResourcesFloat);

                orderProcessor.desiredIngredients.Add(copy);
            }
        }

        public static void FillOrderProcessorFromPawnKindDef(ThingOrderProcessor orderProcessor, PawnKindDef pawnKind)
        {
            ThingDef pawnThingDef = pawnKind.race;
            RaceProperties raceProps = pawnThingDef.race;

            //Assemble all required materials for a fully grown adult.
            ThingDef meatDef = raceProps.meatDef;
            float meatBaseNutrition = meatDef.ingestible.CachedNutrition;
            float meatAmount = pawnThingDef.GetStatValueAbstract(StatDefOf.MeatAmount);
            int nutritionAmount, proteinAmount;

            //protein cost = Meat * base Meat nutrition (vanilla is .05) * magic multiplier of 27
            //Minimum protein cost is 25, max is 750. Multiply by mod setting for clone ingredients
            //finally, round up to the nearest number divisible by 5
            proteinAmount = (int)(Math.Ceiling((meatAmount * meatBaseNutrition * 27f).Clamp(25, 750)
                * QEESettings.instance.cloneTotalResourcesFloat / 5) * 5);

            nutritionAmount = 4 * proteinAmount;

            {
                ThingOrderRequest orderRequest = new ThingOrderRequest(QEThingDefOf.QE_ProteinMash, proteinAmount);
                orderProcessor.desiredIngredients.Add(orderRequest);
            }

            {
                ThingOrderRequest orderRequest = new ThingOrderRequest(QEThingDefOf.QE_NutrientSolution, nutritionAmount);
                orderProcessor.desiredIngredients.Add(orderRequest);
            }
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        /// <summary>
        /// Finds the closest Thing on the map to a pawn that matches the ThingDef needed by the Bill.
        /// The available ingredients are cached in the BillProcessor variable in Building_GrowerBase_WorkTable.
        /// If the cached ThingDef is no longer available, it will look for another ThingDef that's needed.
        /// </summary>
        /// <param name="theBill"></param>
        /// <param name="finder"></param>
        /// <param name="countForVat"></param>
        /// <returns></returns>
        public static Thing ThingPawnShouldRetrieveForBill(Bill theBill, Pawn finder, ref int countForVat)
        {
            Building_GrowerBase_WorkTable vat = theBill.billStack.billGiver as Building_GrowerBase_WorkTable;
            ThingOwner vatStoredIngredients = vat.GetDirectlyHeldThings();

            Thing cachedThing = null;
            bool ingAreAvailable = vat.billProc.ingredientsAvailableNow.TryGetValue(theBill.GetUniqueLoadID(), out cachedThing);
            if (cachedThing == null)
            {
                QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - cachedThing is null");
                return null;
            }

            if (ingAreAvailable == false)
            {
                QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - ingAreAvailable is false");
                return null;
            }

            ThingRequest tRequest;
            ThingOrderRequest desiredRequest;

            vat.billProc.desiredRequests.TryGetValue(cachedThing.def.defName, out desiredRequest);

            //check that the vat still needs the cached ingredient before searching the map for the same ThingDef
            if (desiredRequest == null || desiredRequest.amount <= 0)
            {
                QEEMod.TryLog("Cached ingredient " + cachedThing.LabelShort + " is already fulfilled in vat. Looking for next ingredient in recipe");

                //this ingredient isn't in desiredIngredients or the vat has the full amount. Refresh desiredIngredients and try once more
                vat.billProc.UpdateDesiredRequests();

                //now get a random item in the dictionary of desired requests
                foreach (ThingOrderRequest value in vat.billProc.desiredRequests.Values)
                {
                    desiredRequest = value;
                }

                //return if there's no thingDef or desiredRequest is null
                if (desiredRequest == null)
                {
                    QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - Desired ThingOrderRequest is null");
                    return null;
                }

                //return if the amount for this request is 0
                if (desiredRequest.amount <= 0)
                {
                    QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - Desired Thing " + desiredRequest.Label + " has 0 amount");
                    return null;
                }
            }

            tRequest = desiredRequest.GetThingRequest();

            if(tRequest.IsUndefined)
            {
                QEEMod.TryLog("ThingPawnShouldRetrieveForBill() returning null. Reason - ThingRequest for " + desiredRequest.Label + 
                    " returned undefined ThingRequest");
                return null;
            }

            countForVat = desiredRequest.amount;


            QEEMod.TryLog("Searching map for closest " + desiredRequest.Label + " to " + finder.LabelShort);

            //search the map for the closest Thing to the pawn that matches the ThingDef in 'tRequest'
            Thing result = GenClosest.ClosestThingReachable(finder.Position, finder.Map, tRequest,
                PathEndMode.OnCell, TraverseParms.For(finder),
                validator:
                delegate (Thing testThing)
                {
                    if (tRequest.Accepts(testThing))
                    {
                        if (testThing.IsForbidden(finder))
                        {
                            return false;
                        }

                        if (!finder.CanReserve(testThing))
                        {
                            return false;
                        }
                        return true;
                    }

                    return false;
                });

            if (result != null)
            {
                QEEMod.TryLog(finder.LabelShort + " should retrieve: " + result.Label + " | stackCount: " + result.stackCount +
                    " | countForVat: " + countForVat);
            }

            return result;

        } //end FindClosestIngForBill

        public static Thing FindClosestIngToBillGiver(Bill theBill, IngredientCount curIng)
        {
            IBillGiver billGiver = theBill.billStack.billGiver;
            IThingHolder holder = billGiver as IThingHolder;
            Thing building = billGiver as Thing;
            ThingOwner vatStoredIngredients = holder?.GetDirectlyHeldThings();

            if (billGiver == null || building == null || billGiver == null || vatStoredIngredients == null)
            {
                return null;
            }

            int storedCount = vatStoredIngredients.FirstOrDefault(thing => thing.def == curIng.FixedIngredient)?.stackCount ?? 0;
            int countNeededFromRecipe = (int)(curIng.CountRequiredOfFor(curIng.FixedIngredient, theBill.recipe) * 
                QEESettings.instance.organTotalResourcesFloat);
                
            int countNeededForCrafting = countNeededFromRecipe - storedCount;
            countNeededForCrafting = countNeededForCrafting < 0 ? 0 : countNeededForCrafting;

            //only check the map for Things if the vat still needs some of this ingredient
            if (countNeededForCrafting > 0)
            {
                //find the closest accessible Thing of that ThingDef on the map
                ThingRequest tRequest = ThingRequest.ForDef(curIng.FixedIngredient);

                IEnumerable<Thing> searchSet = billGiver.Map.listerThings.ThingsMatching(tRequest);
                Thing result = GenClosest.ClosestThing_Global(building.Position, searchSet, 
                validator:
                delegate (Thing testThing)
                {
                    if (testThing.def.defName != curIng.FixedIngredient.defName)
                    {
                        return false;
                    }

                    if(testThing.IsForbidden(building.Faction))
                    {
                        return false;
                    }

                    return true;
                });

                //return the Thing, if we found one
                if (result != null)
                {
                    //QEEMod.TryLog("Ingredient found: " + curIng.FixedIngredient.label + " | stackCount: " + result.stackCount + " | recipe: "
                    //    + countNeededFromRecipe);
                    return result;
                }
            }

            return null;
        } //end function FindClosestIngToBillGiver


    } //end class IngredientUtility
}
