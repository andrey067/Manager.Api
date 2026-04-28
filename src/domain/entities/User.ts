export class UserValidator {
  private _errors: string[] = [];

  get errors(): string[] {
    return this._errors;
  }

  validate(user: User): boolean {
    this._errors = [];

    if (user.name === undefined || user.name === null) {
      this._errors.push('O nome não pode ser nulo.');
    } else if (user.name === '') {
      this._errors.push('O nome não pode ser vazio.');
    } else if (user.name.length < 3) {
      this._errors.push('O nome deve ter no mínimo 3 caracteres.');
    } else if (user.name.length > 80) {
      this._errors.push('O nome deve ter no máximo 80 caracteres.');
    }

    if (user.email === undefined || user.email === null) {
      this._errors.push('O email não pode ser nulo.');
    } else if (user.email === '') {
      this._errors.push('O email não pode ser vazio.');
    } else if (user.email.length < 10) {
      this._errors.push('O email deve ter no mínimo 10 caracteres.');
    } else if (user.email.length > 180) {
      this._errors.push('O email deve ter no máximo 180 caracteres.');
    } else {
      const pattern = /^([\w\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$/;
      if (!pattern.test(user.email)) {
        this._errors.push('O email informado não é válido.');
      }
    }

    if (user.password === undefined || user.password === null) {
      this._errors.push('A senha não pode ser nula.');
    } else if (user.password === '') {
      this._errors.push('A senha não pode ser vazia.');
    } else if (user.password.length < 6) {
      this._errors.push('A senha deve ter no mínimo 6 caracteres.');
    } else if (user.password.length > 80) {
      this._errors.push('A senha deve ter no máximo 80 caracteres.');
    }

    return this._errors.length === 0;
  }
}

export class User {
  private _name: string;
  private _email: string;
  private _password: string;
  private _errors: string[] = [];
  public id: number | null = null;

  constructor(name: string, email: string, password: string) {
    this._name = name;
    this._email = email;
    this._password = password;
    this._validate();
  }

  get name(): string {
    return this._name;
  }

  get email(): string {
    return this._email;
  }

  get password(): string {
    return this._password;
  }

  get isValid(): boolean {
    return this._errors.length === 0;
  }

  get errors(): string[] {
    return this._errors;
  }

  setName(name: string): void {
    this._name = name;
    this._validate();
  }

  setEmail(email: string): void {
    this._email = email;
    this._validate();
  }

  setPassword(password: string): void {
    this._password = password;
    this._validate();
  }

  errorsToString(): string {
    return this._errors.join('; ');
  }

  private _validate(): void {
    const validator = new UserValidator();
    validator.validate(this);
    this._errors = validator.errors;
  }
}