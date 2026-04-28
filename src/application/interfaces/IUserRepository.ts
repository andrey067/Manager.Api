import { User } from '../../domain/entities/User';

export interface IUserRepository {
  create(user: User): Promise<User>;
  update(user: User): Promise<User>;
  delete(id: number): Promise<void>;
  getById(id: number): Promise<User | null>;
  getAll(): Promise<User[]>;
  getByEmail(email: string): Promise<User | null>;
  searchByName(name: string): Promise<User[]>;
  searchByEmail(email: string): Promise<User[]>;
}