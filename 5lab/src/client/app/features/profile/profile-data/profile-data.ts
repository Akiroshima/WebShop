import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { InputMask } from 'primeng/inputmask';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ProfileService } from '../../../core/services/profile.service';
import { emailValidator, phoneValidator } from '../../../shared/validators/validators';

@Component({
  selector: 'app-profile-data-page',
  imports: [ReactiveFormsModule, InputMask],
  templateUrl: './profile-data.html',
  styleUrl: './profile-data.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileDataPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly profileApi = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly notifications = inject(NotificationService);

  readonly form = this.fb.group({
    fullName: ['', [Validators.required]],
    email: ['', [Validators.required, emailValidator]],
    phone: ['', [phoneValidator]],
  });

  readonly saving = signal(false);
  readonly serverError = signal('');
  readonly fieldErrors = signal<Record<string, string[]>>({});

  constructor() {
    this.profileApi.profile().subscribe((profile) => {
      this.form.patchValue({
        fullName: profile.fullName,
        email: profile.email,
        phone: profile.phone ?? '',
      });
    });
  }

  save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.serverError.set('');
    this.fieldErrors.set({});

    const value = this.form.getRawValue();
    this.profileApi
      .updateProfile({ fullName: value.fullName.trim(), email: value.email.trim(), phone: value.phone || null })
      .subscribe({
        next: () => {
          this.notifications.success('Контактные данные сохранены');
          // ФИО могло измениться — обновляем пользователя в шапке.
          this.auth.user.update((user) => (user ? { ...user, fullName: value.fullName.trim() } : user));
          this.saving.set(false);
        },
        error: (err) => {
          const anyError = err as any;
          this.fieldErrors.set(anyError?.error?.errors ?? {});
          this.serverError.set(NotificationService.messageOf(err));
          this.saving.set(false);
        },
      });
  }
}
