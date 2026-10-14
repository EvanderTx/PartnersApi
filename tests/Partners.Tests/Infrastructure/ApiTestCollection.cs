namespace Partners.Tests.Infrastructure;

// All API tests share one database, so they run sequentially in a single collection.
[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<PartnersApiFactory>
{
    public const string Name = "Partners API";
}
