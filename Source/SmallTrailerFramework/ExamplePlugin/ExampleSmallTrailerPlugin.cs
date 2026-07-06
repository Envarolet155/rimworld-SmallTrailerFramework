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
            Log.Message("[SmallTrailerFramework] Example small trailer plugin registered.");
        }
    }
}
