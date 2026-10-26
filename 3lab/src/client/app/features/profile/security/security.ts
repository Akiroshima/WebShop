import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { NotificationService } from '../../../core/services/notification.service';
import { ProfileService } from '../../../core/services/profile.service';
import { PASSWORD_RULES, fieldsMatchValidator, passwordStrengthValidator } from '../../../shared/validators/validators';

@Component({
  selector: 'app-security-page',
  imports: [ReactiveFormsModule],
  templateUrl: './security.html',
  styleUrl: './security.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SecurityPage {
  private readonly fb = inject(FormBuilder);
  private readonly profileApi = inject(ProfileService);
  private readonly notifications = inject(NotificationService);

  readonly form = this.fb.group(
    {
      currentPassword: ['', [Validators.required]],
      password: ['', [Validators.required, passwordStrengthValidator]],
      confirm: ['', [Validators.required]],
    },
    { validators: [fieldsMatchValidator('password', 'confirm')] },
  );

  readonly saving = signal(false);
  readonly serverError = signal('');

  private readonly passwordValue = toSignal(this.form.controls.password.valueChanges, { initialValue: '' });

  readonly rules = computed(() =>
    PASSWORD_RULES.map((rule) => ({ ...rule, passed: rule.test(this.passwordValue() ?? '') })),
  );

  submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.serverError.set('');

    this.profileApi
      .changePassword(this.form.controls.currentPassword.value ?? '', this.form.controls.password.value ?? '')
      .subscribe({
        next: () => {
          this.notifications.success('Пароль изменён. В следующий раз входите с новым паролем');
          this.form.reset();
          this.saving.set(false);
        },
        error: (err) => {
          this.serverError.set(NotificationService.messageOf(err));
          this.saving.set(false);
        },
      });
  }
}
