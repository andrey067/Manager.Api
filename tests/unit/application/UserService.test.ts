import { describe, it, expect, vi, beforeEach } from 'vitest';
import { UserService } from '../../../src/application/services/UserService';
import { IUserRepository } from '../../../src/application/interfaces/IUserRepository';
import { User } from '../../../src/domain/entities/User';

function makeUser(id = 1): User {
  const user = new User('John Doe', 'john.doe@example.com', 'secret123');
  user.id = id;
  return user;
}

function makeRepo() {
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

describe('UserService', () => {
  let service: UserService;
  let repo: IUserRepository;

  beforeEach(() => {
    repo = makeRepo();
    service = new UserService(repo);
  });

  describe('create', () => {
    it('should create a user and return DTO', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(null);
      const created = makeUser(1);
      vi.mocked(repo.create).mockResolvedValue(created);

      const result = await service.create({
        name: 'John Doe',
        email: 'john.doe@example.com',
        password: 'secret123',
      });

      expect(result).not.toBeNull();
      expect(result!.name).toBe('John Doe');
    });

    it('should throw when email already exists', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(makeUser());

      await expect(
        service.create({
          name: 'John Doe',
          email: 'john.doe@example.com',
          password: 'secret123',
        }),
      ).rejects.toThrow('already registered');
    });

    it('should throw when domain validation fails', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(null);

      await expect(
        service.create({ name: '', email: 'john.doe@example.com', password: 'secret123' }),
      ).rejects.toThrow('O nome não pode ser vazio');
    });
  });

  describe('update', () => {
    it('should update and return DTO', async () => {
      const existing = makeUser(1);
      vi.mocked(repo.getById).mockResolvedValue(existing);
      vi.mocked(repo.update).mockResolvedValue(existing);

      const result = await service.update({ id: 1, name: 'Jane Doe', email: 'john.doe@example.com', password: 'secret123' });
      expect(result!.name).toBe('Jane Doe');
    });

    it('should throw when user not found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(null);
      await expect(service.update({ id: 99, name: 'Test', email: 'test@test.com', password: 'secret123' })).rejects.toThrow('not found');
    });
  });

  describe('delete', () => {
    it('should call repo.delete', async () => {
      vi.mocked(repo.getById).mockResolvedValue(makeUser(1));
      vi.mocked(repo.delete).mockResolvedValue();
      await service.delete(1);
      expect(repo.delete).toHaveBeenCalledWith(1);
    });

    it('should throw when user not found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(null);
      await expect(service.delete(99)).rejects.toThrow('not found');
    });
  });

  describe('getById', () => {
    it('should return DTO when found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(makeUser(1));
      const result = await service.getById(1);
      expect(result).not.toBeNull();
      expect(result!.id).toBe(1);
    });

    it('should throw when not found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(null);
      await expect(service.getById(99)).rejects.toThrow('not found');
    });
  });

  describe('getAll', () => {
    it('should return list of DTOs', async () => {
      vi.mocked(repo.getAll).mockResolvedValue([makeUser(1), makeUser(2)]);
      const result = await service.getAll();
      expect(result).toHaveLength(2);
    });
  });

  describe('searchByName', () => {
    it('should return matching users', async () => {
      vi.mocked(repo.searchByName).mockResolvedValue([makeUser(1)]);
      const result = await service.searchByName('John');
      expect(result).toHaveLength(1);
    });
  });

  describe('getByEmail', () => {
    it('should return DTO when found', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(makeUser(1));
      const result = await service.getByEmail('john.doe@example.com');
      expect(result!.email).toBe('john.doe@example.com');
    });
  });
});