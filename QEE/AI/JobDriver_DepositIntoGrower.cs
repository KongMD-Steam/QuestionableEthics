using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse.AI;
using Verse;
using RimWorld;

namespace QEthics
{
    /// <summary>
    /// THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
    /// </summary>
    public class JobDriver_DepositIntoGrower : JobDriver
    {
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

            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed) && pawn.Reserve(TargetThingB, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Conditions and reserve
            this.FailOn(delegate ()
            {
                return TargetThingA is Building_GrowerBase vat && vat.status != CrafterStatus.Filling;
            });
            this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);
            yield return Toils_Reserve.Reserve(TargetIndex.A);
            
            Toil reserveIngredient = Toils_Reserve.Reserve(TargetIndex.B);
            yield return reserveIngredient;

            //Go and get the thing to carry.
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            Toil carryThing = Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true);
            carryThing.AddFinishAction(
                delegate()
                {
                    Building_GrowerBase grower = TargetThingA as Building_GrowerBase;
                    if (grower != null)
                    {
                        grower.Notify_StartedCarryThing(GetActor());
                    }
                });
            yield return carryThing;

            //Opportunistically haul a nearby ingredient of same ThingDef. Checks 8 square radius.
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, TargetIndex.B, TargetIndex.None, takeFromValidStorage: true);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, 100, true);
            yield return new Toil()
            {
                initAction = delegate ()
                {
                    Building_GrowerBase grower = TargetThingA as Building_GrowerBase;
                    if (grower != null)
                    {
                        grower.FillThing(GetActor().carryTracker.CarriedThing);
                    }
                }
            };
        }
    }
}
