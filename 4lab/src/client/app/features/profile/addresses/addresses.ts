import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Checkbox } from 'primeng/checkbox';
import { ConfirmationService } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { InputMask } from 'primeng/inputmask';

import { NotificationService } from '../../../core/services/notification.service';
import { ProfileService } from '../../../core/services/profile.service';
import { Address } from '../../../shared/models';

interface AddressFormValue {
  label: string;
  recipient: string;
  phone: string;
  city: string;
  street: string;
  house: string;
  apartment: string;
  entrance: string;
  floor: string;
  intercom: string;
  postalCode: string;
  isDefault: boolean;
}

const emptyForm = (): AddressFormValue => ({
  label: '',
  recipient: '',
  phone: '',
  city: '',
  street: '',
  house: '',
  apartment: '',
  entrance: '',
  floor: '',
  intercom: '',
  postalCode: '',
  isDefault: false,
});

@Component({
  selector: 'app-addresses-page',
  imports: [FormsModule, Checkbox, Dialog, InputMask],
  providers: [ConfirmationService],
  templateUrl: './addresses.html',
  styleUrl: './addresses.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AddressesPage {
  private readonly profileApi = inject(ProfileService);
  private readonly notifications = inject(NotificationService);
  private readonly confirmations = inject(ConfirmationService);

  readonly addresses = signal<Address[]>([]);
  readonly loading = signal(true);

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly form = signal<AddressFormValue>(emptyForm());
  readonly errors = signal<Record<string, string[]>>({});

  constructor() {
    this.reload();
  }

  private reload(): void {
    this.profileApi.addresses().subscribe({
      next: (addresses) => {
        this.addresses.set(addresses);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }

  openCreate(): void {
    const first = this.addresses()[0];
    this.editingId.set(null);
    this.errors.set({});
    this.form.set({
      ...emptyForm(),
      recipient: this.addresses()[0]?.recipient ?? first?.recipient ?? '',
      phone: '',
    });
    this.dialogVisible.set(true);
  }

  openEdit(address: Address): void {
    this.editingId.set(address.id);
    this.errors.set({});
    this.form.set({
      label: address.label ?? '',
      recipient: address.recipient,
      phone: address.phone,
      city: address.city,
      street: address.street,
      house: address.house,
      apartment: address.apartment ?? '',
      entrance: address.entrance ?? '',
      floor: address.floor ?? '',
      intercom: address.intercom ?? '',
      postalCode: address.postalCode ?? '',
      isDefault: address.isDefault,
    });
    this.dialogVisible.set(true);
  }

  update<K extends keyof AddressFormValue>(key: K, value: AddressFormValue[K]): void {
    this.form.update((form) => ({ ...form, [key]: value }));
  }

  save(): void {
    const value = this.form();
    if (!value.recipient.trim() || !value.city.trim() || !value.street.trim() || !value.house.trim() || !value.phone) {
      this.notifications.warn('Заполните получателя, телефон, город, улицу и дом');
      return;
    }
    this.saving.set(true);
    this.errors.set({});

    const draft = {
      label: value.label.trim() || null,
      recipient: value.recipient.trim(),
      phone: value.phone,
      city: value.city.trim(),
      street: value.street.trim(),
      house: value.house.trim(),
      apartment: value.apartment.trim() || null,
      entrance: value.entrance.trim() || null,
      floor: value.floor.trim() || null,
      intercom: value.intercom.trim() || null,
      postalCode: value.postalCode.trim() || null,
      isDefault: value.isDefault,
    };

    const request$ = this.editingId()
      ? this.profileApi.updateAddress(this.editingId()!, draft)
      : this.profileApi.createAddress(draft);

    request$.subscribe({
      next: (address) => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notifications.success(this.editingId() ? 'Адрес обновлён' : `Адрес «${address.label ?? 'Новый адрес'}» сохранён`);
        this.reload();
      },
      error: (err) => {
        const anyError = err as any;
        this.errors.set(anyError?.error?.errors ?? {});
        this.saving.set(false);
        this.notifications.error(NotificationService.messageOf(err));
      },
    });
  }

  makeDefault(address: Address): void {
    this.profileApi.updateAddress(address.id, { ...address, isDefault: true }).subscribe({
      next: () => {
        this.notifications.success('Адрес «по умолчанию» обновлён');
        this.reload();
      },
      error: (err) => this.notifications.error(NotificationService.messageOf(err)),
    });
  }

  askDelete(address: Address): void {
    this.confirmations.confirm({
      header: 'Удалить адрес?',
      message: `Адрес «${address.label ?? address.city + ', ' + address.street}» будет удалён из кабинета. На оформленные заказы это не повлияет.`,
      acceptLabel: 'Удалить',
      rejectLabel: 'Отмена',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.profileApi.deleteAddress(address.id).subscribe({
          next: () => {
            this.notifications.info('Адрес удалён');
            this.reload();
          },
          error: (err) => this.notifications.error(NotificationService.messageOf(err)),
        });
      },
    });
  }

  addressLine(address: Address): string {
    return [
      `${address.city}${address.postalCode ? ', ' + address.postalCode : ''}`,
      `${address.street}, д. ${address.house}`,
      address.apartment ? `кв. ${address.apartment}` : null,
      address.entrance ? `подъезд ${address.entrance}` : null,
      address.floor ? `этаж ${address.floor}` : null,
      address.intercom ? `домофон ${address.intercom}` : null,
    ]
      .filter(Boolean)
      .join(', ');
  }
}
