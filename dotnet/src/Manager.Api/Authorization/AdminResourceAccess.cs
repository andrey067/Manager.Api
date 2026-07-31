namespace Manager.Api.Authorization;

/// <summary>
/// Manager API treats some features as admin resource management:
/// JWT is required, but handlers do not scope rows to <c>IUserContext.UserId</c>.
/// </summary>
public static class AdminResourceAccess
{
    private static readonly HashSet<string> AdminManagedFeatures =
        new(StringComparer.Ordinal) { "Users" };

    public static bool IsAdminManagedFeature(string featureName) =>
        AdminManagedFeatures.Contains(featureName);

    public static bool RequiresResourceOwnership(string featureName) =>
        !IsAdminManagedFeature(featureName);
}
