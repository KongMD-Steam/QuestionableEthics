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
    /// THIS IS CLASS WILL BE DEPRECATED SOON. Do not use in new development work. It is now only used in the Pawn Vat.
    /// </summary>
    public class WorkGiver_MaintainGrower : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_GrowerBase grower = t as Building_GrowerBase;
            if (grower == null)
            {
                return false;
            }

            IMaintainableGrower maintanable = grower as IMaintainableGrower;
            if (maintanable == null)
            {
                return false;
            }

            if (t.IsForbidden(pawn))
            {
                return false;
            }

            if (!pawn.CanReserve(t))
            {
                return false;
            }

            if (grower.status != CrafterStatus.Crafting)
            {
                return false;
            }

            bool maintainScience = true;

            if(maintanable.ScientistMaintenance > QEESettings.instance.maintWorkThresholdFloat)
            {
                if (maintanable.ScientistMaintenance > 0.90f)
                    maintainScience = false;
                maintainScience = forced;
            }

            bool maintainDoctor = true;

            if (maintanable.DoctorMaintenance > QEESettings.instance.maintWorkThresholdFloat)
            {
                if (maintanable.DoctorMaintenance > 0.90f)
                    maintainDoctor = false;
                maintainDoctor = forced;
            }

            bool maintainAtAll = maintainScience || maintainDoctor;

            return maintainAtAll;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            //Building_GrowerBase grower = t as Building_GrowerBase;
            Job job = null;

            IMaintainableGrower maintanable = t as IMaintainableGrower;

            if (maintanable.ScientistMaintenance < QEESettings.instance.maintWorkThresholdFloat)
            {
                job = new Job(QEJobDefOf.QE_MaintainGrowerJob_Intellectual, t);
            }

            if (maintanable.DoctorMaintenance < QEESettings.instance.maintWorkThresholdFloat)
            {
                job = new Job(QEJobDefOf.QE_MaintainGrowerJob_Medicine, t);
            }

            return job;
        }
    }
}
