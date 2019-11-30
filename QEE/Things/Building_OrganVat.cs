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
    /// Building for growing things like organs. Requires constant maintenance in order to not botch the crafting. Dirty rooms increase maintenance drain even more.
    /// </summary>
    public class Building_OrganVat : Building_GrowerBase_WorkTable
    {
        /// <summary>
        /// Ticks required to craft organ. Takes organGrowthRate ModSetting into account.
        /// </summary>
        public override int TicksNeededToCraft => (int)(activeRecipe?.workAmount * QEESettings.instance.organGrowthRateFloat ?? 0);

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

        public Building_OrganVat() : base()
        {
            
        }

        public override string GetInspectString()
        {
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
            RecipeGraphicProperties recipeProps = activeRecipe?.GetModExtension<RecipeGraphicProperties>();

            if ((status == CrafterStatus.Crafting || status == CrafterStatus.Finished) && activeRecipe != null && 
                recipeProps?.productGraphic?.Graphic?.MatSingle != null)
            {
                Material material = recipeProps.productGraphic.Graphic.MatSingle;
                float scale = (0.2f + (CraftingProgressPercent * 0.8f)) * VatGrowerProps.productScaleModifier * recipeProps.scale;
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

        /// <summary>
        /// Initializes maintenance, active recipe, and bill variables used for growing. Sets CrafterStatus to 'Filling'.
        /// Called from WorkGiver_DoBill_Grower.JobOnThing()
        /// </summary>
        /// <param name="billToUse"></param>
        public override void Notify_FillingStarted(Bill billToUse)
        {
            //update Bill Processor
            //IngredientUtility.UpdateBillProcessorDesiredIng(billProc, billToUse);

            //TODO - is this necessary?
            billProc.UpdateDesiredRequests();

            //Initialize maintenance
            scientistMaintenance = 0.25f;
            doctorMaintenance = 0.25f;

            activeRecipe = billToUse.recipe;
            
            status = CrafterStatus.Filling;
        }

        public override void Notify_CraftingStarted()
        {
            billProc.Notify_ContentsChanged();

            status = CrafterStatus.Crafting;
        }

        public override void Notify_CraftingFinished()
        {
            Messages.Message("QE_MessageGrowingDone".Translate(activeRecipe.products[0].thingDef.LabelCap), new LookTargets(this), MessageTypeDefOf.PositiveEvent, false);
        }

        public override void Tick_Crafting()
        {
            base.Tick_Crafting();           

            if(scientistMaintenance < 0f || doctorMaintenance < 0f)
            {
                //Fail the craft, waste all products.
                StopCrafting(false);
                if (activeRecipe?.products[0]?.thingDef?.defName != null)
                {
                    Messages.Message("QE_OrgaVatMaintFailMessage".Translate(activeRecipe.products[0].thingDef.defName.Named("ORGANNAME")), 
                        new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message("QE_OrgaVatMaintFailFallbackMessage".Translate(), new LookTargets(this), MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        /// <summary>
        /// Called from JobDriver_ExtractProductFromGrower. That Job is initiated by WorkGiver_ExtractProductFromGrower, when crafting status is Finished.
        /// </summary>
        /// <param name="actor"></param>
        /// <returns></returns>
        public override void Notify_ProductExtracted(Pawn actor)
        {
            if (status == CrafterStatus.Finished)
            {
                StopCrafting(false);
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

            if (status != CrafterStatus.Idle)
            {

                bool shouldRefundIngredients = false;
                if (status == CrafterStatus.Filling)
                {
                    shouldRefundIngredients = true;
                }
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
                            StopCrafting(shouldRefundIngredients);
                        }
                    };
                    if (Prefs.DevMode)
                    {
                        yield return new Command_Action()
                        {
                            defaultLabel = "QE_VatGrowerDebugFinishGrowing".Translate(),
                            defaultDesc = "QE_OrganVatDebugFinishGrowingDescription".Translate(),
                            action = delegate ()
                            {
                                craftingProgress = TicksNeededToCraft;
                            }
                        };
                    }
                }
            }
        }
    } //end class Building_OrganVat
}
