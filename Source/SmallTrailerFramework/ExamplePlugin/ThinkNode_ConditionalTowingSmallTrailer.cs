using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ThinkNode_ConditionalTowingSmallTrailer : ThinkNode_Conditional
    {
        protected override bool Satisfied(Pawn pawn)
        {
            if (pawn?.inventory == null || pawn.Drafted || pawn.IsFormingCaravan() || pawn.GetLord() != null)
            {
                return false;
            }

            return HediffComp_SmallTrailerTowingControl.TowedTrailers(pawn).Any();
        }
    }
}
