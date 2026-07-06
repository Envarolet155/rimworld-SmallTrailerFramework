using System.Collections.Generic;
using Verse;

namespace SmallTrailerFramework
{
    public static class SmallTrailerRegistry
    {
        private static readonly Dictionary<string, ISmallTrailerModeHandler> handlers = new Dictionary<string, ISmallTrailerModeHandler>();
        private static readonly Dictionary<string, ISmallTrailerInventoryBridge> bridges = new Dictionary<string, ISmallTrailerInventoryBridge>();
        private static readonly Dictionary<string, ISmallTrailerSpeedWorker> speedWorkers = new Dictionary<string, ISmallTrailerSpeedWorker>();

        static SmallTrailerRegistry()
        {
            RegisterSpeedWorker(new SmallTrailerDefaultSpeedWorker());
        }

        public static void RegisterHandler(ISmallTrailerModeHandler handler)
        {
            if (handler == null || handler.Key.NullOrEmpty())
            {
                Log.Warning("[SmallTrailerFramework] Ignored invalid trailer handler registration.");
                return;
            }
            handlers[handler.Key] = handler;
        }

        public static void RegisterInventoryBridge(ISmallTrailerInventoryBridge bridge)
        {
            if (bridge == null || bridge.Key.NullOrEmpty())
            {
                Log.Warning("[SmallTrailerFramework] Ignored invalid inventory bridge registration.");
                return;
            }
            bridges[bridge.Key] = bridge;
        }

        public static void RegisterSpeedWorker(ISmallTrailerSpeedWorker worker)
        {
            if (worker == null || worker.Key.NullOrEmpty())
            {
                Log.Warning("[SmallTrailerFramework] Ignored invalid speed worker registration.");
                return;
            }
            speedWorkers[worker.Key] = worker;
        }

        public static bool TryGetHandler(string key, out ISmallTrailerModeHandler handler)
        {
            return handlers.TryGetValue(key, out handler);
        }

        public static bool TryGetInventoryBridge(string key, out ISmallTrailerInventoryBridge bridge)
        {
            return bridges.TryGetValue(key, out bridge);
        }

        public static ISmallTrailerSpeedWorker GetSpeedWorker(string key)
        {
            if (!key.NullOrEmpty() && speedWorkers.TryGetValue(key, out ISmallTrailerSpeedWorker worker))
            {
                return worker;
            }
            return speedWorkers["SmallTrailerFramework.Default"];
        }
    }
}
