using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using System.Diagnostics;
using System.Threading;

namespace QEthics
{
    /// <summary>
    /// Base class for grower buildings.
    /// </summary>
    public abstract class Building_GrowerBase_WorkTable : Building_WorkTable, IThingHolder, IMaintainableGrower, IBillGiverExtension
    {
        #region Members 

        private GrowerProperties growerPropsInt = null;

        /// <summary>
        /// Current active recipe being crafted.
        /// </summary>
        public RecipeDef activeRecipe;

        /// <summary>
        /// Status of this crafter.
        /// </summary>
        public CrafterStatus status = CrafterStatus.Idle;

        /// <summary>
        /// Internal container representation of stored items.
        /// </summary>
        protected ThingOwner ingredientContainer = null;

        /// <summary>
        /// Current progress being made during crafting.
        /// </summary>
        public int craftingProgress;

        /// <summary>
        /// Helps process bills for this building.
        /// </summary>
        public BillProcessor billProc;

        #endregion Members


        #region Constructors
        public Building_GrowerBase_WorkTable()
        {
            ingredientContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            billProc = new BillProcessor(this);
        }
        #endregion


        #region Properties

        /// <summary>
        /// Grower building properties.
        /// </summary>
        public GrowerProperties GrowerProps
        {
            get
            {
                if (growerPropsInt == null)
                {
                    growerPropsInt = def.GetModExtension<GrowerProperties>();

                    //Fallback; Is defaults.
                    if (growerPropsInt == null)
                    {
                        growerPropsInt = new GrowerProperties();
                    }
                }

                return growerPropsInt;
            }
        }

        public CompPowerTrader PowerTrader
        {
            get
            {
                return GetComp<CompPowerTrader>();
            }
        }

        /// <summary>
        /// Ticks needed until the crafting is finished.
        /// </summary>
        public abstract int TicksNeededToCraft { get; }

        public int TicksLeftToCraft
        {
            get
            {
                return TicksNeededToCraft - craftingProgress;
            }
        }

        public float CraftingProgressPercent
        {
            get
            {
                if (TicksNeededToCraft == 0)
                {
                    return 1;
                }
                else
                {
                    float percentComplete = (float)craftingProgress / (float)TicksNeededToCraft;
                    return percentComplete > 1.0f ? 1.0f : percentComplete;
                }
            }
        }

        #endregion


        #region IMaintainableGrower Implementation

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance hits 0, the recipe should fail. Default: 25%
        /// </summary>
        public float scientistMaintenance = 0.25f;

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance hits 0, the recipe should fail. Default: 25%
        /// </summary>
        public float doctorMaintenance = 0.25f;

        public static SimpleCurve cleanlinessCurve = new SimpleCurve();

        static Building_GrowerBase_WorkTable()
        {
            cleanlinessCurve.Add(-5.0f, 5.00f);
            cleanlinessCurve.Add(-2.0f, 1.75f);
            cleanlinessCurve.Add(0.0f, 1.0f);
            cleanlinessCurve.Add(0.4f, 0.35f);
            cleanlinessCurve.Add(2.0f, 0.1f);
        }

        public float RoomCleanliness
        {
            get
            {
                Room room = this.GetRoom(RegionType.Set_Passable);
                if (room != null)
                {
                    return room.GetStat(RoomStatDefOf.Cleanliness);
                }

                return 0f;
            }
        }

        public float ScientistMaintenance { get => scientistMaintenance; set => scientistMaintenance = value; }

        public float DoctorMaintenance { get => doctorMaintenance; set => doctorMaintenance = value; }

        #endregion


        #region IBillGiverExtension Implementation


        public void Notify_BillAdded(Bill theBill)
        {
            //QEEMod.TryLog("Bill Added!");
            billProc.UpdateDesiredRequests();
            billProc.UpdateAvailIngredientsCache();
        }
        public void Notify_BillDeleted(Bill theBill)
        {
            //QEEMod.TryLog("Bill Removed!");
            billProc.UpdateDesiredRequests();
            billProc.UpdateAvailIngredientsCache();
        }

