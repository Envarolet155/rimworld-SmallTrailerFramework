using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ExampleSmallTrailerHandler : ISmallTrailerModeHandler, ISmallTrailerRotatedLeaveHandler, ISmallTrailerInventoryBridge, ISmallTrailerGizmoProvider, ISmallTrailerAttachGizmoProvider
    {
        public string Key => "SmallTrailerFramework.Example";

        public SmallTrailerResult CanEnterMode(CompSmallTrailerUnit unit, SmallTrailerMode targetMode, IReadOnlyList<Pawn> pawns)
        {
            if (unit?.Extension == null)
            {
                return SmallTrailerResult.Fail("STF_FailNoExtension".Translate());
            }
            if (targetMode == SmallTrailerMode.Towing && unit.Extension.packedThingDef == null)
            {
                return SmallTrailerResult.Fail("STF_FailNoPackedDef".Translate());
            }
            if (pawns == null || pawns.Count < unit.Extension.minPawns || pawns.Count > unit.Extension.maxPawns)
            {
                return SmallTrailerResult.Fail("STF_FailPawnCount".Translate(unit.Extension.minPawns, unit.Extension.maxPawns));
            }
            if (pawns.Any(p => p == null || p.Dead || p.Downed || p.inventory == null))
            {
                return SmallTrailerResult.Fail("STF_FailInvalidPawn".Translate());
            }
            return SmallTrailerResult.Success;
        }

        public SmallTrailerResult TryEnterMode(CompSmallTrailerUnit unit, SmallTrailerMode targetMode, IReadOnlyList<Pawn> pawns)
        {
            SmallTrailerResult can = CanEnterMode(unit, targetMode, pawns);
            if (!can.Accepted)
            {
                return can;
            }
            if (targetMode != SmallTrailerMode.Towing)
            {
                return SmallTrailerResult.Fail("STF_FailModeUnsupported".Translate(targetMode.ToString()));
            }

            Pawn primary = pawns[0];
            Thing source = unit.parent;
            SmallTrailerUnitDefExtension ext = unit.Extension;
            SmallTrailerState state = unit.ReleaseState();
            state.mode = SmallTrailerMode.Towing;
            state.lastPrimaryPawnLoadId = primary.GetUniqueLoadID();
            state.lastMapTile = primary.Map?.Tile ?? -1;
            state.lastCell = source.Position;

            Thing packed = ThingMaker.MakeThing(ext.packedThingDef);
            CompSmallTrailerUnit packedComp = packed.TryGetComp<CompSmallTrailerUnit>();
            if (packedComp == null)
            {
                return SmallTrailerResult.Fail("STF_FailPackedMissingComp".Translate());
            }
            packedComp.AdoptState(state);

            if (source.Spawned)
            {
                unit.SuppressNextContentDrop();
                source.DeSpawn(DestroyMode.Vanish);
            }
            if (!primary.inventory.innerContainer.TryAdd(packed))
            {
                GenPlace.TryPlaceThing(packed, primary.Position, primary.Map, ThingPlaceMode.Near);
                return SmallTrailerResult.Fail("STF_FailInventoryRejected".Translate(primary.LabelShortCap));
            }

            ExampleSmallTrailerMapComponent.EnsureTowingControlHediff(primary, removeIfMissing: false);
            SmallTrailerGameComponent.Current?.Register(state);
            return SmallTrailerResult.Success;
        }

        public SmallTrailerResult TryLeaveMode(CompSmallTrailerUnit unit, SmallTrailerMode currentMode, Map targetMap, IntVec3 targetCell)
        {
            return TryLeaveModeInternal(unit, currentMode, targetMap, targetCell, Rot4.Invalid, exactPlacement: false);
        }

        public SmallTrailerResult TryLeaveMode(CompSmallTrailerUnit unit, SmallTrailerMode currentMode, Map targetMap, IntVec3 targetCell, Rot4 rotation)
        {
            return TryLeaveModeInternal(unit, currentMode, targetMap, targetCell, rotation, exactPlacement: true);
        }

        private SmallTrailerResult TryLeaveModeInternal(CompSmallTrailerUnit unit, SmallTrailerMode currentMode, Map targetMap, IntVec3 targetCell, Rot4 rotation, bool exactPlacement)
        {
            if (unit?.Extension == null)
            {
                return SmallTrailerResult.Fail("STF_FailNoExtension".Translate());
            }
            ThingDef buildingDef = unit.Extension.buildingThingDef;
            if (buildingDef == null)
            {
                return SmallTrailerResult.Fail("STF_FailNoBuildingDef".Translate());
            }
            if (targetMap == null || !targetCell.IsValid)
            {
                return SmallTrailerResult.Fail("STF_MessageNoMap".Translate());
            }

            Thing building = ThingMaker.MakeThing(buildingDef);
            CompSmallTrailerUnit buildingComp = building.TryGetComp<CompSmallTrailerUnit>();
            if (buildingComp == null)
            {
                return SmallTrailerResult.Fail("STF_FailBuildingMissingComp".Translate());
            }

            SmallTrailerState state = unit.ReleaseState();
            state.mode = SmallTrailerMode.Building;
            state.lastMapTile = targetMap.Tile;
            state.lastCell = targetCell;
            buildingComp.AdoptState(state);

            bool placed;
            if (exactPlacement)
            {
                Rot4 rot = rotation.IsValid ? rotation : buildingDef.defaultPlacingRot;
                AcceptanceReport report = GenConstruct.CanPlaceBlueprintAt(buildingDef, targetCell, rot, targetMap);
                if (!report.Accepted)
                {
                    state.mode = currentMode;
                    unit.AdoptState(state);
                    return SmallTrailerResult.Fail(report.Reason.NullOrEmpty() ? "STF_FailPlaceBuilding".Translate() : report.Reason);
                }
                GenSpawn.Spawn(building, targetCell, targetMap, rot);
                placed = true;
            }
            else
            {
                placed = GenPlace.TryPlaceThing(building, targetCell, targetMap, ThingPlaceMode.Near);
            }
            if (!placed)
            {
                state.mode = currentMode;
                unit.AdoptState(state);
                return SmallTrailerResult.Fail("STF_FailPlaceBuilding".Translate());
            }

            unit.SuppressNextContentDrop();
            unit.parent.Destroy(DestroyMode.Vanish);
            SmallTrailerGameComponent.Current?.Register(state);
            return SmallTrailerResult.Success;
        }

        public SmallTrailerResult TryMoveStateToCaravan(SmallTrailerState state, Caravan caravan)
        {
            if (state == null || caravan == null)
            {
                return SmallTrailerResult.Fail("STF_FailInvalidStateOrCaravan".Translate());
            }

            state.SnapshotManifest();
            List<Thing> toMove = state.InnerContainer.ToList();
            for (int i = 0; i < toMove.Count; i++)
            {
                state.InnerContainer.Remove(toMove[i]);
                CaravanInventoryUtility.GiveThing(caravan, toMove[i]);
            }
            state.mode = SmallTrailerMode.Caravan;
            SmallTrailerGameComponent.Current?.Register(state);
            return SmallTrailerResult.Success;
        }

        public SmallTrailerResult TryRestoreStateFromCaravan(SmallTrailerState state, IEnumerable<Pawn> pawns)
        {
            if (state == null || pawns == null)
            {
                return SmallTrailerResult.Fail("STF_FailInvalidStateOrCaravan".Translate());
            }

            int restored = 0;
            foreach (SmallTrailerThingRecord record in state.manifest.ToList())
            {
                int remaining = record.count;
                foreach (Pawn pawn in pawns)
                {
                    if (pawn?.inventory == null)
                    {
                        continue;
                    }
                    for (int i = pawn.inventory.innerContainer.Count - 1; i >= 0 && remaining > 0; i--)
                    {
                        Thing item = pawn.inventory.innerContainer[i];
                        if (item.def != record.thingDef || item.Stuff != record.stuffDef)
                        {
                            continue;
                        }
                        int moved = pawn.inventory.innerContainer.TryTransferToContainer(item, state.InnerContainer, remaining);
                        remaining -= moved;
                        restored += moved;
                    }
                }
            }
            state.SnapshotManifest();
            SmallTrailerGameComponent.Current?.Register(state);
            return restored > 0 ? SmallTrailerResult.Success : SmallTrailerResult.Fail("STF_FailNothingRestored".Translate());
        }

        public IEnumerable<Gizmo> GetAttachGizmos(CompSmallTrailerUnit unit)
        {
            if (unit.parent.def.category != ThingCategory.Building)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "STF_CommandAttach".Translate(),
                defaultDesc = "STF_CommandAttachDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", false),
                action = () => BeginAttachTargeting(unit)
            };
        }

        public IEnumerable<Gizmo> GetGizmos(CompSmallTrailerUnit unit)
        {
            if (unit.parent.Spawned && unit.parent.def.category == ThingCategory.Building)
            {
                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandLoadNearby".Translate(),
                    defaultDesc = "STF_CommandLoadNearbyDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllCaravan", false),
                    action = () => Find.WindowStack.Add(new Dialog_SmallTrailerLoadItems(unit))
                };
            }

            if (unit.State.InnerContainer.Count > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandUnloadContents".Translate(),
                    defaultDesc = "STF_CommandUnloadContentsDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", false),
                    action = () => UnloadContents(unit)
                };
            }
        }

        private static void BeginAttachTargeting(CompSmallTrailerUnit unit)
        {
            TargetingParameters parms = TargetingParameters.ForColonist();
            Find.Targeter.BeginTargeting(parms, target =>
            {
                Pawn pawn = target.Thing as Pawn;
                JobDef jobDef = ExampleSmallTrailerUtility.AttachJobDef;
                if (pawn == null || jobDef == null || unit.parent.Map == null || pawn.Map != unit.parent.Map)
                {
                    Messages.Message("STF_FailInvalidPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                SmallTrailerResult can = CanAttachPawn(unit, pawn);
                if (!can.Accepted)
                {
                    Messages.Message(can.Reason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                Job job = JobMaker.MakeJob(jobDef, unit.parent);
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }

        private static SmallTrailerResult CanAttachPawn(CompSmallTrailerUnit unit, Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.inventory == null)
            {
                return SmallTrailerResult.Fail("STF_FailInvalidPawn".Translate());
            }
            if (!pawn.CanReserveAndReach(unit.parent, PathEndMode.Touch, Danger.Deadly))
            {
                return SmallTrailerResult.Fail("STF_NoReachablePawn".Translate());
            }
            return SmallTrailerResult.Success;
        }

        public static void UnloadContents(CompSmallTrailerUnit unit)
        {
            Map map = unit.parent.MapHeld;
            IntVec3 cell = unit.parent.Spawned ? unit.parent.Position : SmallTrailerUtility.FindHoldingPawn(unit.parent)?.Position ?? IntVec3.Invalid;
            if (map == null || !cell.IsValid)
            {
                Messages.Message("STF_MessageNoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<Thing> contents = unit.State.InnerContainer.ToList();
            for (int i = 0; i < contents.Count; i++)
            {
                unit.State.InnerContainer.Remove(contents[i]);
                GenPlace.TryPlaceThing(contents[i], cell, map, ThingPlaceMode.Near);
            }
            unit.State.SnapshotManifest();
            SmallTrailerGameComponent.Current?.Register(unit.State);
            Messages.Message("STF_MessageSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
        }
    }
}
