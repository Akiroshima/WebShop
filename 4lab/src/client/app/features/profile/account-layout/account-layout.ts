import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-account-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './account-layout.html',
  styleUrl: './account-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountLayout {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly menu = [
    { path: '/account', label: 'Мои данные', icon: 'pi pi-user', exact: true },
    { path: '/account/addresses', label: 'Адреса доставки', icon: 'pi pi-map-marker', exact: false },
    { path: '/account/orders', label: 'Мои заказы', icon: 'pi pi-file', exact: false },
    { path: '/account/security', label: 'Безопасность', icon: 'pi pi-shield', exact: false },
  ];

  readonly initials = computed(() => {
    const name = this.auth.user()?.fullName ?? '';
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');
  });

  logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }
}
