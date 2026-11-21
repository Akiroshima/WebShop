import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Navigation, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthCard } from '../../../shared/components/auth-card/auth-card';
import { PASSWORD_RULES, passwordStrengthValidator, fieldsMatchValidator } from '../../../shared/validators/validators';

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink, AuthCard],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPage {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly resetToken: string;

  readonly form = this.fb.group(
    {
      password: ['', [Validators.required, passwordStrengthValidator]],
      confirm: ['', [Validators.required]],
    },
    { validators: [fieldsMatchValidator('password', 'confirm')] },
  );

  readonly submitting = signal(false);
  readonly serverError = signal('');
  readonly showPassword = signal(false);

  private readonly passwordValue = toSignal(this.form.controls.password.valueChanges, { initialValue: '' });

  readonly rules = computed(() =>
    PASSWORD_RULES.map((rule) => ({ ...rule, passed: rule.test(this.passwordValue() ?? '') })),
  );

  constructor() {
    const navigation = this.router.getCurrentNavigation() as Navigation | null;
    this.resetToken = (navigation?.extras?.state as { resetToken?: string } | undefined)?.resetToken ?? '';
  }

  passwordIssues(): string[] {
    return this.form.controls.password.hasError('passwordStrength')
      ? this.form.controls.password.getError('passwordStrength')
      : [];
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.serverError.set('');

    this.auth.resetPassword(this.resetToken, this.form.controls.password.value ?? '').subscribe({
      next: () => {
        this.notifications.success('Пароль изменён. Войдите с новым паролем', 'Готово');
        void this.router.navigate(['/login']);
      },
      error: (err) => {
        this.serverError.set(NotificationService.messageOf(err));
        this.submitting.set(false);
      },
    });
  }
}
