using Verse;

namespace SmallTrailerFramework
{
    public class SmallTrailerUnitDefExtension : DefModExtension
    {
        public ThingDef buildingThingDef;
        public ThingDef packedThingDef;
        public string handlerKey = "SmallTrailerFramework.Example";
        public string inventoryBridgeKey = "SmallTrailerFramework.Example";
        public string speedWorkerKey = "SmallTrailerFramework.Default";
        public int minPawns = 1;
        public int maxPawns = 1;
        public float restoreRadius = 3f;
        public float massCapacity = 75f;
        public bool canEnterCaravan = true;
        public string drawTexPath;
    }
}
