using Xunit;

namespace Manager.Vsa.Tests.Features.Users;

[CollectionDefinition("UpdateUser")]
public sealed class UpdateUserCollection : ICollectionFixture<CreateUserWebApplicationFactory>
{
}
