import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Окно номеров страниц вокруг текущей (как в каталоге). */
export function pageWindow(total: number, current: number, window = 7): number[] {
  let start = Math.max(1, current - Math.floor(window / 2));
  const end = Math.min(total, start + window - 1);
  start = Math.max(1, end - window + 1);
  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
}

/** Серверная пагинация: «Показано X–Y из Z» + номера страниц и стрелки. */
@Component({
  selector: 'app-pager',
  templateUrl: './pager.html',
  styleUrl: './pager.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pager {
  /** Текущая страница (1-based). */
  readonly page = input.required<number>();

  /** Всего страниц. */
  readonly totalPages = input.required<number>();

  /** Подпись «Показано X–Y из Z». */
  readonly range = input.required<string>();

  readonly pageChange = output<number>();

  readonly pages = computed(() => pageWindow(this.totalPages(), this.page()));

  go(next: number): void {
    if (next >= 1 && next <= this.totalPages() && next !== this.page()) {
      this.pageChange.emit(next);
    }
  }
}
