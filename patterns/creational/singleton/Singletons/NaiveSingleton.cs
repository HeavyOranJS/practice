namespace singleton.Singletons;

/// <summary>
/// Наивная потоконебезопасная реализация синглтона.
/// </summary>
/// <remarks>
/// https://csharpindepth.com/articles/singleton#unsafe
/// </remarks>
public sealed class NaiveSingleton
{
    private static NaiveSingleton? _instance;

    private NaiveSingleton() { }

    public static NaiveSingleton Instance => _instance ??= new NaiveSingleton();
}
