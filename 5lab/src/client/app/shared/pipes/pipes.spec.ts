import { RubPipe } from './rub.pipe';
import { RuDatePipe } from './ru-date.pipe';

describe('RubPipe', () => {
  const pipe = new RubPipe();
  // ru-RU использует неразрывные пробелы (U+00A0) как разделители разрядов.
  const nbsp = '\u00A0';

  it('форматирует целые суммы с пробелами', () => {
    expect(pipe.transform(2290)).toBe(`2${nbsp}290 ₽`);
  });

  it('форматирует копейки', () => {
    expect(pipe.transform(1234.5)).toBe(`1${nbsp}234,50 ₽`);
  });

  it('пустые значения дают пустую строку', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
  });
});

describe('RuDatePipe', () => {
  const pipe = new RuDatePipe();

  it('форматирует дату как дд.мм.гггг', () => {
    expect(pipe.transform('2026-09-18T12:30:00.000Z')).toMatch(/^\d{2}\.\d{2}\.2026$/);
  });

  it('пустое значение — пустая строка', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform('')).toBe('');
  });
});
