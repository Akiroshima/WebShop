import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { Checkbox } from 'primeng/checkbox';
import { InputMask } from 'primeng/inputmask';

import { CartService } from '../../../core/services/cart.service';
import { NotificationService } from '../../../core/services/notification.service';
import { OrdersService } from '../../../core/services/orders.service';
import { ProfileService } from '../../../core/services/profile.service';
import { RubPipe } from '../../../shared/pipes/rub.pipe';
import { Address, DELIVERY_OPTIONS, DeliveryMethod } from '../../../shared/models';
import { requiredEmailValidator, requiredPhoneValidator } from '../../../shared/validators/validators';

@Component({
  selector: 'app-checkout-page',
  imports: [FormsModule, ReactiveFormsModule, RouterLink, Checkbox, InputMask, RubPipe],
  templateUrl: './checkout-page.html',
  styleUrl: './checkout-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckoutPage {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly cart = inject(CartService);
  private readonly profileApi = inject(ProfileService);
  private readonly ordersApi = inject(OrdersService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  readonly deliveryOptions = DELIVERY_OPTIONS;
  readonly submitting = signal(false);
  readonly savingAddress = signal(true);
  readonly promoInput = signal('');
  readonly deliveryMethod = signal<DeliveryMethod>('courier');

  readonly form = this.fb.group({
    recipientName: ['', [Validators.required]],
    recipientPhone: ['', [Validators.required, requiredPhoneValidator]],
    recipientEmail: ['', [requiredEmailValidator]],
    city: ['', [Validators.required]],
    postalCode: [''],
    street: ['', [Validators.required]],
    house: ['', [Validators.required]],
    apartment: [''],
    entrance: [''],
    floor: [''],
    intercom: [''],
    comment: [''],
  });

  readonly delivery = computed(() =>
    this.deliveryOptions.find((option) => option.value === this.deliveryMethod()) ?? this.deliveryOptions[0],
  );

  readonly cartState = computed(() => this.cart.cart());

  readonly totals = computed(() => {
    const cartState = this.cartState();
    if (!cartState) {
      return { subtotal: 0, discount: 0, delivery: 0, total: 0 };
    }
    const delivery = this.delivery().price;
    return {
      subtotal: cartState.subtotal,
      discount: cartState.discount,
      delivery,
      total: Math.round((cartState.total + delivery) * 100) / 100,
    };
  });

  constructor() {
    this.cart.load().subscribe();
    this.profileApi.profile().subscribe((profile) => {
      this.form.patchValue({
        recipientName: profile.fullName,
        recipientPhone: profile.phone ?? '',
        recipientEmail: profile.email,
      });
    });
    this.profileApi.addresses().subscribe((addresses) => {
      const address = addresses.find((item) => item.isDefault) ?? addresses[0];
      if (address) {
        this.applyAddress(address);
      }
    });
  }

  private applyAddress(address: Address): void {
    this.form.patchValue({
      city: address.city,
      postalCode: address.postalCode ?? '',
      street: address.street,
      house: address.house,
      apartment: address.apartment ?? '',
      entrance: address.entrance ?? '',
      floor: address.floor ?? '',
      intercom: address.intercom ?? '',
    });
  }

  setDelivery(method: DeliveryMethod): void {
    this.deliveryMethod.set(method);
  }

  applyPromo(): void {
    const code = this.promoInput().trim();
    if (!code) {
      return;
    }
    this.cart.applyPromo(code).subscribe({
      next: () => this.promoInput.set(''),
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
    });
  }

  removePromo(): void {
    this.cart.removePromo().subscribe();
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      this.notifications.warn('Проверьте выделенные поля формы');
      return;
    }
    this.submitting.set(true);
    const value = this.form.getRawValue();

    this.ordersApi
      .checkout({
        recipientName: value.recipientName,
        recipientPhone: value.recipientPhone,
        recipientEmail: value.recipientEmail,
        address: {
          city: value.city,
          postalCode: value.postalCode || null,
          street: value.street,
          house: value.house,
          apartment: value.apartment || null,
          entrance: value.entrance || null,
          floor: value.floor || null,
          intercom: value.intercom || null,
        },
        deliveryMethod: this.deliveryMethod(),
        saveAddress: this.savingAddress(),
        comment: value.comment || null,
      })
      .subscribe({
        next: (result) => {
          this.cart.load().subscribe();
          void this.router.navigate(['/checkout/success', result.number]);
        },
        error: (err) => {
          this.notifications.error(NotificationService.messageOf(err));
          // Конфликт (остатки/промокод изменились) — актуализируем корзину.
          this.cart.load().subscribe();
          this.submitting.set(false);
        },
      });
  }
}
