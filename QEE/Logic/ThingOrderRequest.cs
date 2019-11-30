using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace QEthics
{
    /// <summary>
    /// Represents a request for either a specified Thing or a ThingDef.
    /// </summary>
    public class ThingOrderRequest : IExposable
    {
        /// <summary>
        /// ThingDef from filter being requested.
        /// </summary>
        public ThingFilter thingFilter = null;

        /// <summary>
        /// Thing being requested.
        /// </summary>
        public Thing thing;

        /// <summary>
        /// How many are being requested.
        /// </summary>
        public int amount = 1;

        /// <summary>
        /// Label to use if its not null.
        /// </summary>
        public string customLabel = null;

        public bool HasThing
        {
            get
            {
                return thing != null;
            }
        }

        public bool HasThingFilter
        {
            get
            {
                return thingFilter != null;
            }
        }

        public string Label
        {
            get
            {
                if(customLabel != null)
                {
                    return customLabel;
                }
                if (HasThing)
                {
                    return thing.LabelNoCount;
                }
                if (HasThingFilter)
                {
                    return thingFilter.Summary;
                }

                return "<null>";
            }
        }

        public string LabelCap
        {
            get
            {
                if (customLabel != null)
                {
                    return customLabel.CapitalizeFirst();
                }
                if (HasThing)
                {
                    return thing.LabelCapNoCount;
                }
                if (HasThingFilter)
                {
                    return thingFilter.Summary.CapitalizeFirst();
                }

                return "<null>";
            }
        }

        public ThingRequest GetThingRequest()
        {
            if (HasThing)
            {
                return ThingRequest.ForDef(thing.def);
            }
            if (HasThingFilter)
            {
                return thingFilter.BestThingRequest;
            }

            return ThingRequest.ForUndefined();
        }

        public ThingOrderRequest()
        {
            
        }

        public ThingOrderRequest(ThingOrderRequest other)
        {
            customLabel = other.customLabel;
            thing = other.thing;
            amount = other.amount;
            if(other.HasThingFilter)
            {
                ThingFilter filter = new ThingFilter();
                filter.CopyAllowancesFrom(other.thingFilter);
                thingFilter = filter;
            }
            Initialize();
        }

        public ThingOrderRequest(ThingOrderRequest other, int amount)
        {
            customLabel = other.customLabel;
            thing = other.thing;
            this.amount = amount;
            if (other.HasThingFilter)
            {
                ThingFilter filter = new ThingFilter();
                filter.CopyAllowancesFrom(other.thingFilter);
                thingFilter = filter;
            }
            Initialize();
        }

        public ThingOrderRequest(int amount = 1)
        {
            this.amount = amount;
            Initialize();
        }

        public ThingOrderRequest(Thing thing, int amount = 1)
        {
            this.thing = thing;
            this.amount = amount;
            Initialize();
        }

        public ThingOrderRequest(Thing thing, ThingDef thingDef, int amount = 1)
        {
            this.thing = thing;
            ThingFilter filter = new ThingFilter();
            filter.SetAllow(thingDef, true);
            thingFilter = filter;
            this.amount = amount;
            Initialize();
        }

        public ThingOrderRequest(ThingDef thingDef, int amount = 1)
        {
            ThingFilter filter = new ThingFilter();
            filter.SetAllow(thingDef, true);
            thingFilter = filter;
            this.amount = amount;
            Initialize();
        }

        public ThingOrderRequest(ThingFilter thingFilter, int amount = 1)
        {
            this.thingFilter = thingFilter;
            this.amount = amount;
            Initialize();
        }

        /// <summary>
        /// Initializes the the order request.
        /// </summary>
        public void Initialize()
        {
            if(thingFilter != null)
            {
                thingFilter.ResolveReferences();
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref customLabel, "customLabel");
            Scribe_Deep.Look(ref thingFilter, "thingFilter");
            Scribe_References.Look<Thing>(ref thing, "thing", false);
            Scribe_Values.Look(ref amount, "amount", 1);
        }

        public bool ThingMatches(Thing thing)
        {
            if (HasThing)
            {
                return this.thing == thing;
            }
            if (HasThingFilter)
            {
                return thingFilter.Allows(thing);
            }

            return false;
        }

        public bool ThingDefMatches(Thing aThing)
        {
            if (HasThing)
            {
                return this.thing.def == aThing.def;
            }
            if (HasThingFilter)
            {
                return thingFilter.Allows(aThing);
            }

            return false;
        }

        public static implicit operator ThingOrderRequest(Thing thing)
        {
            return new ThingOrderRequest(thing);
        }

        public static implicit operator ThingOrderRequest(ThingFilter thingFilter)
        {
            return new ThingOrderRequest(thingFilter);
        }

        public static implicit operator ThingOrderRequest(ThingDef thingDef)
        {
            return new ThingOrderRequest(thingDef);
        }
    }
}
