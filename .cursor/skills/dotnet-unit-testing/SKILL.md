---
name: dotnet-unit-testing
description: Escreve testes unitários com xUnit, Moq, Bogus e FluentAssertions em .NET. Use ao criar ou alterar testes de serviços, repositórios ou quando o usuário menciona testes, xUnit ou Moq.
---

# Testes Unitários neste Projeto

## Stack

- **xUnit** – framework
- **Moq** – mocks
- **Bogus** – dados fake
- **FluentAssertions** – asserções

## Local

`dotnet/tests/Manager.Tests/Projects/Services/`

## Padrão Arrange/Act/Assert

```csharp
[Fact(DisplayName = "Nome do teste")]
[Trait("Category", "Services")]
public async Task Method_WhenCondition_ReturnsExpected()
{
    // Arrange
    var input = Fixture.CreateValidDTO();
    _mock.Setup(x => x.Method(It.IsAny<T>())).ReturnsAsync(result);

    // Act
    var result = await _sut.MethodAsync(input);

    // Assert
    result.Value.Should().BeEquivalentTo(expected);
}
```

## Fixtures

- `Manager.Tests/Fixtures/UserFixture.cs` – `CreateValidUser()`, `CreateValidUserDTO()`, `CreateInvalidUserDTO()`, `CreateListValidUser()`

## Configuração Mapster

- `MapsterConfiguration.Apply()` no construtor da classe de teste.
