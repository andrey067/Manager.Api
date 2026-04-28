import { Router, Request, Response } from 'express';
import { UserService } from '../../application/services/UserService';
import { SqliteUserRepository } from '../../infrastructure/repositories/SqliteUserRepository';
import { DomainError, NotFoundError, ConflictError } from '../../core/errors';

export function createUserRouter(): Router {
  const router = Router();
  const repository = new SqliteUserRepository();
  const service = new UserService(repository);

  router.post('/', async (req: Request, res: Response) => {
    try {
      const { name, email, password } = req.body;
      const user = await service.create({ name, email, password });
      res.status(201).json(user);
    } catch (error) {
      if (error instanceof ConflictError) {
        return res.status(409).json({ detail: error.message });
      }
      if (error instanceof DomainError) {
        return res.status(422).json({ message: error.message, errors: error.errors });
      }
      console.error('Create user error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.put('/:userId', async (req: Request, res: Response) => {
    try {
      const userId = parseInt(req.params.userId, 10);
      const { name, email, password } = req.body;
      const user = await service.update({ id: userId, name, email, password });
      res.json(user);
    } catch (error) {
      if (error instanceof NotFoundError) {
        return res.status(404).json({ detail: error.message });
      }
      if (error instanceof DomainError) {
        return res.status(422).json({ message: error.message, errors: error.errors });
      }
      console.error('Update user error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.delete('/:userId', async (req: Request, res: Response) => {
    try {
      const userId = parseInt(req.params.userId, 10);
      await service.delete(userId);
      res.status(204).send();
    } catch (error) {
      if (error instanceof NotFoundError) {
        return res.status(404).json({ detail: error.message });
      }
      console.error('Delete user error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.get('/:userId', async (req: Request, res: Response) => {
    try {
      const userId = parseInt(req.params.userId, 10);
      const user = await service.getById(userId);
      res.json(user);
    } catch (error) {
      if (error instanceof NotFoundError) {
        return res.status(404).json({ detail: error.message });
      }
      console.error('Get user error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.get('/', async (_req: Request, res: Response) => {
    try {
      const users = await service.getAll();
      res.json(users);
    } catch (error) {
      console.error('Get all users error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.get('/search/name', async (req: Request, res: Response) => {
    try {
      const { name } = req.query;
      const users = await service.searchByName(name as string);
      res.json(users);
    } catch (error) {
      console.error('Search by name error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  router.get('/search/email', async (req: Request, res: Response) => {
    try {
      const { email } = req.query;
      const users = await service.searchByEmail(email as string);
      res.json(users);
    } catch (error) {
      console.error('Search by email error:', error);
      res.status(500).json({ detail: 'Internal server error' });
    }
  });

  return router;
}