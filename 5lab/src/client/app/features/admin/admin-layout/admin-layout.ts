import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminLayout {
  readonly tabs = [
    { path: '/admin/products', label: 'Товары', icon: 'pi pi-box' },
    { path: '/admin/orders', label: 'Заказы', icon: 'pi pi-file' },
    { path: '/admin/users', label: 'Пользователи', icon: 'pi pi-users' },
  ];
}
