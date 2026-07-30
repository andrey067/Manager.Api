namespace Manager.Api.Features.Users;

public static class UserCacheKeys
{
    public const string All = "users:all";

    public static string ById(long id) => $"users:{id}";

    public static string ByEmail(string email) => $"users:email:{email.ToLowerInvariant()}";
}
