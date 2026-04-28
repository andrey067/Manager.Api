import { describe, it, expect, vi, beforeEach } from 'vitest';
import { UserService } from '../../../src/application/services/UserService';
import { IUserRepository } from '../../../src/application/interfaces/IUserRepository';
import { User } from '../../../src/domain/entities/User';

// Mock bcryptjs to keep tests fast
vi.mock('../../../src/core/utils/password', () => ({
  hashPassword: async (p: string) => `hashed:${p}`,
  comparePassword: async () => true,
}));

const makeUser = (id = 1): User =>
  new User('John Doe', 'john.doe@example.com', 'secret123', id);

function makeRepo(): IUserRepository {
  return {
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    getById: vi.fn(),
    getByEmail: vi.fn(),
    getAll: vi.fn(),
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
      expect(repo.create).toHaveBeenCalledOnce();
    });

    it('should throw when email already exists', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(makeUser());

      await expect(
        service.create({
          name: 'John Doe',
          email: 'john.doe@example.com',
          password: 'secret123',
        }),
      ).rejects.toThrow('User already exists');
    });

    it('should throw when domain validation fails', async () => {
      vi.mocked(repo.getByEmail).mockResolvedValue(null);

      await expect(
        service.create({ name: '', email: 'john.doe@example.com', password: 'secret123' }),
      ).rejects.toThrow('validation failed');
    });
  });

  describe('update', () => {
    it('should update and return DTO', async () => {
      const existing = makeUser(1);
      vi.mocked(repo.getById).mockResolvedValue(existing);
      vi.mocked(repo.update).mockResolvedValue(existing);

      const result = await service.update({ id: 1, name: 'Jane Doe' });
      expect(result!.name).toBe('Jane Doe');
    });

    it('should throw when user not found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(null);
      await expect(service.update({ id: 99 })).rejects.toThrow('not found');
    });
  });

  describe('remove', () => {
    it('should call repo.remove', async () => {
      vi.mocked(repo.remove).mockResolvedValue();
      await service.remove(1);
      expect(repo.remove).toHaveBeenCalledWith(1);
    });
  });

  describe('getById', () => {
    it('should return DTO when found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(makeUser(1));
      const result = await service.getById(1);
      expect(result).not.toBeNull();
      expect(result!.id).toBe(1);
    });

    it('should return null when not found', async () => {
      vi.mocked(repo.getById).mockResolvedValue(null);
      const result = await service.getById(99);
      expect(result).toBeNull();
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
