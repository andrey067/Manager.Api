---
name: dotnet-validation-domain
description: Aplica FluentValidation e Domain Notifications via MediatR em .NET. Use ao validar entidades, tratar erros de domínio ou quando o usuário menciona validação ou DomainNotification.
---

# Validação e Domain Notifications

## FluentValidation

- Validators em `dotnet/src/2 - Manager.Domain/Validators/`
- Entidade herda `AbstractValidator<T>`, chama `Validate()` no construtor ou método
- Propriedade `IsValid` e `Errors` na entidade

## Domain Notification

```csharp
await _mediator.PublishDomainNotificationAsync(new DomainNotification(
    ErrorMessages.Mensagem,
    DomainNotificationType.Tipo));
```

## Handler

- `DomainNotificationHandler` em `dotnet/src/5 - Manager.Core/Communication/Handlers/`
- `EndpointResultHelper.ResultFromNotifications(notificationHandler)` retorna `IResult` com erro ou null

## Fluxo no Endpoint

1. Chamar serviço
2. `var errorResult = EndpointResultHelper.ResultFromNotifications(notificationHandler);`
3. Se `errorResult != null`, retornar `errorResult`
4. Caso contrário, retornar sucesso
