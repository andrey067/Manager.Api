using Xunit;

namespace Manager.Vsa.Tests.Features.Users;

[CollectionDefinition("RemoveUser")]
public sealed class RemoveUserCollection : ICollectionFixture<CreateUserWebApplicationFactory>
{
}
