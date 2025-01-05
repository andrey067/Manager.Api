using Api.Database;
using Api.Database.Entities;
using Bogus;

namespace Api.Services;

public class SeedService(ManagerContext context)
{
    public async Task SeedDataAsync()
    {
        if (!context.Users.Any())
        {
            var faker = new Faker<User>()
                .CustomInstantiator(f => User.Create(
                    f.Name.FirstName(),
                    f.Name.LastName(),
                    f.Internet.Email(),
                    f.Internet.Password()));

            var users = faker.Generate(10); // Gera 10 usuários falsos

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }
    }
}