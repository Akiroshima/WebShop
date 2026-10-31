import { Product } from './catalog';

export interface CartItem {
  productId: string;
  name: string;
  sku: string;
  brand: string | null;
  price: number;
  stock: number;
  imageUrl: string;
  quantity: number;
  subtotal: number;
}

export type PromoType = 'percent' | 'fixed';

export interface AppliedPromo {
  code: string;
  type: PromoType;
  value: number;
  discount: number;
}

export interface CartState {
  items: CartItem[];
  itemsCount: number;
  subtotal: number;
  promo: AppliedPromo | null;
  discount: number;
  total: number;
}

export interface CartMutationResult {
  cart: CartState;
  notice?: string;
}

/** Внутреннее представление корзины в мок-БД. */
export interface DbCart {
  userId: string;
  promoCode: string | null;
  items: { productId: string; quantity: number; addedAt: string }[];
}

export interface Promo {
  id: string;
  code: string;
  type: PromoType;
  value: number;
  minSubtotal: number;
  startsAt: string;
  endsAt: string;
  maxUses: number | null;
  usedCount: number;
  isActive: boolean;
}

/** Позиция заказа — снимок товара на момент оформления. */
export interface OrderItem {
  productId: string | null;
  name: string;
  sku: string;
  price: number;
  quantity: number;
}

export type OrderStatus = 'new' | 'confirmed' | 'delivered' | 'cancelled';

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  new: 'Новый',
  confirmed: 'Подтверждён',
  delivered: 'Доставлен',
  cancelled: 'Отменён',
};

export type DeliveryMethod = 'courier' | 'pickup';

export const DELIVERY_OPTIONS: { value: DeliveryMethod; label: string; description: string; price: number }[] = [
  { value: 'courier', label: 'Курьером', description: 'Завтра, 10:00–22:00', price: 290 },
  { value: 'pickup', label: 'Пункт выдачи', description: '2–3 дня, 12 пунктов рядом', price: 190 },
];

export interface OrderAddress {
  city: string;
  postalCode: string | null;
  street: string;
  house: string;
  apartment: string | null;
  entrance: string | null;
  floor: string | null;
  intercom: string | null;
}

export interface Order {
  id: string;
  number: number;
  userId: string;
  status: OrderStatus;
  createdAt: string;
  recipientName: string;
  recipientPhone: string;
  recipientEmail: string | null;
  address: OrderAddress;
  comment: string | null;
  promoCode: string | null;
  subtotal: number;
  discount: number;
  deliveryMethod: DeliveryMethod;
  delivery: number;
  total: number;
  items: OrderItem[];
}

export interface CheckoutPayload {
  recipientName: string;
  recipientPhone: string;
  recipientEmail: string | null;
  address: OrderAddress;
  deliveryMethod: DeliveryMethod;
  saveAddress: boolean;
  comment: string | null;
}

export interface CheckoutResult {
  id: string;
  number: number;
}

/** Product используется в headless-тестах и типизации карточек. */
export type { Product };
