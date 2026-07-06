using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework
{
    public static class SmallTrailerUtility
    {
        public static IEnumerable<Gizmo> GetDefaultGizmos(CompSmallTrailerUnit unit, ISmallTrailerModeHandler handler)
        {
            if (unit.parent.def.category == ThingCategory.Building)
            {
                if (handler is ISmallTrailerAttachGizmoProvider attachGizmoProvider)
                {
                    foreach (Gizmo gizmo in attachGizmoProvider.GetAttachGizmos(unit))
                    {
                        yield return gizmo;
                    }
                    yield break;
                }

                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandAttach".Translate(),
                    defaultDesc = "STF_CommandAttachDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", false),
                    action = () => OpenPawnPicker(unit, handler, SmallTrailerMode.Towing)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandDetach".Translate(),
                    defaultDesc = "STF_CommandDetachDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/UnloadTransporter", false),
                    action = () => TryDetachNearHolder(unit, handler)
                };

                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandCaravanRestore".Translate(),
                    defaultDesc = "STF_CommandCaravanRestoreDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan", false),
                    action = () => TryRestoreInventory(unit)
                };
            }
        }

        public static Pawn FindHoldingPawn(Thing thing)
        {
            if (thing == null)
            {
                return null;
            }

            IThingHolder holder = thing.ParentHolder;
            while (holder != null)
            {
                if (holder is Pawn_InventoryTracker inventory)
                {
                    return inventory.pawn;
                }
                if (holder is Pawn pawn)
                {
                    return pawn;
                }
                if (holder is Thing heldThing)
                {
                    holder = heldThing.ParentHolder;
                }
                else
                {
                    break;
                }
            }
            return null;
        }

        private static void OpenPawnPicker(CompSmallTrailerUnit unit, ISmallTrailerModeHandler handler, SmallTrailerMode targetMode)
        {
            Map map = unit.parent.Map;
            if (map == null)
            {
                Messages.Message("STF_MessageNoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.CanReach(unit.parent, PathEndMode.Touch, Danger.Deadly))
                .Select(p => new FloatMenuOption(p.LabelShortCap, () =>
                {
                    SmallTrailerResult result = handler.TryEnterMode(unit, targetMode, new List<Pawn> { p });
                    Report(result);
                }))
                .ToList();

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("STF_NoReachablePawn".Translate(), null));
            }
            Find.WindowStack.Add(new FloatMenu(options, "STF_SelectPawn".Translate()));
        }

        private static void TryDetachNearHolder(CompSmallTrailerUnit unit, ISmallTrailerModeHandler handler)
        {
            Pawn holder = FindHoldingPawn(unit.parent);
            if (holder == null || holder.Map == null)
            {
                Messages.Message("STF_MessageNoHoldingPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            Report(handler.TryLeaveMode(unit, unit.State.mode, holder.Map, holder.Position));
        }

        private static void TryRestoreInventory(CompSmallTrailerUnit unit)
        {
            SmallTrailerUnitDefExtension ext = unit.Extension;
            if (ext == null || !SmallTrailerRegistry.TryGetInventoryBridge(ext.inventoryBridgeKey, out ISmallTrailerInventoryBridge bridge))
            {
                Messages.Message("STF_MessageNoBridge".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn holder = FindHoldingPawn(unit.parent);
            IEnumerable<Pawn> pawns = holder?.Map?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>();
            Report(bridge.TryRestoreStateFromCaravan(unit.State, pawns));
        }

        public static void Report(SmallTrailerResult result)
        {
            if (result.Accepted)
            {
                Messages.Message("STF_MessageSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
            }
            else if (!result.Reason.NullOrEmpty())
            {
                Messages.Message(result.Reason, MessageTypeDefOf.RejectInput, false);
            }
        }
    }
}
