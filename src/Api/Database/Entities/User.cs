using Api.Common;
using Api.Database.ValueObjects;

namespace Api.Database.Entities;

public class User : AggregateRoot<UserId, long>
{
    //Propriedades
    public Name Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Password { get; private set; } = null!;

    //EF
#pragma warning disable CS0628 
    protected User()    { }
#pragma warning disable CS0628 
    
    
    private User(string firtsName, string lastName, string email, string password)
    {
        var name = Name.From((firtsName, lastName));
        Name = name;
        Email = email;
        Password = password;
    }

    public static User Create(string firtsName, string lastName, string email, string password)
        => new(firtsName, lastName, email, password);      

    public void Update(string firtsName, string lastName, string email, string password)
    {
        var name = Name.From((firtsName, lastName));
        Name = name;
        Email = email;
        Password = password;
    }
}