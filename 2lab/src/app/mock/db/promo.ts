import { Promo } from '../../shared/models';

/** Округление до копеек. */
export function round2(value: number): number {
  return Math.round(value * 100) / 100;
}

/** Скидка промокода от суммы товаров (не больше суммы товаров). */
export function promoDiscount(promo: Promo, subtotal: number): number {
  const raw = promo.type === 'percent' ? (subtotal * promo.value) / 100 : promo.value;
  return round2(Math.min(round2(raw), subtotal));
}

/** Проверяет применимость промокода; возвращает текст ошибки или null. */
export function promoErrorMessage(promo: Promo): string | null {
  if (!promo.isActive) {
    return 'Промокод не найден или не действует';
  }
  const now = Date.now();
  if (new Date(promo.startsAt).getTime() > now) {
    return 'Промокод не найден или не действует';
  }
  if (new Date(promo.endsAt).getTime() < now) {
    return 'Промокод не найден или не действует';
  }
  if (promo.maxUses !== null && promo.usedCount >= promo.maxUses) {
    return 'Промокод не найден или не действует';
  }
  return null;
}

export function findPromoByCode(dbPromos: Promo[], code: string): Promo | undefined {
  const normalized = (code ?? '').trim().toUpperCase();
  return dbPromos.find((promo) => promo.code === normalized);
}
