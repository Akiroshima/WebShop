import { Pipe, PipeTransform } from '@angular/core';

/** Дата в формате дд.мм.гггг. */
@Pipe({ name: 'ruDate' })
export class RuDatePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }
    const date = typeof value === 'string' ? new Date(value) : value;
    if (Number.isNaN(date.getTime())) {
      return '';
    }
    return date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }
}
