using RimWorld;
using Verse;

namespace SmallTrailerFramework
{
    public class StatPart_SmallTrailerMass : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            float contentsMass = ContentsMass(req.Thing);
            if (contentsMass > 0f)
            {
                val += contentsMass;
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            float contentsMass = ContentsMass(req.Thing);
            if (contentsMass <= 0f)
            {
                return null;
            }
            return "STF_StatsContentsMass".Translate(contentsMass.ToStringMassOffset());
        }

        private static float ContentsMass(Thing thing)
        {
            CompSmallTrailerUnit comp = thing?.TryGetComp<CompSmallTrailerUnit>();
            if (comp == null)
            {
                return 0f;
            }

            float mass = 0f;
            foreach (Thing item in comp.State.InnerContainer)
            {
                mass += item.GetStatValue(StatDefOf.Mass) * item.stackCount;
            }
            return mass;
        }
    }
}
