import { Pipe, PipeTransform } from '@angular/core';

/** Форматирует сумму в рублях: 12345.5 → «12 345,5 ₽» (неразрывные пробелы). */
@Pipe({ name: 'rub' })
export class RubPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value === null || value === undefined || Number.isNaN(value)) {
      return '';
    }
    const fractionDigits = Number.isInteger(value) ? 0 : 2;
    const formatted = new Intl.NumberFormat('ru-RU', {
      minimumFractionDigits: fractionDigits,
      maximumFractionDigits: fractionDigits,
    }).format(value);
    return `${formatted} ₽`;
  }
}
