using System.Collections.Generic;
using Verse;

namespace SmallTrailerFramework
{
    public class SmallTrailerState : IExposable, IThingHolder
    {
        public string stableId;
        public float hitPoints = 100f;
        public SmallTrailerMode mode = SmallTrailerMode.Building;
        public string lastPrimaryPawnLoadId;
        public int lastMapTile = -1;
        public IntVec3 lastCell = IntVec3.Invalid;
        public List<SmallTrailerThingRecord> manifest = new List<SmallTrailerThingRecord>();
        public Dictionary<string, string> extensionData = new Dictionary<string, string>();

        private ThingOwner<Thing> innerContainer;

        public IThingHolder ParentHolder => null;

        public ThingOwner<Thing> InnerContainer
        {
            get
            {
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this);
                }
                return innerContainer;
            }
        }

        public SmallTrailerState()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public void EnsureStableId()
        {
            if (stableId.NullOrEmpty())
            {
                stableId = "STF-" + Find.UniqueIDsManager.GetNextThingID();
            }
        }

        public void SnapshotManifest()
        {
            manifest.Clear();
            for (int i = 0; i < InnerContainer.Count; i++)
            {
                Thing thing = InnerContainer[i];
                manifest.Add(new SmallTrailerThingRecord(thing.def, thing.Stuff, thing.stackCount));
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return InnerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref stableId, "stableId");
            Scribe_Values.Look(ref hitPoints, "hitPoints", 100f);
            Scribe_Values.Look(ref mode, "mode", SmallTrailerMode.Building);
            Scribe_Values.Look(ref lastPrimaryPawnLoadId, "lastPrimaryPawnLoadId");
            Scribe_Values.Look(ref lastMapTile, "lastMapTile", -1);
            Scribe_Values.Look(ref lastCell, "lastCell", IntVec3.Invalid);
            Scribe_Collections.Look(ref manifest, "manifest", LookMode.Deep);
            Scribe_Collections.Look(ref extensionData, "extensionData", LookMode.Value, LookMode.Value);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (manifest == null)
                {
                    manifest = new List<SmallTrailerThingRecord>();
                }
                if (extensionData == null)
                {
                    extensionData = new Dictionary<string, string>();
                }
                if (innerContainer == null)
                {
                    innerContainer = new ThingOwner<Thing>(this);
                }
                EnsureStableId();
            }
        }
    }

    public class SmallTrailerThingRecord : IExposable
    {
        public ThingDef thingDef;
        public ThingDef stuffDef;
        public int count;

        public SmallTrailerThingRecord()
        {
        }

        public SmallTrailerThingRecord(ThingDef thingDef, ThingDef stuffDef, int count)
        {
            this.thingDef = thingDef;
            this.stuffDef = stuffDef;
            this.count = count;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Defs.Look(ref stuffDef, "stuffDef");
            Scribe_Values.Look(ref count, "count", 0);
        }
    }
}
