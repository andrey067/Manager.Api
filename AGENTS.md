# Manager API - Instruções Gerais

Projeto organizado em pastas por linguagem. Cada pasta tem seu próprio agente com instruções específicas.

## Estrutura

- **dotnet/** – Projeto .NET (Vertical Slice Architecture, ASP.NET Core, EF Core)
- **python/** – Projeto Python (Vertical Slice Architecture, FastAPI, SQLAlchemy)

## Arquitetura (VSA)

Ambas as stacks seguem **Vertical Slice Architecture**: um deployável por linguagem, um arquivo/módulo por caso de uso, sem MediatR nem camadas Domain/Services/Infra.

Pastas compartilhadas por stack: `Features` (ou `features/`), `Common`, `Database`, `Authentication`.

## Escopo dos Agentes

- Ao trabalhar em **dotnet/**: use o Agente Dotnet (ver `dotnet/AGENTS.md`)
- Ao trabalhar em **python/**: use o Agente Python (ver `python/AGENTS.md`)

O Cursor aplica automaticamente as instruções do agente correspondente conforme o diretório de trabalho.

## Revisão de convenções

Antes de commitar slices novos ou alterados, use `/vsa-review` no diff.
