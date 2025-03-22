namespace singleton.Singletons;

/// <summary>
/// Потокобезопасный синглтон.
/// </summary>
/// <remarks>
/// https://csharpindepth.com/articles/singleton#lock
/// </remarks>
public sealed class ThreadsafeSingleton
{
    private static ThreadsafeSingleton? _instance;
    private static readonly Lock Padlock = new();

    private ThreadsafeSingleton() { }

    public static ThreadsafeSingleton Instance
    {
        get
        {
            lock (Padlock)
            {
                return _instance ??= new ThreadsafeSingleton();
            }
        }
    }
}