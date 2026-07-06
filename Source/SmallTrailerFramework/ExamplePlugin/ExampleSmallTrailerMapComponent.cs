using System.Collections.Generic;
using System.Linq;
using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ExampleSmallTrailerMapComponent : MapComponent
    {
        private readonly List<Thing> tmpTrailers = new List<Thing>();
        private List<SmallTrailerLoadPlan> loadPlans = new List<SmallTrailerLoadPlan>();

        public bool HasLoadPlans => loadPlans.Any(p => p != null && !p.Complete);

        public ExampleSmallTrailerMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref loadPlans, "smallTrailerLoadPlans", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && loadPlans == null)
            {
                loadPlans = new List<SmallTrailerLoadPlan>();
            }
        }

        public override void MapComponentDraw()
        {
            tmpTrailers.Clear();
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.inventory == null)
                {
                    continue;
                }
                for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                {
                    Thing thing = pawn.inventory.innerContainer[j];
                    CompSmallTrailerUnit comp = thing.TryGetComp<CompSmallTrailerUnit>();
                    if (comp?.State.mode == SmallTrailerMode.Towing)
                    {
                        DrawTrailerForPawn(thing, pawn);
                    }
                }
            }
            tmpTrailers.Clear();
        }

        public override void MapComponentTick()
        {
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                TryRestoreCaravanTrailer(pawn);
                ClearAutoUnloadForTowingPawn(pawn);
                if (Find.TickManager.TicksGame % 250 != 0)
                {
                    continue;
                }
                if (pawn.Drafted || pawn.inventory == null || !HasTowedTrailer(pawn))
                {
                    EnsureTowingControlHediff(pawn, removeIfMissing: true);
                    continue;
                }
                EnsureTowingControlHediff(pawn, removeIfMissing: false);
            }
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                CleanupLoadPlans();
            }
        }

        public void SetLoadPlan(CompSmallTrailerUnit unit, Dictionary<Thing, int> counts)
        {
            if (unit == null || counts == null)
            {
                return;
            }

            loadPlans.RemoveAll(p => p == null || p.stableId == unit.State.stableId);
            SmallTrailerLoadPlan plan = new SmallTrailerLoadPlan
            {
                stableId = unit.State.stableId,
                trailerThing = unit.parent
            };

            foreach (KeyValuePair<Thing, int> pair in counts)
            {
                if (pair.Key != null && pair.Value > 0)
                {
                    plan.entries.Add(new SmallTrailerLoadPlanEntry(pair.Key, pair.Value));
                }
            }

            if (plan.entries.Count > 0)
            {
                loadPlans.Add(plan);
            }
        }

        public Job TryMakeLoadJob(Pawn pawn)
        {
            JobDef jobDef = ExampleSmallTrailerUtility.LoadJobDef;
            if (jobDef == null || pawn?.Map != map)
            {
                return null;
            }

            CleanupLoadPlans();
            for (int i = 0; i < loadPlans.Count; i++)
            {
                SmallTrailerLoadPlan plan = loadPlans[i];
                Thing trailer = plan.trailerThing;
                CompSmallTrailerUnit unit = trailer?.TryGetComp<CompSmallTrailerUnit>();
                if (unit == null || !trailer.Spawned || trailer.Map != map || !pawn.CanReserveAndReach(trailer, PathEndMode.Touch, Danger.Deadly))
                {
                    continue;
                }

                for (int j = 0; j < plan.entries.Count; j++)
                {
                    SmallTrailerLoadPlanEntry entry = plan.entries[j];
                    Thing item = entry?.thing;
                    if (entry == null || entry.Remaining <= 0 || item == null || !item.Spawned || item.Map != map || item.IsForbidden(pawn))
                    {
                        continue;
                    }
                    if (!pawn.CanReserveAndReach(item, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        continue;
                    }

                    int maxByTrailer = ExampleSmallTrailerUtility.MaxLoadableCount(unit, item);
                    int maxByPawn = pawn.carryTracker.AvailableStackSpace(item.def);
                    int count = Math.Min(Math.Min(entry.Remaining, item.stackCount), Math.Min(maxByTrailer, maxByPawn));
                    if (count <= 0)
                    {
                        continue;
                    }

                    Job job = JobMaker.MakeJob(jobDef, item, trailer);
                    job.count = count;
                    return job;
                }
            }
            return null;
        }

        public void NotifyLoaded(string stableId, Thing thing, int count)
        {
            if (stableId.NullOrEmpty() || thing == null || count <= 0)
            {
                return;
            }

            SmallTrailerLoadPlan plan = loadPlans.FirstOrDefault(p => p != null && p.stableId == stableId);
            SmallTrailerLoadPlanEntry entry = plan?.entries.FirstOrDefault(e => e != null && e.Remaining > 0 && e.thing == thing);
            if (entry == null)
            {
                entry = plan?.entries.FirstOrDefault(e => e != null && e.Remaining > 0 && e.thing != null && e.thing.def == thing.def && e.thing.Stuff == thing.Stuff);
            }
            if (entry != null)
            {
                entry.loadedCount += count;
            }
            CleanupLoadPlans();
        }

        private void CleanupLoadPlans()
        {
            loadPlans.RemoveAll(p => p == null || p.Complete || p.trailerThing == null || p.trailerThing.Destroyed);
            for (int i = 0; i < loadPlans.Count; i++)
            {
                loadPlans[i].entries.RemoveAll(e => e == null || e.Remaining <= 0 || e.thing == null || e.thing.Destroyed);
            }
        }

        private static bool HasTowedTrailer(Pawn pawn)
        {
            return HediffComp_SmallTrailerTowingControl.TowedTrailers(pawn).Any();
        }

        private void TryRestoreCaravanTrailer(Pawn pawn)
        {
            if (pawn?.inventory == null)
            {
                return;
            }

            for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
            {
                CompSmallTrailerUnit unit = pawn.inventory.innerContainer[i].TryGetComp<CompSmallTrailerUnit>();
                if (unit?.State.mode != SmallTrailerMode.Caravan)
                {
                    continue;
                }

                ISmallTrailerInventoryBridge bridge = ExampleSmallTrailerUtility.BridgeFor(unit);
                if (bridge == null)
                {
                    continue;
                }
                bridge.TryRestoreStateFromCaravan(unit.State, map.mapPawns.FreeColonistsSpawned);
                unit.State.mode = SmallTrailerMode.Towing;
                pawn.inventory.UnloadEverything = false;
                SmallTrailerGameComponent.Current?.Register(unit.State);
            }
        }

        private static void ClearAutoUnloadForTowingPawn(Pawn pawn)
        {
            if (pawn?.inventory != null && HasTowedTrailer(pawn))
            {
                pawn.inventory.UnloadEverything = false;
            }
        }

        public static void EnsureTowingControlHediff(Pawn pawn, bool removeIfMissing)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("STF_TowingControl");
            if (def == null || pawn.health == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            bool hasTrailer = HasTowedTrailer(pawn);
            if (hasTrailer && existing == null)
            {
                pawn.health.AddHediff(def);
            }
            else if (!hasTrailer && removeIfMissing && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static void DrawTrailerForPawn(Thing trailer, Pawn pawn)
        {
            Rot4 rot = pawn.Rotation;
            Vector3 offset = DrawOffsetForPawn(rot);
            Vector3 loc = pawn.DrawPos + offset;
            loc.y = AltitudeLayer.Item.AltitudeFor();
            trailer.Graphic.Draw(loc, DrawRotForPawn(rot), trailer);
        }

        private static Vector3 DrawOffsetForPawn(Rot4 pawnRot)
        {
            if (pawnRot == Rot4.North)
            {
                return new Vector3(0f, 0f, 1.18f);
            }
            if (pawnRot == Rot4.South)
            {
                return new Vector3(0f, 0f, -1.18f);
            }
            return pawnRot.FacingCell.ToVector3() * -0.72f;
        }

        private static Rot4 DrawRotForPawn(Rot4 pawnRot)
        {
            if (pawnRot == Rot4.East)
            {
                return Rot4.West;
            }
            if (pawnRot == Rot4.West)
            {
                return Rot4.East;
            }
            return pawnRot;
        }
    }
}
