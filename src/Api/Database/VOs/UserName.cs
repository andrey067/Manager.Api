using Api.Common;

namespace Api.Database.VOs;

public class UserName : ValueObject
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    
    private UserName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
    
    public static UserName Create(string firstName, string lastName)
    {
        return new UserName(firstName, lastName);
    }

    public override string ToString()
    {
        return $"{FirstName} {LastName}";
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        yield return FirstName;
        yield return LastName;
    }
}