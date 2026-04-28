import express from 'express';
import cors from 'cors';
import { createUserRouter } from './routers/userRouter';

export function createApp(): express.Application {
  const app = express();

  app.use(cors());
  app.use(express.json());

  app.use('/api/v1/users', createUserRouter());

  app.get('/health', (_req, res) => {
    res.json({ status: 'ok' });
  });

  return app;
}