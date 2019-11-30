using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace QEthics
{
    /// <summary>
    /// THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
    /// Status for crafters deriving from QEthics.Building_GrowerBase. 
    /// </summary>
    public enum CrafterStatus
    {
        /// <summary>
        /// Waiting for player input.
        /// </summary>
        Idle,
        /// <summary>
        /// Colonists fill up the crafter with the desired things.
        /// </summary>
        Filling,
        /// <summary>
        /// The crafter is now crafting until its finished and revert back to Idle status.
        /// </summary>
        Crafting,
        /// <summary>
        /// The crafter is now finished. Awaiting for the product to be ejected.
        /// </summary>
        Finished
    }

    /// <summary>
    /// Base class for grower buildings.
    /// </summary>
    public abstract class Building_GrowerBase : Building, IThingHolder
    {
        private GrowerProperties growerPropsInt = null;

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

        /// <summary>
        /// Current progress being made during crafting.
        /// </summary>
        public int craftingProgress;

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

        /// <summary>
        /// Status of this crafter.
        /// </summary>
        public CrafterStatus status = CrafterStatus.Idle;

        /// <summary>
        /// Internal container representation of stored items.
        /// </summary>
        protected ThingOwner innerContainer = null;

        /// <summary>
        /// The crafter order processor. Is set by the player during Idle status.
        /// </summary>
        public ThingOrderProcessor orderProcessor;

        public Building_GrowerBase()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
            orderProcessor = new ThingOrderProcessor(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref craftingProgress, "craftingProgress");
            Scribe_Values.Look(ref status, "status");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this, false, LookMode.Deep);
            Scribe_Deep.Look(ref orderProcessor, "orderProcessor", this);
            /*if(Scribe.mode == LoadSaveMode.LoadingVars)
            {
                orderProcessor.Notify_ContentsChanged();
            }*/
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override string GetInspectString()
        {
            StringBuilder builder = new StringBuilder(base.GetInspectString());

            //Status
            builder.AppendLine();
            builder.AppendLine("QE_GrowerStatus".Translate() + ": " + TransformStatusLabel(("QE_GrowerStatus_" + status.ToString()).Translate()));

            //Ingredients: Needed
            if(status == CrafterStatus.Filling)
            {
                builder.AppendLine("QE_GrowerIngredientsNeeded".Translate() + ": " + orderProcessor.FormatCachedIngredientsInThingOrderProcessor());
            }

            //Crafting Progress
            /*if (status == CrafterStatus.Crafting)
            {
                builder.AppendLine("QE_GrowerCraftingProgress".Translate() + ": " + CraftingProgressPercent.ToStringPercent());
            }*/

            //Ingredients: Filled
            /*if (innerContainer.Count > 0)
            {
                builder.AppendLine("QE_GrowerIngredientsFilled".Translate() + ": " + innerContainer.FormatIngredientsInThingOwner());
            }*/

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
                            //Check if any Things are lost in the order processor.
                            orderProcessor.Cleanup();

                            if (orderProcessor.requestsLost)
                            {
                                //Abort if any of the requests were lost.
                                Reset();
                                Notify_ThingLostInOrderProcessor();
                                orderProcessor.requestsLost = false;
                                Tick_Idle();
                            }
                            else
                            {
                                Tick_Filling();
                            }
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

        }

        /// <summary>
        /// Filling tick.
        /// </summary>
        public virtual void Tick_Filling()
        {
            if(orderProcessor.PendingRequests.Count() <= 0)
            {
                //Log.Message("PendingRequests is 0. Starting crafting...");
                status = CrafterStatus.Crafting;
                Notify_CraftingStarted();
            }
        }

        public virtual void Notify_ThingLostInOrderProcessor()
        {

        }

        public virtual void Notify_StartedCarryThing(Pawn pawn)
        {

        }

        public virtual void Notify_CraftingStarted()
        {

        }

        public virtual void Notify_CraftingFinished()
        {

        }

        /// <summary>
        /// Crafting tick.
        /// </summary>
        public virtual void Tick_Crafting()
        {
            //Increment crafting.
            bool doCrafting = true;
            if(PowerTrader != null && !PowerTrader.PowerOn)
            {
                doCrafting = false;
            }
            if(doCrafting)
            {
                craftingProgress = craftingProgress + 60;
                if (craftingProgress >= TicksNeededToCraft)
                {
                    craftingProgress = TicksNeededToCraft;

                    status = CrafterStatus.Finished;

                    Notify_CraftingFinished();
                }
            }
        }

        /// <summary>
        /// Finished tick.
        /// </summary>
        public virtual void Tick_Finished()
        {
            
        }

        /// <summary>
        /// The crafting has finished.
        /// </summary>
        public virtual void CraftingFinished()
        {
            Reset();
        }

        public virtual void Reset()
        {
            craftingProgress = 0;
            status = CrafterStatus.Idle;
        }

        public void FillThing(Thing thing)
        {
            if(thing != null)
            {
                innerContainer.TryAddOrTransfer(thing, true);
                orderProcessor.Notify_ContentsChanged();
            }
        }

        public virtual bool TryExtractProduct(Pawn actor)
        {
            return true;
        }
    }
}
