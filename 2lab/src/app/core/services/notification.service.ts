import { Injectable, inject } from '@angular/core';

import { MessageService } from 'primeng/api';

/** Единая точка уведомлений: тосты top-right (десктоп), закрываются по таймауту 5 с. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly messages = inject(MessageService);

  success(detail: string, summary = 'Успех'): void {
    this.messages.add({ severity: 'success', summary, detail, life: 5000 });
  }

  error(detail: string, summary = 'Ошибка'): void {
    this.messages.add({ severity: 'error', summary, detail, life: 5000 });
  }

  info(detail: string, summary = 'Информация'): void {
    this.messages.add({ severity: 'info', summary, detail, life: 5000 });
  }

  warn(detail: string, summary = 'Внимание'): void {
    this.messages.add({ severity: 'warn', summary, detail, life: 5000 });
  }

  /** Извлекает сообщение из HttpErrorResponse мок-слоя. */
  static messageOf(error: unknown, fallback = 'Что-то пошло не так. Попробуйте ещё раз'): string {
    const anyError = error as any;
    return anyError?.error?.message ?? fallback;
  }
}
