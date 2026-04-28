# Manager API — Express / TypeScript

Clean Architecture study implementation of the Manager API using **Node.js**, **Express**, and **TypeScript**.

## Stack

| Layer          | Technology             |
| -------------- | ---------------------- |
| API            | Express 4              |
| Language       | TypeScript 5           |
| ORM            | TypeORM + SQLite       |
| Password hashing | bcryptjs             |
| Tests          | Vitest + Supertest     |

## Structure

```
src/
  api/            # Routes, view models (Presentation)
  domain/         # Entities, validators (Domain)
  application/    # Services, DTOs, interfaces (Application)
  infrastructure/ # TypeORM entities, repositories (Infrastructure)
  core/           # Errors, utilities (Cross-cutting)
tests/
  unit/           # Domain & service unit tests (no I/O)
  integration/    # Route-level tests (mocked service)
```

## Getting started

```bash
npm install
npm test          # run all tests
npm run dev       # start dev server
```

## TDD approach

1. Domain entity tests (`tests/unit/domain/`) — pure, no I/O
2. Service tests (`tests/unit/application/`) — repository mocked with `vi.fn()`
3. Route integration tests (`tests/integration/`) — service mocked with Supertest
4. Real DB integration — TypeORM SQLite (sync mode for simplicity)
