import { FormControl, FormGroup } from '@angular/forms';

import {
  emailValidator,
  fieldsMatchValidator,
  isValidPhone,
  normalizePhone,
  passwordIssues,
  passwordStrengthValidator,
  phoneValidator,
  priceRangeValidator,
  requiredEmailValidator,
} from './validators';

describe('Validators', () => {
  describe('normalizePhone / isValidPhone', () => {
    it('приводит 8-… к +7…', () => {
      expect(normalizePhone('8 999 123-45-67')).toBe('+79991234567');
    });

    it('приводит 7… к +7…', () => {
      expect(normalizePhone('79991234567')).toBe('+79991234567');
    });

    it('плюс сохраняется', () => {
      expect(normalizePhone('+7 (999) 123-45-67')).toBe('+79991234567');
    });

    it('пустая строка остаётся пустой', () => {
      expect(normalizePhone('')).toBe('');
    });

    it('валидирует E.164', () => {
      expect(isValidPhone('+79991234567')).toBeTrue();
      expect(isValidPhone('8999123456')).toBeFalse();
      expect(isValidPhone('+799912345678')).toBeFalse();
    });
  });

  describe('emailValidator', () => {
    it('принимает корректный email', () => {
      expect(emailValidator(new FormControl('user@example.com'))).toBeNull();
    });

    it('отклоняет мусор', () => {
      expect(emailValidator(new FormControl('user@example'))).toEqual({ email: 'Введите корректный email' });
    });

    it('пустое значение валидно (поле необязательное)', () => {
      expect(emailValidator(new FormControl(''))).toBeNull();
    });

    it('requiredEmailValidator требует заполнения', () => {
      expect(requiredEmailValidator(new FormControl(''))).toEqual({ email: 'Введите email' });
    });
  });

  describe('phoneValidator', () => {
    it('пустое значение валидно (телефон необязателен)', () => {
      expect(phoneValidator(new FormControl(''))).toBeNull();
    });

    it('отклоняет неполный номер', () => {
      expect(phoneValidator(new FormControl('+7 (999) 123-45-6'))).toEqual({
        phone: 'Введите корректный номер телефона',
      });
    });
  });

  describe('passwordIssues', () => {
    it('собирает все нарушения', () => {
      expect(passwordIssues('abc')).toEqual(['минимум 8 символов', 'хотя бы одна цифра', 'хотя бы один спецзнак']);
    });

    it('сложный пароль без нарушений', () => {
      expect(passwordIssues('Kот2026!')).toEqual([]);
    });

    it('валидатор возвращает ошибки для слабого пароля', () => {
      const errors = passwordStrengthValidator(new FormControl('12345678'));
      expect(errors).withContext('только цифры: нет буквы и спецзнака').not.toBeNull();
    });
  });

  describe('fieldsMatchValidator', () => {
    const group = () =>
      new FormGroup({
        password: new FormControl('qwerty1!'),
        confirm: new FormControl(''),
      });

    it('нет ошибки, пока второе поле пустое', () => {
      expect(fieldsMatchValidator('password', 'confirm')(group())).toBeNull();
    });

    it('ошибка при несовпадении', () => {
      const form = group();
      form.get('confirm')!.setValue('другое');
      expect(fieldsMatchValidator('password', 'confirm')(form)).toEqual({ fieldsMismatch: true });
    });
  });

  describe('priceRangeValidator', () => {
    it('«от» больше «до» — ошибка', () => {
      const form = new FormGroup({
        priceMin: new FormControl(5000),
        priceMax: new FormControl(1000),
      });
      expect(priceRangeValidator('priceMin', 'priceMax')(form)).toEqual({ priceRange: true });
    });

    it('нормальный диапазон проходит', () => {
      const form = new FormGroup({
        priceMin: new FormControl(100),
        priceMax: new FormControl(5000),
      });
      expect(priceRangeValidator('priceMin', 'priceMax')(form)).toBeNull();
    });

    it('частично заполненный диапазон проходит', () => {
      const form = new FormGroup({
        priceMin: new FormControl(100),
        priceMax: new FormControl(0),
      });
      expect(priceRangeValidator('priceMin', 'priceMax')(form)).toBeNull();
    });
  });
});
