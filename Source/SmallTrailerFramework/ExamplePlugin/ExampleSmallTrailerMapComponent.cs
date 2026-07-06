using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ExampleSmallTrailerMapComponent : MapComponent
    {
        private readonly List<Thing> tmpTrailers = new List<Thing>();

        public ExampleSmallTrailerMapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentDraw()
        {
            tmpTrailers.Clear();
            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.inventory == null)
                {
                    continue;
                }
                for (int j = 0; j < pawn.inventory.innerContainer.Count; j++)
                {
                    Thing thing = pawn.inventory.innerContainer[j];
                    CompSmallTrailerUnit comp = thing.TryGetComp<CompSmallTrailerUnit>();
                    if (comp?.State.mode == SmallTrailerMode.Towing)
                    {
                        DrawTrailerForPawn(thing, pawn);
                    }
                }
            }
            tmpTrailers.Clear();
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.FreeColonistsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                if (pawn.Drafted || pawn.inventory == null || !HasTowedTrailer(pawn))
                {
                    EnsureTowingControlHediff(pawn, removeIfMissing: true);
                    continue;
                }
                EnsureTowingControlHediff(pawn, removeIfMissing: false);
                if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Wait)
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Wait);
                    job.expiryInterval = 300;
                    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }
        }

        private static bool HasTowedTrailer(Pawn pawn)
        {
            return HediffComp_SmallTrailerTowingControl.TowedTrailers(pawn).Any();
        }

        public static void EnsureTowingControlHediff(Pawn pawn, bool removeIfMissing)
        {
            HediffDef def = DefDatabase<HediffDef>.GetNamedSilentFail("STF_TowingControl");
            if (def == null || pawn.health == null)
            {
                return;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(def);
            bool hasTrailer = HasTowedTrailer(pawn);
            if (hasTrailer && existing == null)
            {
                pawn.health.AddHediff(def);
            }
            else if (!hasTrailer && removeIfMissing && existing != null)
            {
                pawn.health.RemoveHediff(existing);
            }
        }

        private static void DrawTrailerForPawn(Thing trailer, Pawn pawn)
        {
            Rot4 rot = pawn.Rotation;
            Vector3 offset = rot.FacingCell.ToVector3() * -0.72f;
            Vector3 loc = pawn.DrawPos + offset;
            loc.y = AltitudeLayer.Item.AltitudeFor();
            trailer.Graphic.Draw(loc, rot, trailer);
        }
    }
}
