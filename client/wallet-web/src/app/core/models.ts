export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
}

export interface CardInfo {
  holderName: string;
  maskedNumber: string;
  expiry: string;
  isFrozen: boolean;
}

export interface WalletHome {
  spendingBalance: number;
  savingsBalance: number;
  currency: string;
  salaryAmount: number;
  canCollectSalary: boolean;
  incomingRequests: number;
  card: CardInfo | null;
}

export interface MoneyMovement {
  spendingBalance: number;
  savingsBalance: number;
  currency: string;
  referenceId: string;
}

export interface SalaryState {
  salaryAmount: number;
  canCollectSalary: boolean;
}

export interface Bill {
  code: string;
  name: string;
}

export interface MoneyRequestItem {
  id: string;
  direction: 'incoming' | 'outgoing' | string;
  status: string;
  amount: number;
  note: string | null;
  counterpartyName: string;
  counterpartyEmail: string;
  createdAt: string;
}

export interface Transaction {
  id: string;
  type: string;
  pocket: 'Spending' | 'Savings' | string;
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
    case 'Salary':
      return 'راتب';
    case 'BillPayment':
      return 'فاتورة';
    case 'CardPayment':
      return 'بطاقة';
    case 'MoveToSavings':
      return 'نقل للتوفير';
    case 'MoveToSpending':
      return 'نقل للصرف';
    default:
      return type;
  }
}

export function pocketLabel(pocket: string): string {
  return pocket === 'Savings' ? 'توفير' : 'صرف';
}

export function requestStatusLabel(status: string): string {
  switch (status) {
    case 'Pending':
      return 'بانتظار';
    case 'Accepted':
      return 'مقبولة';
    case 'Rejected':
      return 'مرفوضة';
    case 'Cancelled':
      return 'ملغاة';
    default:
      return status;
  }
}

export function isOutgoing(type: string, pocket: string): boolean {
  if (type === 'TransferOut' || type === 'BillPayment' || type === 'CardPayment') return true;
  if (type === 'MoveToSavings' && pocket === 'Spending') return true;
  if (type === 'MoveToSpending' && pocket === 'Savings') return true;
  return false;
}

export function errorMessage(error: unknown, fallback: string): string {
  if (typeof error === 'object' && error && 'error' in error) {
    const body = (error as { error?: { message?: string } }).error;
    if (body?.message) return body.message;
  }
  return fallback;
}
