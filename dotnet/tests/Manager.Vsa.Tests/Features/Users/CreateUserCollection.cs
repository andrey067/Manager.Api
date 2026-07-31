using Xunit;

namespace Manager.Vsa.Tests.Features.Users;

[CollectionDefinition("CreateUser")]
public sealed class CreateUserCollection : ICollectionFixture<CreateUserWebApplicationFactory>
{
}
