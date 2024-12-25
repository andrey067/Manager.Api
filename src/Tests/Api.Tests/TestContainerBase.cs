using Testcontainers.MsSql;

namespace Api.Tests;

public class TestContainerBase : IAsyncLifetime
{
    protected readonly MsSqlContainer MsSqlContainer;

    public TestContainerBase()
    {
        MsSqlContainer = new MsSqlBuilder()
            .WithPassword("yourStrong(!)Password")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await MsSqlContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await MsSqlContainer.StopAsync();
    }
}