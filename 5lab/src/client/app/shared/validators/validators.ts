import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;
export const PHONE_E164_PATTERN = /^\+7\d{10}$/;

/** Приводит ввод к E.164: «8 999 123-45-67» и «+7 (999) 123-45-67» → «+79991234567». */
export function normalizePhone(value: string): string {
  const digits = (value ?? '').replace(/\D/g, '');
  if (!digits) {
    return '';
  }
  if (digits.length === 11 && digits.startsWith('8')) {
    return `+7${digits.slice(1)}`;
  }
  if (digits.startsWith('7')) {
    return `+${digits}`;
  }
  return `+${digits}`;
}

export function isValidPhone(value: string): boolean {
  return PHONE_E164_PATTERN.test(normalizePhone(value));
}

export function emailValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '').trim();
  if (!value) {
    return null;
  }
  return EMAIL_PATTERN.test(value) ? null : { email: 'Введите корректный email' };
}

export function requiredEmailValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '').trim();
  if (!value) {
    return { email: 'Введите email' };
  }
  return EMAIL_PATTERN.test(value) ? null : { email: 'Введите корректный email' };
}

export function phoneValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value ?? '';
  if (!value) {
    return null; // телефон необязателен
  }
  return isValidPhone(value) ? null : { phone: 'Введите корректный номер телефона' };
}

export function requiredPhoneValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value ?? '';
  if (!value) {
    return { phone: 'Введите номер телефона' };
  }
  return isValidPhone(value) ? null : { phone: 'Введите корректный номер телефона' };
}

export interface PasswordRule {
  key: string;
  label: string;
  test: (value: string) => boolean;
}

export const PASSWORD_RULES: PasswordRule[] = [
  { key: 'length', label: 'Минимум 8 символов', test: (v) => v.length >= 8 },
  { key: 'digit', label: 'Хотя бы одна цифра', test: (v) => /[0-9]/.test(v) },
  { key: 'letter', label: 'Хотя бы одна буква', test: (v) => /[A-Za-zА-Яа-яЁё]/.test(v) },
  { key: 'special', label: 'Хотя бы один спецзнак (!, @, #, %…)', test: (v) => /[^A-Za-zА-Яа-яЁё0-9]/.test(v) },
];

export function passwordIssues(value: string): string[] {
  const issues: string[] = [];
  if (value.length < 8) {
    issues.push('минимум 8 символов');
  }
  if (!/[0-9]/.test(value)) {
    issues.push('хотя бы одна цифра');
  }
  if (!/[A-Za-zА-Яа-яЁё]/.test(value)) {
    issues.push('хотя бы одна буква');
  }
  if (!/[^A-Za-zА-Яа-яЁё0-9]/.test(value)) {
    issues.push('хотя бы один спецзнак');
  }
  return issues;
}

export function passwordStrengthValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value ?? '') as string;
  if (!value) {
    return null;
  }
  const issues = passwordIssues(value);
  return issues.length ? { passwordStrength: issues } : null;
}

/** Проверяет совпадение двух полей группы (например, пароль и повтор). */
export function fieldsMatchValidator(firstField: string, secondField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const first = group.get(firstField);
    const second = group.get(secondField);
    if (!first || !second || !first.value || !second.value) {
      return null;
    }
    return first.value === second.value ? null : { fieldsMismatch: true };
  };
}

/** «От» не больше «До» — для фильтра цены. */
export function priceRangeValidator(minField: string, maxField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const min = Number(group.get(minField)?.value);
    const max = Number(group.get(maxField)?.value);
    if (Number.isFinite(min) && Number.isFinite(max) && min > 0 && max > 0 && min > max) {
      return { priceRange: true };
    }
    return null;
  };
}
