using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SmallTrailerFramework
{
    public enum SmallTrailerMode
    {
        Building,
        Towing,
        Caravan
    }

    public interface ISmallTrailerModeHandler
    {
        string Key { get; }
        SmallTrailerResult CanEnterMode(CompSmallTrailerUnit unit, SmallTrailerMode targetMode, IReadOnlyList<Pawn> pawns);
        SmallTrailerResult TryEnterMode(CompSmallTrailerUnit unit, SmallTrailerMode targetMode, IReadOnlyList<Pawn> pawns);
        SmallTrailerResult TryLeaveMode(CompSmallTrailerUnit unit, SmallTrailerMode currentMode, Map targetMap, IntVec3 targetCell);
    }

    public interface ISmallTrailerStateStore
    {
        void Register(SmallTrailerState state);
        bool TryGet(string stableId, out SmallTrailerState state);
        void Forget(string stableId);
    }

    public interface ISmallTrailerInventoryBridge
    {
        string Key { get; }
        SmallTrailerResult TryMoveStateToCaravan(SmallTrailerState state, Caravan caravan);
        SmallTrailerResult TryRestoreStateFromCaravan(SmallTrailerState state, IEnumerable<Pawn> pawns);
    }

    public interface ISmallTrailerSpeedWorker
    {
        string Key { get; }
        float GetTowingSpeedFactor(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns);
        int GetCaravanTicksPerMove(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns, int vanillaTicksPerMove);
    }

    public interface ISmallTrailerGizmoProvider
    {
        IEnumerable<Gizmo> GetGizmos(CompSmallTrailerUnit unit);
    }

    public interface ISmallTrailerAttachGizmoProvider
    {
        IEnumerable<Gizmo> GetAttachGizmos(CompSmallTrailerUnit unit);
    }

    public sealed class SmallTrailerDefaultSpeedWorker : ISmallTrailerSpeedWorker
    {
        public string Key => "SmallTrailerFramework.Default";

        public float GetTowingSpeedFactor(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns)
        {
            return 1f;
        }

        public int GetCaravanTicksPerMove(CompSmallTrailerUnit unit, IReadOnlyList<Pawn> pawns, int vanillaTicksPerMove)
        {
            return vanillaTicksPerMove;
        }
    }
}
