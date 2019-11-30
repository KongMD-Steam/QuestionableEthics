using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// THIS IS A DEPRECATED CLASS. It is now only used in the legacy organ vat, and is here for save compatibility only.
    /// Building for growing things like organs. Requires constant maintenance in order to not botch the crafting. Dirty rooms increase maintenance drain even more.
    /// </summary>
    public class Building_VatGrower : Building_GrowerBase, IMaintainableGrower
    {
        static Building_VatGrower()
        {
            cleanlinessCurve.Add(-5.0f, 5.00f);
            cleanlinessCurve.Add(-2.0f, 1.75f);
            cleanlinessCurve.Add(0.0f, 1.0f);
            cleanlinessCurve.Add(0.4f, 0.35f);
            cleanlinessCurve.Add(2.0f, 0.1f);
        }

        public static SimpleCurve cleanlinessCurve = new SimpleCurve();

        /// <summary>
        /// Current active recipe being crafted.
        /// </summary>
        public GrowerRecipeDef activeRecipe;

        //public override int TicksNeededToCraft => activeRecipe?.craftingTime ?? 0;
        public override int TicksNeededToCraft => (int)(activeRecipe?.craftingTime * QEESettings.instance.organGrowthRateFloat ?? 0);

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
        /// </summary>
        public float scientistMaintenance;

        /// <summary>
        /// From 0.0 to 1.0. If the maintenance is below 50% there is a chance for failure.
        /// </summary>
        public float doctorMaintenance;

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

        private VatGrowerProperties vatGrowerPropsInt;

        public VatGrowerProperties VatGrowerProps
        {
            get
            {
                if (vatGrowerPropsInt == null)
                {
                    vatGrowerPropsInt = def.GetModExtension<VatGrowerProperties>();

                    //Fallback; Is defaults.
                    if (vatGrowerPropsInt == null)
                    {
                        vatGrowerPropsInt = new VatGrowerProperties();
                    }
                }

                return vatGrowerPropsInt;
            }
        }

        public float ScientistMaintenance { get => scientistMaintenance; set => scientistMaintenance = value; }

        public float DoctorMaintenance { get => doctorMaintenance; set => doctorMaintenance = value; }

        public Building_VatGrower() : base()
        {
            
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref activeRecipe, "activeRecipe");
            Scribe_Values.Look(ref scientistMaintenance, "scientistMaintenance");
            Scribe_Values.Look(ref doctorMaintenance, "doctorMaintenance");
        }

        public override string GetInspectString()
        {
            if(!(ParentHolder is Map))
            {
                return null;
            }

            StringBuilder builder = new StringBuilder(base.GetInspectString());

            if (status == CrafterStatus.Crafting)
            {
                builder.AppendLine();
                builder.AppendLine("QE_VatGrowerMaintenance".Translate(String.Format("{0:0%}", scientistMaintenance), 
                    String.Format("{0:0%}", doctorMaintenance)));

                builder.AppendLine("QE_VatGrowerCleanlinessMult".Translate(cleanlinessCurve.Evaluate(RoomCleanliness).ToString("0.00")));
            }

            return builder.ToString().TrimEndNewlines();
        }

        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            //Draw bottom graphic
            Vector3 drawAltitude = drawLoc;
            if(VatGrowerProps.bottomGraphic != null)
            {
                VatGrowerProps.bottomGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw product
            drawAltitude += new Vector3(0f, 0.005f, 0f);
            if ((status == CrafterStatus.Crafting || status == CrafterStatus.Finished) && activeRecipe != null && activeRecipe.productGraphic != null)
            {
                Material material = activeRecipe.productGraphic.Graphic.MatSingle;

                float scale = (0.2f + (CraftingProgressPercent * 0.8f)) * VatGrowerProps.productScaleModifier;
                Vector3 scaleVector = new Vector3(scale, 1f, scale);
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawAltitude + VatGrowerProps.productOffset, Quaternion.AngleAxis(0f, Vector3.up), scaleVector);

                Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
            }

            //Draw top graphic
            if (VatGrowerProps.topGraphic != null)
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.topGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw top detail graphic
            if (VatGrowerProps.topDetailGraphic != null && (PowerTrader?.PowerOn ?? false))
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.topDetailGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }

            //Draw glow graphic
            if ((status == CrafterStatus.Crafting || status == CrafterStatus.Finished) && VatGrowerProps.glowGraphic != null && (PowerTrader?.PowerOn ?? false))
            {
                drawAltitude += new Vector3(0f, 0.005f, 0f);
                VatGrowerProps.glowGraphic.Graphic.Draw(drawAltitude, Rotation, this);
            }
        }

        public override void Notify_CraftingStarted()
        {
            innerContainer.ClearAndDestroyContents();
        }

        public override void Notify_CraftingFinished()
        {
            Messages.Message("QE_MessageGrowingDone".Translate(activeRecipe.productDef.LabelCap), new LookTargets(this), MessageTypeDefOf.PositiveEvent, false);
        }

        public override void Tick_Crafting()
        {
            base.Tick_Crafting();

            //Deduct maintenance, fail if any of them go below 0%.
            float powerModifier = 1f;
            if (PowerTrader != null && !PowerTrader.PowerOn)
            {
                powerModifier = 15f;
            }
            float cleanlinessModifer = cleanlinessCurve.Evaluate(RoomCleanliness);
            float decayRate = 0.0012f * cleanlinessModifer * powerModifier / (QEESettings.instance.maintRateFloat);

            scientistMaintenance -= decayRate;
            doctorMaintenance -= decayRate;

            if(scientistMaintenance < 0f || doctorMaintenance < 0f)
            {
                //Fail the craft, waste all products.
                Reset();
                if (activeRecipe?.productDef?.defName != null)
                {
                    Messages.Message("QE_OrgaVatMaintFailMessage".Translate(activeRecipe.productDef.defName.Named("ORGANNAME")), new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message("QE_OrgaVatMaintFailFallbackMessage".Translate(), new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        public override bool TryExtractProduct(Pawn actor)
        {
            Thing product = ThingMaker.MakeThing(activeRecipe?.productDef);

            if (product == null || actor == null)
            {
                return false;
            }

            product.stackCount = activeRecipe?.productAmount ?? 1;

            if(status == CrafterStatus.Finished)
            {
                CraftingFinished();
            }

            //place product on the interaction cell of the grower
            bool placeSucceeded = GenPlace.TryPlaceThing(product, this.InteractionCell, this.Map, ThingPlaceMode.Near);

            //search for a better storage location
            IntVec3 storeCell;
            IHaulDestination haulDestination;
            if (placeSucceeded && StoreUtility.TryFindBestBetterStorageFor(product, actor, product.Map, StoragePriority.Unstored,
                actor.Faction, out storeCell, out haulDestination, false))
            {
                //try to haul product to better storage zone
                if (storeCell.IsValid || haulDestination != null)
                {
                    Job haulProductJob = HaulAIUtility.HaulToStorageJob(actor, product);
                    if (haulProductJob != null)
                    {
                        actor.jobs.StartJob(haulProductJob, JobCondition.Succeeded);
                    }
                }
            }

            return true;
        }

        public void StartCraftingRecipe(GrowerRecipeDef recipeDef)
        {
            //Setup recipe order
            orderProcessor.Reset();
            IngredientUtility.FillOrderProcessorFromVatGrowerRecipe(orderProcessor, recipeDef);
            orderProcessor.Notify_ContentsChanged();

            //Initialize maintenance
            scientistMaintenance = 0.25f;
            doctorMaintenance = 0.25f;

            activeRecipe = recipeDef;
            status = CrafterStatus.Filling;
        }

        public override void Notify_ThingLostInOrderProcessor()
        {
            StopCrafting();
        }

        public void StopCrafting()
        {
            craftingProgress = 0;
            orderProcessor.Reset();

            status = CrafterStatus.Idle;
            activeRecipe = null;
            if(innerContainer.Count > 0)
            {
                innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            }
        }

        public override string TransformStatusLabel(string label)
        {
            string recipeLabel = activeRecipe?.LabelCap ?? "QE_VatGrowerNoRecipe".Translate();

            if (status == CrafterStatus.Filling || status == CrafterStatus.Finished)
            {
                return label + " " + recipeLabel.CapitalizeFirst();
            }
            if (status == CrafterStatus.Crafting)
            {
                //return label + " " + recipeLabel.CapitalizeFirst() + " (" + CraftingProgressPercent.ToStringPercent() + ")";
                float daysRemaining = GenDate.TicksToDays(TicksLeftToCraft);
                if (daysRemaining > 1.0)
                {
                    return recipeLabel.CapitalizeFirst() + " (" + String.Format("{0:0.0}", daysRemaining) +
                        " " + "QE_VatGrowerDaysRemaining".Translate() + ")";
                }
                else
                {
                    return " " + recipeLabel.CapitalizeFirst() + " (" + String.Format("{0:0.0}", (TicksLeftToCraft / 2500.0f)) +
                        " " + "QE_VatGrowerHoursRemaining".Translate() + ")";
                }
            }

            return base.TransformStatusLabel(label);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if(status == CrafterStatus.Idle)
            {
                yield return new Command_Action()
                {
                    defaultLabel = "QE_VatGrowerStartCraftingGizmoLabel".Translate(),
                    defaultDesc = "QE_VatGrowerStartCraftingGizmoDescription".Translate(),
                    icon = ContentFinder<Texture2D>.Get("Things/Item/Health/HealthItemNatural", true),
                    order = -100,
                    action = delegate ()
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();

                        foreach (GrowerRecipeDef recipeDef in DefDatabase<GrowerRecipeDef>.AllDefs.OrderBy(def => def.orderID))
                        {
                            bool disabled = false;
                            if (recipeDef.requiredResearch != null && !recipeDef.requiredResearch.IsFinished)
                            {
                                disabled = true;
                            }

                            string label = null;
                            if (disabled)
                            {
                                label = "QE_VatGrowerStartCraftingFloatMenuDisabled".Translate(recipeDef.LabelCap, recipeDef.requiredResearch.LabelCap);
                            }
                            else
                            {
                                label = recipeDef.LabelCap;
                            }

                            FloatMenuOption option = new FloatMenuOption(label, delegate ()
                            {
                                StartCraftingRecipe(recipeDef);
                            });

                            option.Disabled = disabled;

                            options.Add(option);
                        }

                        if (options.Count > 0)
                        {
                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                    }
                };
            }
            else
            {
                if (status != CrafterStatus.Finished)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "QE_VatGrowerStopCraftingGizmoLabel".Translate(),
                        defaultDesc = "QE_VatGrowerStopCraftingGizmoDescription".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                        order = -100,
                        action = delegate ()
                        {
                            StopCrafting();
                        }
                    };
                    if (Prefs.DevMode)
                    {
                        yield return new Command_Action()
                        {
                            defaultLabel = "QE_VatGrowerDebugFinishGrowing".Translate(),
                            defaultDesc = "QE_OrganVatDebugFinishGrowingDescription".Translate(),
                            //icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true),
                            action = delegate ()
                            {
                                craftingProgress = TicksNeededToCraft;
                            }
                        };
                    }
                }
            }
        }
    }
}
