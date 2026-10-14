using System.Data.SqlTypes;
using System.Net;
using Partners.Tests.Infrastructure;

namespace Partners.Tests.Api;

public sealed class ListPartnersTests(PartnersApiFactory factory) : ApiTestBase(factory)
{
    // Scenario 2
    [Fact]
    public async Task TypeFilter_BothAppearsInEveryFilter_AndCustomerIsNotASupplier()
    {
        var both = await CreatePartnerAsync("Ambos Lda", "100000001", "BOTH");
        var customer = await CreatePartnerAsync("Cliente Lda", "100000002", "CUSTOMER");
        var supplier = await CreatePartnerAsync("Fornecedor Lda", "100000003", "SUPPLIER");

        var customers = await ListAsync("?Type=CUSTOMER");
        var suppliers = await ListAsync("?Type=SUPPLIER");
        var onlyBoth = await ListAsync("?Type=BOTH");

        Assert.Equal([both.Id, customer.Id], customers.Items.Select(p => p.Id));
        Assert.Equal([both.Id, supplier.Id], suppliers.Items.Select(p => p.Id));
        Assert.Equal([both.Id], onlyBoth.Items.Select(p => p.Id));
        Assert.DoesNotContain(suppliers.Items, p => p.Id == customer.Id);
    }

    // Scenario 7
    [Fact]
    public async Task Filters_AreCombinedWithAnd()
    {
        await CreatePartnerAsync("Ana Costa", "200000001", "CUSTOMER");
        await CreatePartnerAsync("Ana Fornecedora", "200000002", "SUPPLIER");
        await CreatePartnerAsync("Ana Ambos", "200000003", "BOTH");
        await CreatePartnerAsync("Bruno Costa", "200000004", "CUSTOMER");
        var inactive = await CreatePartnerAsync("Ana Inativa", "200000005", "CUSTOMER");
        var body = UpdateBodyFrom(inactive);
        body["isActive"] = false;
        Assert.Equal(HttpStatusCode.OK, (await PutAsync(inactive.Id, body)).StatusCode);

        var activeOnly = await ListAsync("?Type=CUSTOMER&Search=ana");
        var withInactive = await ListAsync("?Type=CUSTOMER&Search=ana&IncludeInactive=true");

        Assert.Equal(["Ana Ambos", "Ana Costa"], activeOnly.Items.Select(p => p.Name));
        Assert.Equal(2, activeOnly.TotalCount);
        Assert.Equal(["Ana Ambos", "Ana Costa", "Ana Inativa"], withInactive.Items.Select(p => p.Name));
        Assert.Equal(3, withInactive.TotalCount);
    }

    // Scenario 7
    [Fact]
    public async Task Paging_CountsBeforePaging_AndBreaksTiesById()
    {
        var sameName = new List<Guid>();
        for (var i = 1; i <= 3; i++)
            sameName.Add((await CreatePartnerAsync("Mesmo Nome", $"30000000{i}")).Id);
        await CreatePartnerAsync("Antes", "300000004");

        var firstPage = await ListAsync("?Page=1&PageSize=2");
        var secondPage = await ListAsync("?Page=2&PageSize=2");

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(4, secondPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(2, secondPage.Items.Count);

        // SQL Server compares uniqueidentifier differently from System.Guid; SqlGuid reproduces its order.
        var expectedTieOrder = sameName.OrderBy(id => new SqlGuid(id)).ToList();
        var actualOrder = firstPage.Items.Concat(secondPage.Items).ToList();
        Assert.Equal("Antes", actualOrder[0].Name);
        Assert.Equal(expectedTieOrder, actualOrder.Skip(1).Select(p => p.Id));
    }

    [Fact]
    public async Task Paging_PageBeyondResults_ReturnsEmptyItemsAndCorrectTotal()
    {
        await CreatePartnerAsync("Ana Silva", "400000001");
        await CreatePartnerAsync("Bruno Silva", "400000002");

        var page = await ListAsync("?Page=99&PageSize=10");

        Assert.Empty(page.Items);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(99, page.Page);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task Paging_UsesDefaults()
    {
        var page = await ListAsync();

        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
    }

    // Scenario 7
    [Fact]
    public async Task Search_ByName_DoesNotMatchEveryPartnerThroughTaxNumber()
    {
        var ana = await CreatePartnerAsync("Ana Silva", "500000001");
        await CreatePartnerAsync("Bruno Costa", "500000002");
        await CreatePartnerAsync("Carla Dias", "500000003");

        var result = await ListAsync("?Search=Ana");

        Assert.Equal([ana.Id], result.Items.Select(p => p.Id));
        Assert.Equal(1, result.TotalCount);
    }

    [Theory]
    [InlineData("---")]
    [InlineData(". .")]
    public async Task Search_WithOnlySeparators_DoesNotMatchEveryTaxNumber(string search)
    {
        await CreatePartnerAsync("Ana Silva", "500000001");
        await CreatePartnerAsync("Bruno Costa", "500000002");

        var result = await ListAsync($"?Search={Uri.EscapeDataString(search)}");

        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData("ANA")]
    [InlineData("silva")]
    public async Task Search_ByName_IsCaseInsensitiveAndPartial(string search)
    {
        var ana = await CreatePartnerAsync("Ana Silva", "600000001");
        await CreatePartnerAsync("Bruno Costa", "600000002");

        var result = await ListAsync($"?Search={search}");

        Assert.Equal([ana.Id], result.Items.Select(p => p.Id));
    }

    [Theory]
    [InlineData("001.234")]
    [InlineData("234 567")]
    [InlineData("001-234-567")]
    public async Task Search_ByTaxNumber_NormalizesSeparators(string search)
    {
        var ana = await CreatePartnerAsync("Ana Silva", "001234567");
        await CreatePartnerAsync("Bruno Costa", "700000002");

        var result = await ListAsync($"?Search={Uri.EscapeDataString(search)}");

        Assert.Equal([ana.Id], result.Items.Select(p => p.Id));
    }

    // Scenario 6
    [Theory]
    [InlineData("?Page=0")]
    [InlineData("?Page=-1")]
    [InlineData("?PageSize=0")]
    [InlineData("?PageSize=101")]
    [InlineData("?Type=CLIENT")]
    [InlineData("?Page=abc")]
    [InlineData("?IncludeInactive=maybe")]
    public async Task List_WithInvalidQuery_ReturnsBadRequest(string queryString)
    {
        var response = await Client.GetAsync($"{PartnersUrl}{queryString}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }
}
