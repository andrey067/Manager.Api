---
name: dotnet-mapster-mapping
description: Configura mapeamentos com Mapster entre ViewModel, DTO e Entity em .NET. Use ao adicionar mapeamentos, novas entidades ou quando o usuário menciona Mapster, Adapt ou mapeamento.
---

# Mapster neste Projeto

## Local

`dotnet/src/1 - Manager.API/Extensions/ServiceCollectionExtensions.cs` – método `AddMapster`.

## Padrão de Mapeamento

```csharp
config.NewConfig<Entity, DTO>();
config.NewConfig<CreateViewModel, DTO>();
config.NewConfig<UpdateViewModel, DTO>();

config.NewConfig<DTO, Entity>()
    .ConstructUsing(dto => new Entity(dto.Prop1, dto.Prop2))
    .AfterMapping((dto, entity) => entity.Id = dto.Id);
```

## Uso

```csharp
var dto = viewModel.Adapt<UserDTO>();
var entity = dto.Adapt<User>();
var result = entity.Adapt<UserDTO>();
```

## Direções

- ViewModel → DTO (entrada API)
- DTO → Entity (criação/atualização)
- Entity → DTO (retorno do repositório)
