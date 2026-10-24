import { ChangeDetectionStrategy, Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Navigation, Router, RouterLink } from '@angular/router';
import { Subscription, interval } from 'rxjs';

import { AuthService, RECOVERY_STORAGE_KEY } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthCard } from '../../../shared/components/auth-card/auth-card';
import { RecoveryChannel } from '../../../shared/models';

interface PendingRecovery {
  contact: string;
  channel: RecoveryChannel;
  maskedContact: string;
}

@Component({
  selector: 'app-recovery-sent-page',
  imports: [FormsModule, RouterLink, AuthCard],
  templateUrl: './recovery-sent.html',
  styleUrl: './recovery-sent.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecoverySentPage implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  private readonly timerSubscription: Subscription;

  readonly pending: PendingRecovery;

  readonly code = signal('');
  readonly submitting = signal(false);
  readonly serverError = signal('');
  readonly resendIn = signal(60);

  constructor() {
    const navigation = this.router.getCurrentNavigation() as Navigation | null;
    const state = navigation?.extras?.state as PendingRecovery | undefined;
    this.pending = state ?? this.readFromStorage() ?? { contact: '', channel: 'email', maskedContact: 'указанный контакт' };
    if (!state) {
      this.readFromStorage();
    }
    this.timerSubscription = interval(1000).subscribe(() => {
      if (this.resendIn() > 0) {
        this.resendIn.update((value) => value - 1);
      }
    });
  }

  private readFromStorage(): PendingRecovery | null {
    try {
      const raw = sessionStorage.getItem(RECOVERY_STORAGE_KEY);
      return raw ? (JSON.parse(raw) as PendingRecovery) : null;
    } catch {
      return null;
    }
  }

  get isEmail(): boolean {
    return this.pending.channel === 'email';
  }

  codeValid(): boolean {
    return /^\d{6}$/.test(this.code().trim());
  }

  confirm(event: Event): void {
    // Форма без formGroup — предотвращаем нативный submit (перезагрузку страницы).
    event.preventDefault();
    if (!this.codeValid() || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.serverError.set('');
    this.auth.confirmRecoveryCode(this.pending.contact, this.code().trim()).subscribe({
      next: (result) => {
        void this.router.navigate(['/reset-password'], { state: { resetToken: result.resetToken } });
      },
      error: (err) => {
        this.serverError.set(NotificationService.messageOf(err, 'Код восстановления не подходит'));
        this.submitting.set(false);
      },
    });
  }

  resend(): void {
    if (this.resendIn() > 0) {
      return;
    }
    this.auth.requestRecoveryCode(this.pending.contact).subscribe({
      next: (response) => {
        this.pending.maskedContact = response.maskedContact;
        this.resendIn.set(60);
        this.notifications.info('Новый код отправлен. Он также выведен в консоль браузера');
      },
      error: (err) => this.serverError.set(NotificationService.messageOf(err)),
    });
  }

  ngOnDestroy(): void {
    this.timerSubscription.unsubscribe();
  }
}
