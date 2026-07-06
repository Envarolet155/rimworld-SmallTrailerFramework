using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SmallTrailerFramework.ExamplePlugin
{
    public static class SmallTrailerDeployUtility
    {
        private static Rot4 placingRot = Rot4.South;

        public static JobDef DeployJobDef => DefDatabase<JobDef>.GetNamed("STF_DeploySmallTrailer", false);

        public static void BeginDeployTargeting(CompSmallTrailerUnit unit)
        {
            Pawn pawn = SmallTrailerUtility.FindHoldingPawn(unit.parent);
            ThingDef buildingDef = unit.Extension?.buildingThingDef;
            JobDef jobDef = DeployJobDef;
            if (pawn?.Map == null || buildingDef == null || jobDef == null)
            {
                Messages.Message("STF_MessageNoMap".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            TargetingParameters parms = TargetingParameters.ForCell();
            Find.Targeter.BeginTargeting(
                targetParams: parms,
                action: target => StartDeployJob(pawn, unit, target.Cell, placingRot),
                highlightAction: target => DrawDeployGhost(buildingDef, pawn.Map, target.Cell, placingRot),
                targetValidator: target => CanDeployAt(buildingDef, pawn.Map, target.Cell, placingRot).Accepted,
                mouseAttachment: TexCommand.Install,
                onGuiAction: target =>
                {
                    UpdateRotation();
                    DrawDeployMouseAttachments(buildingDef, pawn.Map, target.Cell, placingRot);
                },
                onUpdateAction: target => UpdateRotation());
        }

        private static void StartDeployJob(Pawn pawn, CompSmallTrailerUnit unit, IntVec3 cell, Rot4 rot)
        {
            AcceptanceReport report = CanDeployAt(unit.Extension.buildingThingDef, pawn.Map, cell, rot);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!pawn.CanReach(cell, PathEndMode.Touch, Danger.Deadly))
            {
                Messages.Message("STF_NoReachablePawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Job job = JobMaker.MakeJob(DeployJobDef, cell, unit.parent);
            job.count = rot.AsInt;
            job.playerForced = true;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private static AcceptanceReport CanDeployAt(ThingDef buildingDef, Map map, IntVec3 cell, Rot4 rot)
        {
            if (!cell.InBounds(map))
            {
                return false;
            }
            return GenConstruct.CanPlaceBlueprintAt(buildingDef, cell, rot, map);
        }

        private static void DrawDeployGhost(ThingDef buildingDef, Map map, IntVec3 cell, Rot4 rot)
        {
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return;
            }
            Color color = CanDeployAt(buildingDef, map, cell, rot).Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
            GhostDrawer.DrawGhostThing(cell, rot, buildingDef, buildingDef.graphic, color, AltitudeLayer.Blueprint, null, drawPlaceWorkers: true);
        }

        private static void DrawDeployMouseAttachments(ThingDef buildingDef, Map map, IntVec3 cell, Rot4 rot)
        {
            AcceptanceReport report = CanDeployAt(buildingDef, map, cell, rot);
            string rotateText = "STF_DeployRotateHint".Translate(
                KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel,
                KeyBindingDefOf.Designator_RotateRight.MainKeyLabel,
                rot.ToStringHuman());
            if (!report.Accepted && !report.Reason.NullOrEmpty())
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot, report.Reason + "\n" + rotateText);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.Install, rotateText);
            }
        }

        private static void UpdateRotation()
        {
            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                placingRot.Rotate(RotationDirection.Clockwise);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
            {
                placingRot.Rotate(RotationDirection.Counterclockwise);
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }
    }
}
