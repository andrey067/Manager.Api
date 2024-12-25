using ValueOf;

namespace Api.Database.ValueObjects;

public class Name : ValueOf<(string FirstName, string LastName), Name>
{
    protected override void Validate()
    {
        if (string.IsNullOrWhiteSpace(Value.FirstName))
        {
            throw new ArgumentException("First name cannot be empty", nameof(Value.FirstName));
        }

        if (string.IsNullOrWhiteSpace(Value.LastName))
        {
            throw new ArgumentException("Last name cannot be empty", nameof(Value.LastName));
        }
    }

    public override string ToString() => $"{Value.FirstName} {Value.LastName}";
}