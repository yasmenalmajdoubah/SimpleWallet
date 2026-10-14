import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import {
  Bill,
  CardInfo,
  MoneyMovement,
  MoneyRequestItem,
  Paged,
  SalaryState,
  Transaction,
  WalletHome,
} from './models';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly http = inject(HttpClient);

  home() {
    return this.http.get<WalletHome>('/api/wallet');
  }

  transactions(page = 1, pageSize = 20) {
    return this.http.get<Paged<Transaction>>('/api/wallet/transactions', {
      params: { page: String(page), pageSize: String(pageSize) },
    });
  }

  bills() {
    return this.http.get<Bill[]>('/api/wallet/bills');
  }

  requests() {
    return this.http.get<MoneyRequestItem[]>('/api/wallet/requests');
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

  setSalary(amount: number) {
    return this.http.put<SalaryState>('/api/wallet/salary', { amount });
  }

  collectSalary() {
    return this.http.post<MoneyMovement>('/api/wallet/salary/collect', {});
  }

  move(amount: number, toSavings: boolean) {
    return this.http.post<MoneyMovement>('/api/wallet/move', { amount, toSavings });
  }

  freezeCard(frozen: boolean) {
    return this.http.post<CardInfo>('/api/wallet/card/freeze', { frozen });
  }

  payWithCard(amount: number, merchant: string) {
    return this.http.post<MoneyMovement>('/api/wallet/card/pay', { amount, merchant });
  }

  payBill(code: string, amount: number, reference: string) {
    return this.http.post<MoneyMovement>('/api/wallet/bills/pay', { code, amount, reference });
  }

  createRequest(payerEmail: string, amount: number, note: string) {
    return this.http.post('/api/wallet/requests', {
      payerEmail,
      amount,
      note: note.trim() || null,
    });
  }

  acceptRequest(id: string) {
    return this.http.post<MoneyMovement>(`/api/wallet/requests/${id}/accept`, {});
  }

  rejectRequest(id: string) {
    return this.http.post(`/api/wallet/requests/${id}/reject`, {});
  }

  cancelRequest(id: string) {
    return this.http.post(`/api/wallet/requests/${id}/cancel`, {});
  }
}
