using Xunit;

namespace Manager.Vsa.Tests.Features.Users;

[CollectionDefinition("RegisterBootstrap")]
public sealed class RegisterBootstrapCollection : ICollectionFixture<RegisterBootstrapWebApplicationFactory>
{
}
