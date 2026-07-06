using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class WorkGiver_LoadSmallTrailer : WorkGiver
    {
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn?.Map == null || pawn.WorkTagIsDisabled(WorkTags.ManualDumb)
                || pawn.Map.GetComponent<ExampleSmallTrailerMapComponent>()?.HasLoadPlans != true;
        }

        public override Job NonScanJob(Pawn pawn)
        {
            return pawn.Map.GetComponent<ExampleSmallTrailerMapComponent>()?.TryMakeLoadJob(pawn);
        }
    }
}
