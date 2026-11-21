export interface Address {
  id: string;
  userId: string;
  label: string | null;
  recipient: string;
  phone: string;
  city: string;
  street: string;
  house: string;
  apartment: string | null;
  entrance: string | null;
  floor: string | null;
  intercom: string | null;
  postalCode: string | null;
  isDefault: boolean;
}

export interface AddressDraft {
  label: string | null;
  recipient: string;
  phone: string;
  city: string;
  street: string;
  house: string;
  apartment: string | null;
  entrance: string | null;
  floor: string | null;
  intercom: string | null;
  postalCode: string | null;
  isDefault: boolean;
}
