using TicketGate.Core.Pagination;

namespace TicketGate.Payment.Tests;

public sealed class PagedResultTests
{
    [Fact]
    public void ComputedProperties_ShouldReflectPageState()
    {
        var page = new PagedResult<int>([1, 2, 3], TotalCount: 25, Page: 2, PageSize: 10);

        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.True(page.HasPreviousPage);
    }

    [Fact]
    public void ComputedProperties_ShouldHandleEmptyResults()
    {
        var page = new PagedResult<int>([], TotalCount: 0, Page: 1, PageSize: 10);

        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNextPage);
        Assert.False(page.HasPreviousPage);
    }
}
