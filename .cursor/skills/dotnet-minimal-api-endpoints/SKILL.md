---
name: dotnet-minimal-api-endpoints
description: Cria endpoints com Minimal API e Vertical Slice em ASP.NET Core. Use ao criar ou alterar endpoints REST, rotas ou quando o usuário menciona Minimal API, endpoints ou Features.
---

# Endpoints Minimal API com Vertical Slice

## Padrão de Estrutura

```
Features/
  {Entidade}/
    {Entidade}Endpoints.cs
  Common/
    EndpointResultHelper.cs
```

## Template de Endpoint

```csharp
var group = app.MapGroup("/api/v1/{recurso}").RequireAuthorization();

group.MapPost("/create", CreateAsync);
group.MapPut("/update", UpdateAsync);
group.MapDelete("/remove/{id:long}", RemoveAsync);
group.MapGet("/get/{id:long}", GetAsync);
group.MapGet("/get-all", GetAllAsync);
```

## Handler Pattern

```csharp
private static async Task<IResult> CreateAsync(
    [FromBody] CreateViewModel viewModel,
    [FromServices] I{Entidade}Service service,
    [FromServices] DomainNotificationHandler notificationHandler)
{
    var dto = viewModel.Adapt<DTO>();
    var result = await service.CreateAsync(dto);
    var errorResult = EndpointResultHelper.ResultFromNotifications(notificationHandler);
    if (errorResult != null) return errorResult;
    return Results.Ok(new ResultViewModel { Message = "...", Success = true, Data = result.Value });
}
```

## Registro

Em `Program.cs`: `app.Map{Entidade}Endpoints();`
