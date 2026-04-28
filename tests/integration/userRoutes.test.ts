import 'reflect-metadata';
import { describe, it, expect, vi, beforeEach, afterAll } from 'vitest';
import request from 'supertest';
import { createApp } from '../../src/app';
import { IUserService } from '../../src/application/interfaces/IUserService';

function makeService(): IUserService {
  return {
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    getById: vi.fn(),
    getAll: vi.fn(),
    searchByName: vi.fn(),
    searchByEmail: vi.fn(),
    getByEmail: vi.fn(),
  };
}

const sampleUser = {
  id: 1,
  name: 'John Doe',
  email: 'john.doe@example.com',
  password: 'hashed',
};

describe('User routes integration', () => {
  let service: IUserService;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let app: any;

  beforeEach(() => {
    service = makeService();
    app = createApp(service);
  });

  it('POST /users → 201 on success', async () => {
    vi.mocked(service.create).mockResolvedValue(sampleUser);
    const res = await request(app)
      .post('/users')
      .send({ name: 'John Doe', email: 'john.doe@example.com', password: 'secret123' });
    expect(res.status).toBe(201);
    expect(res.body.success).toBe(true);
    expect(res.body.data.name).toBe('John Doe');
  });

  it('POST /users → 400 on service error', async () => {
    vi.mocked(service.create).mockRejectedValue(new Error('User already exists'));
    const res = await request(app)
      .post('/users')
      .send({ name: 'John Doe', email: 'john.doe@example.com', password: 'secret123' });
    expect(res.status).toBe(400);
    expect(res.body.success).toBe(false);
  });

  it('GET /users → 200 with list', async () => {
    vi.mocked(service.getAll).mockResolvedValue([sampleUser]);
    const res = await request(app).get('/users');
    expect(res.status).toBe(200);
    expect(res.body.data).toHaveLength(1);
  });

  it('GET /users/:id → 200 when found', async () => {
    vi.mocked(service.getById).mockResolvedValue(sampleUser);
    const res = await request(app).get('/users/1');
    expect(res.status).toBe(200);
  });

  it('GET /users/:id → 404 when not found', async () => {
    vi.mocked(service.getById).mockResolvedValue(null);
    const res = await request(app).get('/users/999');
    expect(res.status).toBe(404);
  });

  it('PUT /users/:id → 200 on update', async () => {
    vi.mocked(service.update).mockResolvedValue({ ...sampleUser, name: 'Jane' });
    const res = await request(app).put('/users/1').send({ name: 'Jane' });
    expect(res.status).toBe(200);
    expect(res.body.data.name).toBe('Jane');
  });

  it('DELETE /users/:id → 204', async () => {
    vi.mocked(service.remove).mockResolvedValue();
    const res = await request(app).delete('/users/1');
    expect(res.status).toBe(204);
  });
});
