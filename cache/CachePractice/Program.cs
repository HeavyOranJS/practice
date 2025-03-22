using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        // Flags = HybridCacheEntryFlags.DisableDistributedCacheRead & HybridCacheEntryFlags.DisableDistributedCacheWrite
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
    // TODO how do you get name of classes in aot?
    // return await cache.GetOrCreateAsync($"{nameof(Todo)}-{id}",
    var result = await cache.GetOrCreateAsync<Todo?>($"todo-{id}",
        async token => await SlowGet(id, token),
        tags: ["todo"], // todo make pretty, do not allocate a list every time -- move to property
        cancellationToken: cancellationToken
        );

    return result != null ? Results.Ok(result) : Results.NotFound();
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
class Serializer<Todo> : IHybridCacheSerializer<Todo>
{
    // TODO fix memory
    private readonly JsonSerializerOptions _options = new()
    {
        TypeInfoResolver = AppJsonSerializerContext.Default
    };

    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)")]
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(ReadOnlySpan<Byte>, JsonSerializerOptions)")]
    public Todo Deserialize(ReadOnlySequence<byte> source)
    {
        // var reader = new SequenceReader<byte>(source);
        return JsonSerializer.Deserialize<Todo>(source.FirstSpan, _options) ?? throw new Exception("cannot deserialize todo");
    }

    // TODO what
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public void Serialize(Todo value, IBufferWriter<byte> target)
    {
        var result = JsonSerializer.Serialize(value, _options).AsSpan();
        var span = target.GetSpan(result.Length);
        var written = Encoding.ASCII.GetBytes(result, span);
        target.Advance(written);
    }
}