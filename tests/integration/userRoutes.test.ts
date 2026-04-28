import 'reflect-metadata';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/api/app';
import { UserService } from '../../src/application/services/UserService';

function makeMockRepo() {
  return {
    create: vi.fn(),
    update: vi.fn(),
    delete: vi.fn(),
    getById: vi.fn(),
    getAll: vi.fn(),
    getByEmail: vi.fn(),
    searchByName: vi.fn(),
    searchByEmail: vi.fn(),
  };
}

const sampleUser = {
  id: 1,
  name: 'John Doe',
  email: 'john.doe@example.com',
  password: 'hashed',
};

describe('User routes integration', () => {
  let app: ReturnType<typeof createApp>;

  beforeEach(() => {
    app = createApp();
  });

  it('POST /api/v1/users → 201 on success', async () => {
    const res = await request(app)
      .post('/api/v1/users')
      .send({ name: 'John Doe', email: 'john.doe@example.com', password: 'secret123' });
    expect(res.status).toBe(201);
    expect(res.body.email).toBe('john.doe@example.com');
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