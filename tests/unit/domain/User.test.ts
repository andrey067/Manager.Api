import { describe, it, expect } from 'vitest';
import { User } from '../../../src/domain/entities/User';

describe('User entity', () => {
  const validName = 'John Doe';
  const validEmail = 'john.doe@example.com';
  const validPassword = 'secret123';

  describe('construction', () => {
    it('should create a valid user', () => {
      const user = new User(validName, validEmail, validPassword);
      expect(user.isValid).toBe(true);
      expect(user.errors).toHaveLength(0);
      expect(user.name).toBe(validName);
      expect(user.email).toBe(validEmail);
      expect(user.password).toBe(validPassword);
    });

    it('should be invalid when name is empty', () => {
      const user = new User('', validEmail, validPassword);
      expect(user.isValid).toBe(false);
      expect(user.errors.some((e) => e.includes('nome'))).toBe(true);
    });

    it('should be invalid when name is too short (< 3 chars)', () => {
      const user = new User('Jo', validEmail, validPassword);
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when name is too long (> 80 chars)', () => {
      const user = new User('A'.repeat(81), validEmail, validPassword);
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when email is empty', () => {
      const user = new User(validName, '', validPassword);
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when email format is wrong', () => {
      const user = new User(validName, 'not-an-email', validPassword);
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when email is too short (< 10 chars)', () => {
      const user = new User(validName, 'a@b.c', validPassword);
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when password is empty', () => {
      const user = new User(validName, validEmail, '');
      expect(user.isValid).toBe(false);
    });

    it('should be invalid when password is too short (< 6 chars)', () => {
      const user = new User(validName, validEmail, 'abc');
      expect(user.isValid).toBe(false);
    });
  });

  describe('mutations', () => {
    it('setName should update name and revalidate', () => {
      const user = new User(validName, validEmail, validPassword);
      user.setName('Jane Doe');
      expect(user.name).toBe('Jane Doe');
      expect(user.isValid).toBe(true);
    });

    it('setName with invalid value should mark user invalid', () => {
      const user = new User(validName, validEmail, validPassword);
      user.setName('X');
      expect(user.isValid).toBe(false);
    });

    it('setEmail should update email and revalidate', () => {
      const user = new User(validName, validEmail, validPassword);
      user.setEmail('new.email@example.com');
      expect(user.email).toBe('new.email@example.com');
      expect(user.isValid).toBe(true);
    });

    it('setPassword should update password and revalidate', () => {
      const user = new User(validName, validEmail, validPassword);
      user.setPassword('newpassword');
      expect(user.password).toBe('newpassword');
      expect(user.isValid).toBe(true);
    });
  });

  describe('errorsToString', () => {
    it('should return joined error messages', () => {
      const user = new User('', '', '');
      expect(user.errorsToString()).toContain('nome');
    });
  });
});
