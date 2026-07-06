using System.Collections.Generic;
using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class JobDriver_LoadSmallTrailer : JobDriver
    {
        private Thing Item => job.GetTarget(TargetIndex.A).Thing;
        private Thing Trailer => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, job.count, null, errorOnFailed)
                && pawn.Reserve(Trailer, job, 1, 1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
            yield return Toils_General.Do(DepositCarriedThing);
        }

        private void DepositCarriedThing()
        {
            Thing carried = pawn.carryTracker.CarriedThing;
            CompSmallTrailerUnit unit = Trailer.TryGetComp<CompSmallTrailerUnit>();
            if (carried == null || unit == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int maxCount = ExampleSmallTrailerUtility.MaxLoadableCount(unit, carried);
            int count = Math.Min(Math.Max(job.count, 1), carried.stackCount);
            count = Math.Min(Math.Max(count, 0), maxCount);
            if (count <= 0)
            {
                pawn.carryTracker.TryDropCarriedThing(Trailer.Position, ThingPlaceMode.Near, out Thing _);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            int moved = pawn.carryTracker.innerContainer.TryTransferToContainer(carried, unit.State.InnerContainer, count);
            if (moved > 0)
            {
                unit.State.SnapshotManifest();
                SmallTrailerGameComponent.Current?.Register(unit.State);
                Map.GetComponent<ExampleSmallTrailerMapComponent>()?.NotifyLoaded(unit.State.stableId, job.GetTarget(TargetIndex.A).Thing, moved);
            }

            if (pawn.carryTracker.CarriedThing != null)
            {
                pawn.carryTracker.TryDropCarriedThing(Trailer.Position, ThingPlaceMode.Near, out Thing _);
            }
        }
    }
}
