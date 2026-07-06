using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ExampleSmallTrailerSpeedWorker : ISmallTrailerSpeedFactorWorker
    {
        public string Key => "SmallTrailerFramework.Example";

        public float GetTowingSpeedFactor(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns)
        {
            return GetCaravanSpeedFactor(unit, pawns);
        }

        public int GetCaravanTicksPerMove(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns, int vanillaTicksPerMove)
        {
            float factor = GetCaravanSpeedFactor(unit, pawns);
            int adjustedTicks = Mathf.CeilToInt(vanillaTicksPerMove / factor);
            Pawn mainPawn = pawns != null && pawns.Count > 0 ? pawns[0] : null;
            if (mainPawn != null)
            {
                adjustedTicks = Mathf.Max(adjustedTicks, Mathf.CeilToInt(SmallTrailerCaravanUtility.MainPawnMaxSpeedTicks(mainPawn)));
            }
            return Mathf.Max(adjustedTicks, 1);
        }

        public float GetCaravanSpeedFactor(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns)
        {
            Pawn mainPawn = pawns != null && pawns.Count > 0 ? pawns[0] : null;
            if (unit?.Extension == null || mainPawn == null)
            {
                return 1f;
            }

            float contentsMass = ExampleSmallTrailerUtility.ContentsMass(unit.State);
            float loadRatio = unit.Extension.massCapacity <= 0f ? 1f : Mathf.Clamp01(contentsMass / unit.Extension.massCapacity);
            float baseReliefFactor = Mathf.Lerp(1.28f, 1.12f, loadRatio);

            float bodyFactor = Mathf.Lerp(0.85f, 1.15f, Mathf.InverseLerp(0.8f, 2.5f, mainPawn.BodySize));
            float movingFactor = Mathf.Clamp(mainPawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Moving) ?? 1f, 0.5f, 1.2f);
            float manipulationFactor = Mathf.Clamp(mainPawn.health?.capacities?.GetLevel(PawnCapacityDefOf.Manipulation) ?? 1f, 0.5f, 1.1f);
            float meleeLevel = mainPawn.skills?.GetSkill(SkillDefOf.Melee)?.Level ?? 0f;
            float meleeFactor = 1f + Mathf.Clamp(meleeLevel / 100f, 0f, 0.18f);

            float pawnSuitability = Mathf.Clamp((bodyFactor * 0.45f) + (movingFactor * 0.3f) + (manipulationFactor * 0.15f) + (meleeFactor * 0.1f), 0.75f, 1.2f);
            return Mathf.Clamp(baseReliefFactor * pawnSuitability, 1.02f, 1.3f);
        }
    }
}
