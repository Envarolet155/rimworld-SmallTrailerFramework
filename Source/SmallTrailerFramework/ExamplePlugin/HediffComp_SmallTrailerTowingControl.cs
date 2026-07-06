using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class HediffCompProperties_SmallTrailerTowingControl : HediffCompProperties
    {
        public HediffCompProperties_SmallTrailerTowingControl()
        {
            compClass = typeof(HediffComp_SmallTrailerTowingControl);
        }
    }

    public class HediffComp_SmallTrailerTowingControl : HediffComp
    {
        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn?.inventory == null || Find.Selector.SingleSelectedThing != Pawn)
            {
                yield break;
            }

            List<CompSmallTrailerUnit> trailers = TowedTrailers(Pawn).ToList();
            for (int i = 0; i < trailers.Count; i++)
            {
                CompSmallTrailerUnit unit = trailers[i];
                string suffix = trailers.Count > 1 ? " " + (i + 1).ToString() : string.Empty;

                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandEmergencyDetachPawn".Translate() + suffix,
                    defaultDesc = "STF_CommandEmergencyDetachPawnDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/UnloadTransporter", false),
                    action = () => Detach(unit)
                };

                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandDeployPawn".Translate() + suffix,
                    defaultDesc = "STF_CommandDeployPawnDesc".Translate(),
                    icon = TexCommand.Install,
                    action = () => SmallTrailerDeployUtility.BeginDeployTargeting(unit)
                };

                if (unit.State.InnerContainer.Count > 0)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "STF_CommandUnloadContents".Translate() + suffix,
                        defaultDesc = "STF_CommandUnloadContentsDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Drop", false),
                        action = () => ExampleSmallTrailerHandler.UnloadContents(unit)
                    };
                }

                yield return new Command_Action
                {
                    defaultLabel = "STF_CommandCaravanRestore".Translate() + suffix,
                    defaultDesc = "STF_CommandCaravanRestoreDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/FormCaravan", false),
                    action = () => RestoreInventory(unit)
                };
            }
        }

        public static IEnumerable<CompSmallTrailerUnit> TowedTrailers(Pawn pawn)
        {
            if (pawn?.inventory == null)
            {
                yield break;
            }

            for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
            {
                CompSmallTrailerUnit comp = pawn.inventory.innerContainer[i].TryGetComp<CompSmallTrailerUnit>();
                if (comp?.State.mode == SmallTrailerMode.Towing)
                {
                    yield return comp;
                }
            }
        }

        private static void Detach(CompSmallTrailerUnit unit)
        {
            SmallTrailerUnitDefExtension ext = unit.Extension;
            if (ext == null || !SmallTrailerRegistry.TryGetHandler(ext.handlerKey, out ISmallTrailerModeHandler handler))
            {
                Messages.Message("STF_MessageNoHandler".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn holder = SmallTrailerUtility.FindHoldingPawn(unit.parent);
            if (holder?.Map == null)
            {
                Messages.Message("STF_MessageNoHoldingPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            SmallTrailerUtility.Report(handler.TryLeaveMode(unit, unit.State.mode, holder.Map, holder.Position));
        }

        private static void RestoreInventory(CompSmallTrailerUnit unit)
        {
            SmallTrailerUnitDefExtension ext = unit.Extension;
            if (ext == null || !SmallTrailerRegistry.TryGetInventoryBridge(ext.inventoryBridgeKey, out ISmallTrailerInventoryBridge bridge))
            {
                Messages.Message("STF_MessageNoBridge".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn holder = SmallTrailerUtility.FindHoldingPawn(unit.parent);
            IEnumerable<Pawn> pawns = holder?.Map?.mapPawns?.FreeColonistsSpawned ?? Enumerable.Empty<Pawn>();
            SmallTrailerUtility.Report(bridge.TryRestoreStateFromCaravan(unit.State, pawns));
        }
    }
}
