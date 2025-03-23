using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.XPath;
using Microsoft.Extensions.Caching.Hybrid;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromSeconds(10),
    };
})
.AddSerializer<Todo, Serializer<Todo>>();

var app = builder.Build();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id:int}", async (CancellationToken cancellationToken,
    HybridCache cache,
    int id) =>
{
    // Console.WriteLine("request");
    // TODO how do you get name of classes in aot?
    // return await cache.GetOrCreateAsync($"{nameof(Todo)}-{id}",
    var result = await cache.GetOrCreateAsync<Todo?>($"{nameof(Todo)}-{id}",
        async token => await SlowGet(id, token),
        tags: ["todo"], // todo make pretty, do not allocate a list every time -- move to property
        cancellationToken: cancellationToken
        );

    return result is not null ? Results.Ok(result) : Results.NotFound();
});

app.Run();
return;

async Task<Todo?> SlowGet(int id, CancellationToken token = default)
{
    await Task.Delay(1000, token);
    return sampleTodos.FirstOrDefault(todo => todo.Id == id);
}

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(IResult))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

// class CustomSerializerFactory : IHybridCacheSerializerFactory
// {
//     public bool TryCreateSerializer<T>([NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer)
//     {
//         var aotJsonOptions = new JsonSerializerOptions()
//         {
//             TypeInfoResolver = AppJsonSerializerContext.Default
//         };
//         IHybridCacheSerializer<T> some = JsonSerializer.
//     }
// }

// needed bcs of aot and to show when it's called
class Serializer<T> : IHybridCacheSerializer<T> where T : class
{
    // TODO fix memory
    private readonly JsonSerializerOptions _options = new()
    {
        TypeInfoResolver = AppJsonSerializerContext.Default
    };

    // TODO move from generic class
    private static readonly byte[] NullByteArray = Encoding.UTF8.GetBytes("null");

    public T Deserialize(ReadOnlySequence<byte> source)
    {
        if (source.FirstSpan.SequenceEqual(NullByteArray.AsSpan()))
        {
            return null;
        }
        return JsonSerializer.Deserialize<T>(source.FirstSpan, _options) ?? throw new Exception("cannot deserialize todo");
    }

    public void Serialize(T value, IBufferWriter<byte> target)
    {
        var result = JsonSerializer.Serialize(value, _options).AsSpan();
        var span = target.GetSpan(result.Length);
        var written = Encoding.ASCII.GetBytes(result, span);
        target.Advance(written);
    }
}