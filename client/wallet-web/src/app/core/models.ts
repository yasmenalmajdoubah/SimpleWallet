export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
}

export interface WalletBalance {
  balance: number;
  currency: string;
}

export interface MoneyMovement extends WalletBalance {
  referenceId: string;
}

export interface Transaction {
  id: string;
  type: 'Deposit' | 'TransferIn' | 'TransferOut' | string;
  amount: number;
  balanceAfter: number;
  counterpartyEmail: string | null;
  note: string | null;
  createdAt: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export function money(value: number): string {
  return new Intl.NumberFormat('ar-JO', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

export function when(value: string): string {
  return new Intl.DateTimeFormat('ar', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}

export function movementLabel(type: string): string {
  switch (type) {
    case 'Deposit':
      return 'شحن';
    case 'TransferIn':
      return 'تحويل وارد';
    case 'TransferOut':
      return 'تحويل صادر';
    default:
      return type;
  }
}

export function errorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error && 'error' in error) {
    const body = (error as { error?: { message?: string } }).error;
    if (body?.message) return body.message;
  }
  return fallback;
}
