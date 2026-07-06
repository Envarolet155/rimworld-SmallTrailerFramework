using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class Dialog_SmallTrailerLoadItems : Window
    {
        private readonly CompSmallTrailerUnit unit;
        private readonly Map map;
        private readonly List<Thing> items;
        private readonly Dictionary<Thing, int> counts = new Dictionary<Thing, int>();
        private Vector2 scrollPosition;

        public Dialog_SmallTrailerLoadItems(CompSmallTrailerUnit unit)
        {
            this.unit = unit;
            map = unit.parent.Map;
            doCloseX = true;
            absorbInputAroundWindow = true;
            items = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
                .Where(t => t.Spawned && t.def.category == ThingCategory.Item && !t.IsForbidden(Faction.OfPlayer))
                .OrderBy(t => t.LabelCap.ToString())
                .ThenBy(t => t.Position.DistanceToSquared(unit.parent.Position))
                .ToList();
        }

        public override Vector2 InitialSize => new Vector2(760f, 620f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 32f), "STF_LoadDialogTitle".Translate());
            Text.Font = GameFont.Small;

            float usedMass = ExampleSmallTrailerUtility.ContentsMass(unit.State) + SelectedMass();
            Widgets.Label(new Rect(0f, 36f, inRect.width, 24f), "STF_LoadDialogMass".Translate(usedMass.ToStringMass(), unit.Extension.massCapacity.ToStringMass()));

            Rect outRect = new Rect(0f, 68f, inRect.width, inRect.height - 118f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, items.Count * 34f);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float y = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                DrawRow(new Rect(0f, y, viewRect.width, 32f), items[i]);
                y += 34f;
            }
            Widgets.EndScrollView();

            Rect cancelRect = new Rect(inRect.width - 220f, inRect.height - 38f, 100f, 38f);
            Rect acceptRect = new Rect(inRect.width - 110f, inRect.height - 38f, 110f, 38f);
            if (Widgets.ButtonText(cancelRect, "CancelButton".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(acceptRect, "AcceptButton".Translate()))
            {
                map.GetComponent<ExampleSmallTrailerMapComponent>()?.SetLoadPlan(unit, counts.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value));
                Messages.Message("STF_MessageLoadPlanCreated".Translate(), MessageTypeDefOf.PositiveEvent, false);
                Close();
            }
        }

        private void DrawRow(Rect rect, Thing thing)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            int count = counts.TryGetValue(thing, out int value) ? value : 0;
            float unitMass = ExampleSmallTrailerUtility.UnitMass(thing);
            Widgets.Label(new Rect(rect.x, rect.y + 6f, rect.width - 210f, 24f), thing.LabelCap);
            Widgets.Label(new Rect(rect.x + rect.width - 210f, rect.y + 6f, 70f, 24f), "x" + thing.stackCount);
            Widgets.Label(new Rect(rect.x + rect.width - 150f, rect.y + 6f, 64f, 24f), (unitMass * count).ToStringMass());

            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 82f, rect.y + 2f, 28f, 28f), "-"))
            {
                counts[thing] = Mathf.Max(0, count - 1);
            }
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + rect.width - 52f, rect.y + 2f, 24f, 28f), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(new Rect(rect.x + rect.width - 28f, rect.y + 2f, 28f, 28f), "+"))
            {
                TryAddOne(thing);
            }
        }

        private void TryAddOne(Thing thing)
        {
            int count = counts.TryGetValue(thing, out int value) ? value : 0;
            if (count >= thing.stackCount)
            {
                return;
            }
            float projected = ExampleSmallTrailerUtility.ContentsMass(unit.State) + SelectedMass() + ExampleSmallTrailerUtility.UnitMass(thing);
            if (projected > unit.Extension.massCapacity)
            {
                Messages.Message("STF_FailMassCapacity".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            counts[thing] = count + 1;
        }

        private float SelectedMass()
        {
            float mass = 0f;
            foreach (KeyValuePair<Thing, int> pair in counts)
            {
                mass += ExampleSmallTrailerUtility.UnitMass(pair.Key) * pair.Value;
            }
            return mass;
        }
    }
}
