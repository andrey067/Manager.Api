export class DomainError extends Error {
  constructor(public errors: string[]) {
    super(errors.join('; '));
    this.name = 'DomainError';
  }
}

export class NotFoundError extends Error {
  constructor(message = 'Resource not found') {
    super(message);
    this.name = 'NotFoundError';
  }
}

export class ConflictError extends Error {
  constructor(message = 'Resource already exists') {
    super(message);
    this.name = 'ConflictError';
  }
}