        #endregion

        #region Methods

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref craftingProgress, "craftingProgress");
            Scribe_Values.Look(ref status, "status");
            Scribe_Deep.Look(ref ingredientContainer, "ingredientContainer", this, false, LookMode.Deep);
            Scribe_Values.Look(ref scientistMaintenance, "scientistMaintenance");
            Scribe_Values.Look(ref doctorMaintenance, "doctorMaintenance");
            Scribe_Defs.Look(ref activeRecipe, "activeRecipe");
            Scribe_Deep.Look(ref billProc, "billProc", this);

            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            //{
            //    billProc.anyBillIngredientsAvailable = true;
            //}
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return ingredientContainer;
        }

        public override string GetInspectString()
        {
            if (!(ParentHolder is Map))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder(base.GetInspectString());

            //Status
            builder.AppendLine();
            builder.AppendLine("QE_GrowerStatus".Translate() + ": " + TransformStatusLabel(("QE_GrowerStatus_" + status.ToString()).Translate()));

            //Ingredients: Needed
            if (status == CrafterStatus.Filling)
            {
                builder.Append("QE_GrowerIngredientsNeeded".Translate() + ": " + FormatCachedIngredients());
            }

            return builder.ToString().TrimEndNewlines();
        }


        public string FormatCachedIngredients(string format = "{0} x {1}", char delimiter = ',')
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < activeRecipe.ingredients.Count; i++)
            {
                ThingDef currIngredient = activeRecipe.ingredients[i].FixedIngredient;
                int remainingCount = RemainingCountForIngredient(currIngredient.defName);

                if (remainingCount > 0)
                {
                    //add a delimiter if any ingredients were added in previous iterations
                    if (builder.Length > 0)
                    {
                        builder.Append(delimiter);
                        builder.Append(' ');
                    }

                    builder.Append(string.Format(format, currIngredient.LabelCap, remainingCount));
                }

            }

            return builder.ToString().TrimEndNewlines();
        }

        public virtual string TransformStatusLabel(string label)
        {
            return label;
        }

        public override void Tick()
        {
            base.Tick();

            if (this.IsHashIntervalTick(60))
            {

                switch (status)
                {
                    case CrafterStatus.Idle:
                        {
                            Tick_Idle();
                        }
                        break;
                    case CrafterStatus.Filling:
                        {
                            Tick_Filling();
                        }
                        break;
                    case CrafterStatus.Crafting:
                        {
                            Tick_Crafting();
                        }
                        break;
                    case CrafterStatus.Finished:
                        {
                            Tick_Finished();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Idle tick.
        /// </summary>
        public virtual void Tick_Idle()
        {
            if (this.IsHashIntervalTick(60))
            {
                billProc.UpdateDesiredRequests();

                if (this.IsHashIntervalTick(QEESettings.instance.ingredientCheckIntervalSeconds * 60))
                {
                    billProc.UpdateAvailIngredientsCache();
                }
            }
        }

        /// <summary>
        /// Filling tick.
        /// </summary>
        public virtual void Tick_Filling()
        {
            if (this.IsHashIntervalTick(60))
            {
                //Check if any Things are lost in the order processor.
                billProc.ValidateDesiredRequests();

                if (billProc.requestsLost)
                {
                    //Abort if any of the requests were lost.
                    StopCrafting(true);
                    Notify_ThingLostInBillProcessor();
                    billProc.requestsLost = false;
                    Tick_Idle();
                }
                else if (this.IsHashIntervalTick(QEESettings.instance.ingredientCheckIntervalSeconds * 60))
                {
                    billProc.UpdateAvailIngredientsCache();
                }
            }
        }

        /// <summary>
        /// Crafting tick.
        /// </summary>
        public virtual void Tick_Crafting()
        {
            //Increment crafting.
            float powerModifier;
            if (PowerTrader == null || PowerTrader.PowerOn)
            {
                powerModifier = 1f;
                craftingProgress = craftingProgress + 60;
                if (craftingProgress >= TicksNeededToCraft)
                {
                    craftingProgress = TicksNeededToCraft;

                    status = CrafterStatus.Finished;

                    Notify_CraftingFinished();
                }
            }
            else
            {
                powerModifier = 15f;
            }

            //Set maintenance to decay
            float cleanlinessModifer = cleanlinessCurve.Evaluate(RoomCleanliness);
            float decayRate = 0.0012f * cleanlinessModifer * powerModifier / (QEESettings.instance.maintRateFloat);

            scientistMaintenance -= decayRate;
            doctorMaintenance -= decayRate;
        }

        /// <summary>
        /// Finished tick.
        /// </summary>
        public virtual void Tick_Finished()
        {

        }

        public virtual void Notify_StartedCarryThing(Pawn pawn)
        {

        }

        public virtual void Notify_FillingStarted(Bill activeBill)
        {

        }

        public virtual void Notify_CraftingStarted()
        {

        }

        public virtual void Notify_CraftingFinished()
        {

        }

        public virtual void Notify_ThingLostInBillProcessor()
        {

        }

        public void FillThing(Thing thing)
        {
            if (thing != null)
            {
                ingredientContainer.TryAddOrTransfer(thing, true);
                billProc.Notify_ContentsChanged();
            }
        }

        public virtual void Notify_ProductExtracted(Pawn actor)
        {

        }

        /// <summary>
        /// Stops the growing process and resets the vat. Refunds any ingredients used, if keepIngredients is true.
        /// This function is called if growing fails, succeeds, or is manually stopped via the Stop gizmo.
        /// It does not spawn any products.
        /// </summary>
        /// <param name="keepIngredients"></param>
        /// 
        public virtual void StopCrafting(bool keepIngredients = true)
        {
            QEEMod.TryLog("Stopping growing process. Keep Ingredients: " + keepIngredients);

            craftingProgress = 0;
            status = CrafterStatus.Idle;
            activeRecipe = null;
            billProc.Reset();

            if (ingredientContainer != null && ingredientContainer.Count > 0)
            {
                QEEMod.TryLog("Ingredient container count: " + ingredientContainer.Count);

                if (keepIngredients)
                {
                    bool wasSuccess = ingredientContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                    //QEEMod.TryLog("TryDropAll() success: " + wasSuccess);
                }
            }

            ingredientContainer.ClearAndDestroyContents();
        }

        /// <summary>
        /// Returns count of ingredient in recipe - count of that ingredient in the ingredientContainer
        /// </summary>
        /// <param name="thingDefName"></param>
        /// <returns></returns>
        public virtual int RemainingCountForIngredient(string thingDefName, bool countAll = false)
        {
            int totalCount = 0;
            for (int i = 0; i < activeRecipe.ingredients.Count; i++)
            {
                IngredientCount currIngredient = activeRecipe.ingredients[i];
                string currIngName = currIngredient.FixedIngredient.defName;

                if (thingDefName == currIngName || countAll == true)
                {
                    int wantedCount = (int)(currIngredient.CountRequiredOfFor(currIngredient.FixedIngredient, activeRecipe) * 
                        QEESettings.instance.organTotalResourcesFloat);

                    int haveCount = ingredientContainer?.FirstOrDefault(thing => thing?.def?.defName == currIngName)?.stackCount ?? 0;

                    int remainingCount = wantedCount - haveCount < 0 ? 0 : wantedCount - haveCount;
                    //QEEMod.TryLog(currIngredient.FixedIngredient.LabelCap + " wanted: " + wantedCount + " have: " + haveCount);

                    totalCount += remainingCount;

                    if (thingDefName == currIngName)
                    {
                        break;
                    }
                }
                else
                {
                    continue;
                }
            }
            return totalCount;
        }

        #endregion
    }
}
