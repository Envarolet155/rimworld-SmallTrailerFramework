using System.Collections.Generic;
using RimWorld;
using Verse;

namespace SmallTrailerFramework
{
    public struct SmallTrailerCaravanUnit
    {
        public CompSmallTrailerUnit unit;
        public Pawn holder;
    }

    public static class SmallTrailerCaravanUtility
    {
        private static readonly List<SmallTrailerCaravanUnit> tmpUnits = new List<SmallTrailerCaravanUnit>();

        public static List<SmallTrailerCaravanUnit> TowedOrCaravanUnits(List<Pawn> pawns)
        {
            tmpUnits.Clear();
            if (pawns == null)
            {
                return tmpUnits;
            }

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn?.inventory == null)
                {
                    continue;
                }

                for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                {
                    CompSmallTrailerUnit comp = pawn.inventory.innerContainer[j].TryGetComp<CompSmallTrailerUnit>();
                    if (comp == null || comp.Extension == null)
                    {
                        continue;
                    }
                    if (comp.State.mode == SmallTrailerMode.Towing || comp.State.mode == SmallTrailerMode.Caravan)
                    {
                        tmpUnits.Add(new SmallTrailerCaravanUnit { unit = comp, holder = pawn });
                    }
                }
            }
            return tmpUnits;
        }

        public static float ExtraMassCapacity(List<Pawn> pawns, System.Text.StringBuilder explanation = null)
        {
            float capacity = 0f;
            List<SmallTrailerCaravanUnit> units = TowedOrCaravanUnits(pawns);
            for (int i = 0; i < units.Count; i++)
            {
                float unitCapacity = units[i].unit.Extension.massCapacity;
                if (unitCapacity <= 0f)
                {
                    continue;
                }
                capacity += unitCapacity;
                if (explanation != null)
                {
                    if (explanation.Length > 0)
                    {
                        explanation.AppendLine();
                    }
                    explanation.Append("  - " + "STF_CaravanMassCapacitySource".Translate(units[i].holder.LabelShortCap) + ": " + unitCapacity.ToStringMassOffset());
                }
            }
            return capacity;
        }

        public static float MainPawnMaxSpeedTicks(Pawn pawn)
        {
            if (pawn == null)
            {
                return 1f;
            }
            float moveSpeed = pawn.GetStatValue(StatDefOf.MoveSpeed);
            if (moveSpeed <= 0f)
            {
                return float.MaxValue;
            }
            float ticksPerCell = 60f / moveSpeed;
            return ticksPerCell * 340f / 2f;
        }
    }
}
