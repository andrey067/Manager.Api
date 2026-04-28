import 'reflect-metadata';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/api/app';

vi.mock('../../src/infrastructure/database', () => {
  const sqlite3 = require('sqlite3');
  const db = new sqlite3.Database(':memory:');
  db.exec(`
    CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL,
      email TEXT NOT NULL UNIQUE,
      password TEXT NOT NULL
    )
  `);
  return { getDatabase: () => db, closeDatabase: () => db.close() };
});

describe('User routes integration', () => {
  let app: ReturnType<typeof createApp>;

  beforeEach(() => {
    app = createApp();
  });

  it('POST /api/v1/users → 201 on success', async () => {
    const res = await request(app)
      .post('/api/v1/users')
      .send({ name: 'John Doe', email: `john${Date.now()}@example.com`, password: 'secret123' });
    expect(res.status).toBe(201);
    expect(res.body.email).toContain('@example.com');
  });

  it('GET /api/v1/users → 200 with list', async () => {
    const res = await request(app).get('/api/v1/users');
    expect(res.status).toBe(200);
  });

  it('GET /api/v1/users/:id → 404 when not found', async () => {
    const res = await request(app).get('/api/v1/users/9999');
    expect(res.status).toBe(404);
  });

  it('health check → 200', async () => {
    const res = await request(app).get('/health');
    expect(res.status).toBe(200);
    expect(res.body.status).toBe('ok');
  });
});