export class ValidationError extends Error {
    constructor(
        public override message: string,
        public statusCode: number = 403,
    ) {
        super(message);
        this.name = "ValidationError";
    }
}

export class RepositoryError extends Error {
    constructor(
        public override message: string,
        public statusCode: number = 500,
    ) {
        super(message);
        this.name = "RepositoryError";
    }
}

export class AuthenticationError extends Error {
    constructor(
        public override message: string,
        public statusCode: number = 401,
    ) {
        super(message);
        this.name = "AuthenticationError";
    }
}

export class CheatingAttemptError extends Error {
    constructor(
        public override message: string,
        public statusCode: number = 403,
        public cheatDetails?: string,
    ) {
        super(message);
        this.name = "CheatingAttemptError";
    }
}
