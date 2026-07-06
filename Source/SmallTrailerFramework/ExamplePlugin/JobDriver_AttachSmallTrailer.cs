using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class JobDriver_AttachSmallTrailer : JobDriver
    {
        private Thing Trailer => job.GetTarget(TargetIndex.A).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Trailer, job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(delegate
            {
                CompSmallTrailerUnit unit = Trailer.TryGetComp<CompSmallTrailerUnit>();
                if (unit?.Extension == null || !SmallTrailerRegistry.TryGetHandler(unit.Extension.handlerKey, out ISmallTrailerModeHandler handler))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                SmallTrailerResult result = handler.TryEnterMode(unit, SmallTrailerMode.Towing, new List<Pawn> { pawn });
                if (!result.Accepted)
                {
                    Messages.Message(result.Reason, MessageTypeDefOf.RejectInput, false);
                    EndJobWith(JobCondition.Incompletable);
                }
            });
        }
    }
}
