import { User } from '../../domain/entities/User';
import { UserDTO, CreateUserDTO, UpdateUserDTO } from '../dtos/UserDTO';
import { DomainError, NotFoundError, ConflictError } from '../../core/errors';
import { IUserRepository } from './IUserRepository';

function toDTO(user: User): UserDTO {
  return { id: user.id, name: user.name, email: user.email };
}

export class UserService {
  constructor(private _repo: IUserRepository) {}

  async create(dto: CreateUserDTO): Promise<UserDTO> {
    const existing = await this._repo.getByEmail(dto.email);
    if (existing) {
      throw new ConflictError(`Email '${dto.email}' is already registered.`);
    }

    const user = new User(dto.name, dto.email, dto.password);
    if (!user.isValid) {
      throw new DomainError(user.errors);
    }

    const saved = await this._repo.create(user);
    return toDTO(saved);
  }

  async update(dto: UpdateUserDTO): Promise<UserDTO> {
    const existing = await this._repo.getById(dto.id);
    if (!existing) {
      throw new NotFoundError(`User with id ${dto.id} not found.`);
    }

    existing.setName(dto.name);
    existing.setEmail(dto.email);
    existing.setPassword(dto.password);
    if (!existing.isValid) {
      throw new DomainError(existing.errors);
    }

    const saved = await this._repo.update(existing);
    return toDTO(saved);
  }

  async delete(id: number): Promise<void> {
    const existing = await this._repo.getById(id);
    if (!existing) {
      throw new NotFoundError(`User with id ${id} not found.`);
    }
    await this._repo.delete(id);
  }

  async getById(id: number): Promise<UserDTO> {
    const user = await this._repo.getById(id);
    if (!user) {
      throw new NotFoundError(`User with id ${id} not found.`);
    }
    return toDTO(user);
  }

  async getAll(): Promise<UserDTO[]> {
    const users = await this._repo.getAll();
    return users.map(u => toDTO(u));
  }

  async searchByName(name: string): Promise<UserDTO[]> {
    const users = await this._repo.searchByName(name);
    return users.map(u => toDTO(u));
  }

  async searchByEmail(email: string): Promise<UserDTO[]> {
    const users = await this._repo.searchByEmail(email);
    return users.map(u => toDTO(u));
  }

  async getByEmail(email: string): Promise<UserDTO> {
    const user = await this._repo.getByEmail(email);
    if (!user) {
      throw new NotFoundError(`User with email '${email}' not found.`);
    }
    return toDTO(user);
  }
}