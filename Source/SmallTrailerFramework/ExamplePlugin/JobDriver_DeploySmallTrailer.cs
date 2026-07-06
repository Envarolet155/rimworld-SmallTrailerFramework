using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class JobDriver_DeploySmallTrailer : JobDriver
    {
        private IntVec3 DeployCell => job.GetTarget(TargetIndex.A).Cell;
        private Thing TrailerToken => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => TrailerToken == null || TrailerToken.Destroyed || SmallTrailerUtility.FindHoldingPawn(TrailerToken) != pawn);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.Do(Deploy);
        }

        private void Deploy()
        {
            CompSmallTrailerUnit unit = TrailerToken.TryGetComp<CompSmallTrailerUnit>();
            if (unit?.Extension == null || !SmallTrailerRegistry.TryGetHandler(unit.Extension.handlerKey, out ISmallTrailerModeHandler handler))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Rot4 rot = new Rot4(job.count);
            SmallTrailerResult result;
            if (handler is ISmallTrailerRotatedLeaveHandler rotatedHandler)
            {
                result = rotatedHandler.TryLeaveMode(unit, unit.State.mode, pawn.Map, DeployCell, rot);
            }
            else
            {
                result = handler.TryLeaveMode(unit, unit.State.mode, pawn.Map, DeployCell);
            }

            if (!result.Accepted)
            {
                Messages.Message(result.Reason, MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
            }
        }
    }
}
