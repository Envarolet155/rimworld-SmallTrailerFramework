using Verse;

namespace SmallTrailerFramework.ExamplePlugin
{
    [StaticConstructorOnStartup]
    public static class ExampleSmallTrailerPlugin
    {
        static ExampleSmallTrailerPlugin()
        {
            ExampleSmallTrailerHandler handler = new ExampleSmallTrailerHandler();
            SmallTrailerRegistry.RegisterHandler(handler);
            SmallTrailerRegistry.RegisterInventoryBridge(handler);
            SmallTrailerRegistry.RegisterSpeedWorker(new ExampleSmallTrailerSpeedWorker());
            Log.Message("[SmallTrailerFramework] Example small trailer plugin registered.");
        }
    }
}
