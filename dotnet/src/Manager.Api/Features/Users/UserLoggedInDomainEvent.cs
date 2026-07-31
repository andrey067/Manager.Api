using Manager.Api.Common.Domain;

namespace Manager.Api.Features.Users;

public sealed record UserLoggedInDomainEvent(long Id) : IDomainEvent;
