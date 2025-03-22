namespace singleton.Singletons;

// TODO not threadsafe?
public sealed class LazySingleton
{
    private LazySingleton() { }

    public static LazySingleton Instance => Nested.instance;

    private class Nested
    {
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static Nested() { }

        internal static readonly LazySingleton instance = new();
    }
}
