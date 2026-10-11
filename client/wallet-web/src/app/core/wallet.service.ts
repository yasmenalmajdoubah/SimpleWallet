import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MoneyMovement, Paged, Transaction, WalletBalance } from './models';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly http = inject(HttpClient);

  balance() {
    return this.http.get<WalletBalance>('/api/wallet');
  }

  transactions(page = 1, pageSize = 20) {
    return this.http.get<Paged<Transaction>>('/api/wallet/transactions', {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }

  deposit(amount: number, note: string) {
    return this.http.post<MoneyMovement>('/api/wallet/deposit', {
      amount,
      note: note.trim() || null,
    });
  }

  transfer(toEmail: string, amount: number, note: string) {
    return this.http.post<MoneyMovement>('/api/wallet/transfer', {
      toEmail,
      amount,
      note: note.trim() || null,
    });
  }
}
