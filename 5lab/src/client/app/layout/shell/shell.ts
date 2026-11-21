import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';
import { AppFooter } from '../footer/footer';
import { AppHeader } from '../header/header';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, AppHeader, AppFooter],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);

  constructor() {
    // Оболочка создаётся после authGuard — пользователь уже известен.
    // Тянем корзину один раз за сессию: бейдж в шапке и страница /cart.
    if (this.auth.user()) {
      this.cart.load().subscribe();
    }
  }
}
