import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { InputMask } from 'primeng/inputmask';

import { AuthService, RECOVERY_STORAGE_KEY } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthCard } from '../../../shared/components/auth-card/auth-card';
import { RecoveryChannel } from '../../../shared/models';
import { requiredEmailValidator } from '../../../shared/validators/validators';

interface PendingRecovery {
  contact: string;
  channel: RecoveryChannel;
  maskedContact: string;
}

@Component({
  selector: 'app-recovery-contact-page',
  imports: [ReactiveFormsModule, RouterLink, InputMask, AuthCard],
  templateUrl: './recovery.html',
  styleUrl: './recovery.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecoveryContactPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly channel = signal<RecoveryChannel>('email');
  readonly submitting = signal(false);
  readonly serverError = signal('');

  readonly contactControl = new FormControl('', { nonNullable: true });

  private readonly controlValue = toSignal(this.contactControl.valueChanges, { initialValue: '' });

  readonly invalid = computed(() => {
    const value = this.controlValue() ?? '';
    if (this.channel() === 'email') {
      return requiredEmailValidator({ value } as any) !== null;
    }
    // +7 (999) 123-45-67 → 11 цифр
    return value.replace(/\D/g, '').length !== 11;
  });

  selectChannel(channel: RecoveryChannel): void {
    this.channel.set(channel);
    this.contactControl.reset('');
    this.serverError.set('');
  }

  submit(event: Event): void {
    // Форма без formGroup — предотвращаем нативный submit (перезагрузку страницы).
    event.preventDefault();
    const raw = (this.contactControl.value ?? '').trim();
    if (!raw || this.submitting()) {
      return;
    }
    if (this.channel() === 'email') {
      this.contactControl.setValidators([Validators.required, requiredEmailValidator]);
    } else {
      this.contactControl.setValidators([Validators.required]);
    }
    this.contactControl.updateValueAndValidity();
    if (this.contactControl.invalid) {
      this.contactControl.markAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverError.set('');
    this.auth.requestRecoveryCode(raw).subscribe({
      next: (response) => {
        const pending: PendingRecovery = {
          contact: raw,
          channel: response.channel,
          maskedContact: response.maskedContact,
        };
        try {
          sessionStorage.setItem(RECOVERY_STORAGE_KEY, JSON.stringify(pending));
        } catch {
          // Хранилище недоступно — продолжаем через state маршрута.
        }
        void this.router.navigate(['/recovery/sent'], { state: pending as any });
      },
      error: (err) => {
        this.serverError.set(NotificationService.messageOf(err));
        this.submitting.set(false);
      },
    });
  }
}
