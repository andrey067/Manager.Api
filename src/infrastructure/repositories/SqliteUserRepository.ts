import { User } from '../../domain/entities/User';
import { IUserRepository } from '../../application/interfaces/IUserRepository';
import { getDatabase } from '../database';

function toDomain(model: any): User {
  const user = new User(model.name, model.email, model.password);
  user.id = model.id;
  return user;
}

function promisify<T>(fn: (callback: (err: Error | null, result: T) => void) => void): Promise<T> {
  return new Promise((resolve, reject) => {
    fn((err, result) => {
      if (err) reject(err);
      else resolve(result);
    });
  });
}

export class SqliteUserRepository implements IUserRepository {
  async create(user: User): Promise<User> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.run(
        'INSERT INTO users (name, email, password) VALUES (?, ?, ?)',
        [user.name, user.email, user.password],
        function(err) {
          if (err) reject(err);
          else {
            user.id = this.lastID;
            resolve(user);
          }
        }
      );
    });
  }

  async update(user: User): Promise<User> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.run(
        'UPDATE users SET name = ?, email = ?, password = ? WHERE id = ?',
        [user.name, user.email, user.password, user.id],
        function(err) {
          if (err) reject(err);
          else resolve(user);
        }
      );
    });
  }

  async delete(id: number): Promise<void> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.run('DELETE FROM users WHERE id = ?', [id], function(err) {
        if (err) reject(err);
        else resolve();
      });
    });
  }

  async getById(id: number): Promise<User | null> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.get('SELECT * FROM users WHERE id = ?', [id], (err, row) => {
        if (err) reject(err);
        else resolve(row ? toDomain(row) : null);
      });
    });
  }

  async getAll(): Promise<User[]> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.all('SELECT * FROM users', [], (err, rows) => {
        if (err) reject(err);
        else resolve(rows.map((row: any) => toDomain(row)));
      });
    });
  }

  async getByEmail(email: string): Promise<User | null> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.get('SELECT * FROM users WHERE email = ?', [email], (err, row) => {
        if (err) reject(err);
        else resolve(row ? toDomain(row) : null);
      });
    });
  }

  async searchByName(name: string): Promise<User[]> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.all('SELECT * FROM users WHERE name LIKE ?', [`%${name}%`], (err, rows) => {
        if (err) reject(err);
        else resolve(rows.map((row: any) => toDomain(row)));
      });
    });
  }

  async searchByEmail(email: string): Promise<User[]> {
    const db = getDatabase();
    return new Promise((resolve, reject) => {
      db.all('SELECT * FROM users WHERE email LIKE ?', [`%${email}%`], (err, rows) => {
        if (err) reject(err);
        else resolve(rows.map((row: any) => toDomain(row)));
      });
    });
  }
}