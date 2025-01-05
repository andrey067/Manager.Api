using Api.Database.VOs;

namespace Api.Database.Entities;

public class User
{
    //EF
#pragma warning disable CS0628
    protected User() { }
#pragma warning disable CS0628

    //Propriedades
    public long Id { get; set; }
    public UserName Name { get; private set; }
    public string Email { get; private set; } = null!;
    public string Password { get; private set; } = null!;

    private User(string firtsName, string lastName, string email, string password)
    {
        Name = UserName.Create(firtsName, lastName);;
        Email = email;
        Password = password;
    }

    public static User Create(string firtsName, string lastName, string email, string password)
    {
        return new User(firtsName, lastName, email, password);
    }

    public void Update(string firtsName, string lastName, string email, string password)
    {
        Name = UserName.Create(firtsName, lastName);;
        Email = email;
        Password = password;
    }
}