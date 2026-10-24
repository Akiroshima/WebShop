/** Ошибка мок-сервера: статус + сообщение (+ ошибки полей для 400). */
export class MockHttpError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'MockHttpError';
  }

  static badRequest(message = 'Данные заполнены неверно', fieldErrors?: Record<string, string[]>): MockHttpError {
    return new MockHttpError(400, message, fieldErrors);
  }

  static unauthorized(message = 'Требуется вход в аккаунт'): MockHttpError {
    return new MockHttpError(401, message);
  }

  static forbidden(message = 'Доступ запрещён'): MockHttpError {
    return new MockHttpError(403, message);
  }

  static notFound(message = 'Не найдено'): MockHttpError {
    return new MockHttpError(404, message);
  }

  static conflict(message: string): MockHttpError {
    return new MockHttpError(409, message);
  }

  static tooManyRequests(message: string): MockHttpError {
    return new MockHttpError(429, message);
  }

  static server(message = 'Внутренняя ошибка сервера. Попробуйте позже'): MockHttpError {
    return new MockHttpError(500, message);
  }
}
