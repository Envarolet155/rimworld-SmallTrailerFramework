using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class Dialog_SmallTrailerLoadItems : Window
    {
        private readonly CompSmallTrailerUnit unit;
        private readonly Map map;
        private readonly List<TransferableOneWay> transferables = new List<TransferableOneWay>();
        private readonly TransferableOneWayWidget itemsTransfer;

        public Dialog_SmallTrailerLoadItems(CompSmallTrailerUnit unit)
        {
            this.unit = unit;
            map = unit.parent.Map;
            doCloseX = true;
            absorbInputAroundWindow = true;

            AddReachableColonyItems();
            itemsTransfer = new TransferableOneWayWidget(
                transferables,
                null,
                null,
                "FormCaravanColonyThingCountTip".Translate(),
                drawMass: true,
                IgnorePawnsInventoryMode.DontIgnore,
                includePawnsMassInMassUsage: false,
                () => MassCapacity - MassUsage,
                0f,
                ignoreSpawnedCorpseGearAndInventoryMass: false,
                map.Tile,
                drawMarketValue: true,
                drawEquippedWeapon: false,
                drawNutritionEatenPerDay: false,
                drawMechEnergy: false,
                drawItemNutrition: true,
                drawForagedFoodPerDay: false,
                drawDaysUntilRot: true);
        }

        public override Vector2 InitialSize => new Vector2(1024f, UI.screenHeight - 120f);

        private float MassCapacity => unit.Extension.massCapacity - ExampleSmallTrailerUtility.ContentsMass(unit.State);

        private float MassUsage => CollectionsMassCalculator.MassUsageTransferables(transferables, IgnorePawnsInventoryMode.DontIgnore, includePawnsMass: false, ignoreSpawnedCorpsesGearAndInventory: false);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), "STF_LoadDialogTitle".Translate());
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(0f, 36f, inRect.width, 24f), "STF_LoadDialogMass".Translate(MassUsage.ToStringMass(), MassCapacity.ToStringMass()));
            Rect listRect = new Rect(0f, 68f, inRect.width, inRect.height - 118f);
            itemsTransfer.OnGUI(listRect);

            Rect cancelRect = new Rect(inRect.width - 220f, inRect.height - 38f, 100f, 38f);
            Rect acceptRect = new Rect(inRect.width - 110f, inRect.height - 38f, 110f, 38f);
            if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(acceptRect, "AcceptButton".Translate()))
            {
                if (MassUsage > MassCapacity)
                {
                    Messages.Message("STF_FailMassCapacity".Translate(), MessageTypeDefOf.RejectInput, false);
                    return;
                }
                map.GetComponent<ExampleSmallTrailerMapComponent>()?.SetLoadPlan(unit, SelectedThings());
                Messages.Message("STF_MessageLoadPlanCreated".Translate(), MessageTypeDefOf.PositiveEvent, false);
                Close();
            }
        }

        private void AddReachableColonyItems()
        {
            List<Thing> items = CaravanFormingUtility.AllReachableColonyItems(map, allowEvenIfOutsideHomeArea: false, allowEvenIfReserved: false, canMinify: false);
            for (int i = 0; i < items.Count; i++)
            {
                AddToTransferables(items[i]);
            }
        }

        private void AddToTransferables(Thing thing)
        {
            TransferableOneWay transferable = TransferableUtility.TransferableMatching(thing, transferables, TransferAsOneMode.PodsOrCaravanPacking);
            if (transferable == null)
            {
                transferable = new TransferableOneWay();
                transferables.Add(transferable);
            }
            if (!transferable.things.Contains(thing))
            {
                transferable.things.Add(thing);
            }
        }

        private Dictionary<Thing, int> SelectedThings()
        {
            Dictionary<Thing, int> selected = new Dictionary<Thing, int>();
            for (int i = 0; i < transferables.Count; i++)
            {
                TransferableOneWay transferable = transferables[i];
                int remaining = transferable.CountToTransfer;
                if (remaining <= 0)
                {
                    continue;
                }

                foreach (Thing thing in transferable.things.OrderBy(t => t.Position.DistanceToSquared(unit.parent.Position)))
                {
                    if (remaining <= 0)
                    {
                        break;
                    }
                    int count = Mathf.Min(remaining, thing.stackCount);
                    if (count > 0)
                    {
                        selected[thing] = count;
                        remaining -= count;
                    }
                }
            }
            return selected;
        }
    }
}
