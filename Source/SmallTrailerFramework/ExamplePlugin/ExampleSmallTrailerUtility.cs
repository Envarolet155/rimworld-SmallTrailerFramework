using System.Collections.Generic;
using System.Linq;
using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public static class ExampleSmallTrailerUtility
    {
        public static float ContentsMass(SmallTrailerState state)
        {
            if (state == null)
            {
                return 0f;
            }

            float mass = 0f;
            foreach (Thing thing in state.InnerContainer)
            {
                mass += ThingMass(thing);
            }
            return mass;
        }

        public static float ThingMass(Thing thing)
        {
            return thing == null ? 0f : thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
        }

        public static float UnitMass(Thing thing)
        {
            return thing == null ? 0f : thing.GetStatValue(StatDefOf.Mass);
        }

        public static int MaxLoadableCount(CompSmallTrailerUnit unit, Thing thing)
        {
            if (unit?.Extension == null || thing == null)
            {
                return 0;
            }

            float available = unit.Extension.massCapacity - ContentsMass(unit.State);
            if (available <= 0f)
            {
                return 0;
            }

            float unitMass = UnitMass(thing);
            if (unitMass <= 0f)
            {
                return thing.stackCount;
            }
            return Math.Min(Math.Max(Mathf.FloorToInt(available / unitMass), 0), thing.stackCount);
        }

        public static JobDef LoadJobDef => DefDatabase<JobDef>.GetNamed("STF_LoadSmallTrailer", false);
        public static JobDef AttachJobDef => DefDatabase<JobDef>.GetNamed("STF_AttachSmallTrailer", false);

        public static IEnumerable<Thing> TrailerTokens(Pawn pawn)
        {
            if (pawn?.inventory == null)
            {
                yield break;
            }
            for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
            {
                Thing thing = pawn.inventory.innerContainer[i];
                if (thing.TryGetComp<CompSmallTrailerUnit>() != null)
                {
                    yield return thing;
                }
            }
        }

        public static IEnumerable<CompSmallTrailerUnit> TrailerUnitsInCaravan(Caravan caravan)
        {
            if (caravan == null)
            {
                yield break;
            }
            List<Pawn> pawns = caravan.PawnsListForReading;
            for (int i = 0; i < pawns.Count; i++)
            {
                foreach (Thing token in TrailerTokens(pawns[i]))
                {
                    CompSmallTrailerUnit comp = token.TryGetComp<CompSmallTrailerUnit>();
                    if (comp != null)
                    {
                        yield return comp;
                    }
                }
            }
        }

        public static CompSmallTrailerUnit FindSpawnedTrailerById(Map map, string stableId)
        {
            if (map == null || stableId.NullOrEmpty())
            {
                return null;
            }
            List<Thing> things = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < things.Count; i++)
            {
                CompSmallTrailerUnit comp = things[i].TryGetComp<CompSmallTrailerUnit>();
                if (comp?.State.stableId == stableId)
                {
                    return comp;
                }
            }
            return null;
        }

        public static ISmallTrailerInventoryBridge BridgeFor(CompSmallTrailerUnit unit)
        {
            if (unit?.Extension == null)
            {
                return null;
            }
            SmallTrailerRegistry.TryGetInventoryBridge(unit.Extension.inventoryBridgeKey, out ISmallTrailerInventoryBridge bridge);
            return bridge;
        }
    }
}
