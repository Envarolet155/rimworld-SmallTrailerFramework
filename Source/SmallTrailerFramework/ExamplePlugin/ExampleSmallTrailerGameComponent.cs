using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    public class ExampleSmallTrailerGameComponent : GameComponent
    {
        public ExampleSmallTrailerGameComponent(Game game)
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame % 250 != 0 || Find.WorldObjects == null)
            {
                return;
            }

            List<Caravan> caravans = Find.WorldObjects.Caravans;
            for (int i = 0; i < caravans.Count; i++)
            {
                SyncCaravan(caravans[i]);
            }
        }

        private static void SyncCaravan(Caravan caravan)
        {
            foreach (CompSmallTrailerUnit unit in ExampleSmallTrailerUtility.TrailerUnitsInCaravan(caravan))
            {
                if (unit.State.mode != SmallTrailerMode.Towing || unit.State.InnerContainer.Count == 0)
                {
                    continue;
                }
                ISmallTrailerInventoryBridge bridge = ExampleSmallTrailerUtility.BridgeFor(unit);
                bridge?.TryMoveStateToCaravan(unit.State, caravan);
            }
        }
    }
}
