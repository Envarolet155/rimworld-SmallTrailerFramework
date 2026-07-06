using RimWorld;
using Verse;

namespace SmallTrailerFramework
{
    public class StatPart_SmallTrailerCarryingCapacity : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            float capacity = TowedTrailerCapacity(req.Thing as Pawn);
            if (capacity > 0f)
            {
                val += capacity;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            float capacity = TowedTrailerCapacity(req.Thing as Pawn);
            if (capacity <= 0f)
            {
                return null;
            }
            return "STF_StatsTowedTrailerCapacity".Translate(capacity.ToStringMassOffset());
        }

        private static float TowedTrailerCapacity(Pawn pawn)
        {
            if (pawn?.inventory == null)
            {
                return 0f;
            }

            float capacity = 0f;
            for (int i = 0; i < pawn.inventory.innerContainer.Count; i++)
            {
                CompSmallTrailerUnit comp = pawn.inventory.innerContainer[i].TryGetComp<CompSmallTrailerUnit>();
                if (comp?.State.mode == SmallTrailerMode.Towing && comp.Extension != null)
                {
                    capacity += comp.Extension.massCapacity;
                }
            }
            return capacity;
        }
    }
}
