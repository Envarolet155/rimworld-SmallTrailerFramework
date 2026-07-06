using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SmallTrailerFramework
{
    [StaticConstructorOnStartup]
    public static class SmallTrailerHarmonyPatches
    {
        static SmallTrailerHarmonyPatches()
        {
            Harmony harmony = new Harmony("Envarolet155.SmallTrailerFramework");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.Capacity), typeof(List<ThingCount>), typeof(StringBuilder))]
    public static class Patch_CollectionsMassCalculator_Capacity
    {
        public static void Postfix(List<ThingCount> thingCounts, StringBuilder explanation, ref float __result)
        {
            if (thingCounts == null)
            {
                return;
            }

            List<Pawn> pawns = new List<Pawn>();
            for (int i = 0; i < thingCounts.Count; i++)
            {
                if (thingCounts[i].Count > 0 && thingCounts[i].Thing is Pawn pawn)
                {
                    pawns.Add(pawn);
                }
            }
            __result += SmallTrailerCaravanUtility.ExtraMassCapacity(pawns, explanation);
        }
    }

    [HarmonyPatch(typeof(CaravanTicksPerMoveUtility), nameof(CaravanTicksPerMoveUtility.GetTicksPerMove), typeof(CaravanTicksPerMoveUtility.CaravanInfo), typeof(StringBuilder))]
    public static class Patch_CaravanTicksPerMoveUtility_GetTicksPerMove
    {
        public static void Postfix(CaravanTicksPerMoveUtility.CaravanInfo caravanInfo, StringBuilder explanation, ref int __result)
        {
            List<SmallTrailerCaravanUnit> units = SmallTrailerCaravanUtility.TowedOrCaravanUnits(caravanInfo.pawns);
            if (units.Count == 0 || __result <= 1)
            {
                return;
            }

            List<float> factors = new List<float>();
            float minAllowedTicks = float.MaxValue;
            for (int i = 0; i < units.Count; i++)
            {
                CompSmallTrailerUnit unit = units[i].unit;
                Pawn holder = units[i].holder;
                ISmallTrailerSpeedWorker worker = SmallTrailerRegistry.GetSpeedWorker(unit.Extension.speedWorkerKey);
                IReadOnlyList<Pawn> pawns = new List<Pawn> { holder };

                float factor = 1f;
                if (worker is ISmallTrailerSpeedFactorWorker factorWorker)
                {
                    factor = factorWorker.GetCaravanSpeedFactor(unit, pawns);
                }
                else
                {
                    int workerTicks = worker.GetCaravanTicksPerMove(unit, pawns, __result);
                    if (workerTicks > 0)
                    {
                        factor = (float)__result / workerTicks;
                    }
                }

                if (factor > 1f)
                {
                    factors.Add(factor);
                }
                minAllowedTicks = Mathf.Min(minAllowedTicks, SmallTrailerCaravanUtility.MainPawnMaxSpeedTicks(holder));
            }

            if (factors.Count == 0)
            {
                return;
            }

            factors.Sort();
            factors.Reverse();
            float bonus = factors[0] - 1f;
            for (int i = 1; i < factors.Count; i++)
            {
                bonus += (factors[i] - 1f) * 0.25f;
            }

            float combinedFactor = Mathf.Clamp(1f + bonus, 1f, 1.35f);
            int adjustedTicks = Mathf.CeilToInt(__result / combinedFactor);
            if (minAllowedTicks < float.MaxValue)
            {
                adjustedTicks = Mathf.Max(adjustedTicks, Mathf.CeilToInt(minAllowedTicks));
            }

            if (adjustedTicks < __result)
            {
                if (explanation != null)
                {
                    explanation.AppendLine();
                    explanation.Append("  " + "STF_CaravanSpeedFactor".Translate(combinedFactor.ToStringPercent()));
                }
                __result = adjustedTicks;
            }
        }
    }
}
