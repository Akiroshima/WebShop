import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthCard } from '../../../shared/components/auth-card/auth-card';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, AuthCard],
  templateUrl: './login.html',
  styleUrl: './login.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifications = inject(NotificationService);

  readonly form = this.fb.group({
    login: ['', [Validators.required]],
    password: ['', [Validators.required]],
  });

  readonly submitting = signal(false);
  readonly serverError = signal('');
  readonly showPassword = signal(false);

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.serverError.set('');

    const { login, password } = this.form.getRawValue();
    this.auth.login(login, password).subscribe({
      next: async (user) => {
        this.notifications.success(`${user.fullName}, рады видеть вас и вашего кота!`, 'Вы вошли в аккаунт');
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        await this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : '/catalog');
      },
      error: (err) => {
        this.serverError.set(NotificationService.messageOf(err, 'Не удалось войти. Попробуйте ещё раз'));
        this.submitting.set(false);
      },
    });
  }
}
