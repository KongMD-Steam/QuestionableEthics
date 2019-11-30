using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace QEthics
{
    public class WorkGiver_GrowerMaintenance : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(def.fixedBillGiverDefs.FirstOrDefault());

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_GrowerBase_WorkTable grower = t as Building_GrowerBase_WorkTable;
            if (grower == null)
            {
                return false;
            }

            IMaintainableGrower maintainable = grower as IMaintainableGrower;
            if (maintainable == null)
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

            bool shouldMaintainScience = true;

            if(maintainable.ScientistMaintenance > QEESettings.instance.maintWorkThresholdFloat)
            {
                if (maintainable.ScientistMaintenance > 0.90f)
                    shouldMaintainScience = false;
                shouldMaintainScience = forced;
            }

            bool shouldMaintainDoctor = true;

            if (maintainable.DoctorMaintenance > QEESettings.instance.maintWorkThresholdFloat)
            {
                if (maintainable.DoctorMaintenance > 0.90f)
                    shouldMaintainDoctor = false;
                shouldMaintainDoctor = forced;
            }

            bool maintainAtAll = shouldMaintainScience || shouldMaintainDoctor;

            return maintainAtAll;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Job job = null;

            IMaintainableGrower maintainable = t as IMaintainableGrower;

            if (maintainable.ScientistMaintenance < QEESettings.instance.maintWorkThresholdFloat)
            {
                job = new Job(QEJobDefOf.QE_GrowerMaintenanceJob_Intellectual, t);
            }

            if (maintainable.DoctorMaintenance < QEESettings.instance.maintWorkThresholdFloat)
            {
                job = new Job(QEJobDefOf.QE_GrowerMaintenanceJob_Medicine, t);
            }

            return job;
        }
    }
}
