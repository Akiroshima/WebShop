import { ChangeDetectionStrategy, Component, HostListener, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-header',
  imports: [FormsModule, RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppHeader {
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  private readonly router = inject(Router);

  readonly menuOpen = signal(false);
  readonly searchValue = signal('');

  readonly navLinks = [
    { label: 'Каталог', categoryId: null },
    { label: 'Корм и лакомства', categoryId: 'c-food' },
    { label: 'Наполнители', categoryId: 'c-litter' },
    { label: 'Игрушки', categoryId: 'c-toys' },
    { label: 'Уход и гигиена', categoryId: 'c-care' },
    { label: 'Домики и лежанки', categoryId: 'c-home' },
  ];

  toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  submitSearch(event: Event): void {
    // Нативная форма без директив Angular — отменяем перезагрузку страницы.
    event.preventDefault();
    const query = this.searchValue().trim();
    this.router.navigate(['/catalog'], { queryParams: { search: query || null } });
  }

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (this.menuOpen() && !target.closest('.mzk-account')) {
      this.menuOpen.set(false);
    }
  }
}
