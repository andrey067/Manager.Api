using System.Security.Claims;

namespace Manager.Api.Authentication;

public sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public long? UserId
    {
        get
        {
            var id = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id is not null && long.TryParse(id, out var userId) ? userId : null;
        }
    }
}
