using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace QEthics
{
    public interface IBillGiverExtension
    {

        void Notify_BillAdded(Bill aBill);
        void Notify_BillDeleted(Bill aBill);
    }
}
