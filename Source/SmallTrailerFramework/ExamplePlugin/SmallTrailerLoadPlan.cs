using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class SmallTrailerLoadPlan : IExposable
    {
        public string stableId;
        public Thing trailerThing;
        public List<SmallTrailerLoadPlanEntry> entries = new List<SmallTrailerLoadPlanEntry>();

        public bool Complete => entries.All(e => e == null || e.Remaining <= 0 || e.thing == null || e.thing.Destroyed);

        public void ExposeData()
        {
            Scribe_Values.Look(ref stableId, "stableId");
            Scribe_References.Look(ref trailerThing, "trailerThing");
            Scribe_Collections.Look(ref entries, "entries", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && entries == null)
            {
                entries = new List<SmallTrailerLoadPlanEntry>();
            }
        }
    }

    public class SmallTrailerLoadPlanEntry : IExposable
    {
        public Thing thing;
        public int targetCount;
        public int loadedCount;

        public int Remaining => targetCount - loadedCount;

        public SmallTrailerLoadPlanEntry()
        {
        }

        public SmallTrailerLoadPlanEntry(Thing thing, int targetCount)
        {
            this.thing = thing;
            this.targetCount = targetCount;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref thing, "thing");
            Scribe_Values.Look(ref targetCount, "targetCount");
            Scribe_Values.Look(ref loadedCount, "loadedCount");
        }
    }
}
