using Microsoft.AspNetCore.OutputCaching;

namespace TicketGate.Event.Tests;

internal sealed class FakeOutputCacheStore : IOutputCacheStore
{
    public string? EvictedTag { get; private set; }

    public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult<byte[]?>(null);
    }

    public ValueTask SetAsync(
        string key,
        byte[] value,
        string[]? tags,
        TimeSpan validFor,
        CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
    {
        EvictedTag = tag;
        return ValueTask.CompletedTask;
    }
}

