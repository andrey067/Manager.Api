using Manager.Api.Common.Domain;

namespace Manager.Api.Features.Users;

public sealed record UserRemovedDomainEvent(long Id) : IDomainEvent;
