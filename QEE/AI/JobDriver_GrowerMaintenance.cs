using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace QEthics
{
    public class JobDriver_GrowerMaintenance : JobDriver
    {
        public MaintainVatProperties VatJobProperties
        {
            get
            {
                return job.def.GetModExtension<MaintainVatProperties>();
            }
        }

        public IMaintainableGrower Maintainable
        {
            get
            {
                return TargetThingA as IMaintainableGrower;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.CanReserve(TargetThingA))
            {
                return false;
            }

            return pawn.Reserve(TargetThingA, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(delegate()
            {
                return TargetThingA is Building_GrowerBase_WorkTable vat && vat.status != CrafterStatus.Crafting;
            });
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil()
            {
                defaultCompleteMode = ToilCompleteMode.Never,
                tickAction = delegate()
                {
                    MaintainVatProperties props = VatJobProperties;
                    if (props.maintainingSkill == SkillDefOf.Intellectual)
                    {
                        Maintainable.ScientistMaintenance += (1f / 600f) * pawn.GetStatValue(StatDefOf.ResearchSpeed);
                        if (Maintainable.ScientistMaintenance >= 1f)
                        {
                            Maintainable.ScientistMaintenance = 1f;
                            EndJobWith(JobCondition.Succeeded);
                        }
                    }
                    else if (props.maintainingSkill == SkillDefOf.Medicine)
                    {
                        Maintainable.DoctorMaintenance += (1f / 600f) * pawn.GetStatValue(StatDefOf.MedicalTendSpeed);
                        if(Maintainable.DoctorMaintenance >= 1f)
                        {
                            Maintainable.DoctorMaintenance = 1f;
                            EndJobWith(JobCondition.Succeeded);
                        }
                    }

                    pawn.skills.Learn(props.maintainingSkill, 200f / 600f);
                }
            }.WithProgressBar(TargetIndex.A, 
            delegate()
            {
                MaintainVatProperties props = VatJobProperties;
                if(props.maintainingSkill == SkillDefOf.Intellectual)
                {
                    return Maintainable.ScientistMaintenance;
                }
                else if (props.maintainingSkill == SkillDefOf.Medicine)
                {
                    return Maintainable.DoctorMaintenance;
                }

                return 0f;
            }).
            WithEffect(EffecterDefOf.Research, TargetIndex.A).
            PlaySustainerOrSound(QESoundDefOf.Interact_Research);
        }
    }
}
