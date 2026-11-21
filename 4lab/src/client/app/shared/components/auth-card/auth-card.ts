import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Общая карточка экранов входа/восстановления: зелёная панель + форма. */
@Component({
  selector: 'app-auth-card',
  templateUrl: './auth-card.html',
  styleUrl: './auth-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthCard {
  readonly overline = input('');
  readonly title = input('');
  readonly subtitle = input('');
}
