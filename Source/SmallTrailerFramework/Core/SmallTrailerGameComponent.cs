using System.Collections.Generic;
using System.Linq;
using Verse;

namespace SmallTrailerFramework
{
    public class SmallTrailerGameComponent : GameComponent, ISmallTrailerStateStore
    {
        private List<SmallTrailerState> states = new List<SmallTrailerState>();

        public SmallTrailerGameComponent(Game game)
        {
        }

        public static SmallTrailerGameComponent Current => Verse.Current.Game?.GetComponent<SmallTrailerGameComponent>();

        public void Register(SmallTrailerState state)
        {
            if (state == null)
            {
                return;
            }
            state.EnsureStableId();
            states.RemoveAll(x => x == null || x.stableId == state.stableId);
            states.Add(state);
        }

        public bool TryGet(string stableId, out SmallTrailerState state)
        {
            state = null;
            if (stableId.NullOrEmpty())
            {
                return false;
            }
            state = states.FirstOrDefault(x => x != null && x.stableId == stableId);
            return state != null;
        }

        public void Forget(string stableId)
        {
            if (!stableId.NullOrEmpty())
            {
                states.RemoveAll(x => x == null || x.stableId == stableId);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref states, "smallTrailerStates", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && states == null)
            {
                states = new List<SmallTrailerState>();
            }
        }

        public override void LoadedGame()
        {
            Cleanup();
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            states.RemoveAll(x => x == null || x.stableId.NullOrEmpty());
        }
    }
}
