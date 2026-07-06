using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework
{
    public class CompProperties_SmallTrailerUnit : CompProperties
    {
        public CompProperties_SmallTrailerUnit()
        {
            compClass = typeof(CompSmallTrailerUnit);
        }
    }

    public class CompSmallTrailerUnit : ThingComp, IThingHolder
    {
        private SmallTrailerState state;

        public SmallTrailerState State
        {
            get
            {
                if (state == null)
                {
                    state = new SmallTrailerState();
                }
                state.EnsureStableId();
                return state;
            }
        }

        public SmallTrailerUnitDefExtension Extension => parent.def.GetModExtension<SmallTrailerUnitDefExtension>();

        public void AdoptState(SmallTrailerState newState)
        {
            state = newState ?? new SmallTrailerState();
            state.EnsureStableId();
            SmallTrailerGameComponent.Current?.Register(state);
        }

        public SmallTrailerState ReleaseState()
        {
            SmallTrailerState released = State;
            state = new SmallTrailerState();
            state.EnsureStableId();
            return released;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            State.EnsureStableId();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref state, "smallTrailerState");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (state == null)
                {
                    state = new SmallTrailerState();
                }
                state.EnsureStableId();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            State.mode = parent.def.category == ThingCategory.Building ? SmallTrailerMode.Building : State.mode;
            State.lastMapTile = parent.Map?.Tile ?? -1;
            State.lastCell = parent.Position;
            SmallTrailerGameComponent.Current?.Register(State);
        }

        public override void CompTick()
        {
            State.InnerContainer.DoTick();
        }

        public override string CompInspectStringExtra()
        {
            return "STF_InspectState".Translate(State.mode.ToString(), State.InnerContainer.Count);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            SmallTrailerUnitDefExtension ext = Extension;
            if (ext == null)
            {
                yield break;
            }

            if (SmallTrailerRegistry.TryGetHandler(ext.handlerKey, out ISmallTrailerModeHandler handler))
            {
                foreach (Gizmo gizmo in SmallTrailerUtility.GetDefaultGizmos(this, handler))
                {
                    yield return gizmo;
                }
                if (handler is ISmallTrailerGizmoProvider gizmoProvider)
                {
                    foreach (Gizmo gizmo in gizmoProvider.GetGizmos(this))
                    {
                        yield return gizmo;
                    }
                }
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "STF_NoHandler".Translate(),
                    defaultDesc = "STF_NoHandlerDesc".Translate(ext.handlerKey ?? "null"),
                    Disabled = true,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/DesirePower", false)
                };
            }

            foreach (Thing item in State.InnerContainer)
            {
                Gizmo gizmo = Building.SelectContainedItemGizmo(parent, item);
                if (gizmo != null)
                {
                    yield return gizmo;
                }
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return State.InnerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }
    }
}
