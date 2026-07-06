using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ITab_SmallTrailerContents : ITab_ContentsBase
    {
        public ITab_SmallTrailerContents()
        {
            labelKey = "STF_TabContents";
            containedItemsKey = "STF_TabContentsContained";
        }

        public override IList<Thing> container
        {
            get
            {
                CompSmallTrailerUnit comp = SelThing?.TryGetComp<CompSmallTrailerUnit>();
                return comp?.State.InnerContainer;
            }
        }

        public override bool IsVisible
        {
            get
            {
                CompSmallTrailerUnit comp = SelThing?.TryGetComp<CompSmallTrailerUnit>();
                return SelThing?.Faction == Faction.OfPlayer || comp != null;
            }
        }

        protected override void OnDropThing(Thing t, int count)
        {
            CompSmallTrailerUnit comp = SelThing?.TryGetComp<CompSmallTrailerUnit>();
            if (comp == null || t == null || count <= 0)
            {
                return;
            }

            Thing dropped = t.SplitOff(count);
            if (GenDrop.TryDropSpawn(dropped, SelThing.Position + DropOffset, SelThing.Map, ThingPlaceMode.Near, out Thing _))
            {
                comp.State.SnapshotManifest();
                SmallTrailerGameComponent.Current?.Register(comp.State);
            }
        }
    }
}
