using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace QEthics
{
    /// <summary>
    /// THIS IS A DEPRECATED CLASS. It is here for save compatibility only.
    /// Extracts the product out of a Grower.
    /// </summary>
    public class JobDriver_ExtractGrowerProduct : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if(!pawn.CanReserve(TargetThingA))
            {
                return false;
            }

            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, 200, true);
            yield return new Toil()
            {
                initAction = delegate()
                {
                    Building_GrowerBase grower = TargetThingA as Building_GrowerBase;
                    if(grower != null)
                    {
                        grower.TryExtractProduct(GetActor());

                        EndJobWith(JobCondition.Succeeded);
                    }
                }
            };
        }
    }
}
