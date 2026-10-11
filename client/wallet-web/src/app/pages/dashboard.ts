import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { WalletService } from '../core/wallet.service';
import { Transaction, errorMessage, money, movementLabel, when } from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    @if (error()) {
      <p class="alert">{{ error() }}</p>
    } @else if (balance() === null) {
      <p class="muted">يتم تحميل الرصيد...</p>
    } @else {
      <section class="balance">
        <p>الرصيد الحالي</p>
        <strong>{{ money(balance()!) }} <small>د.أ</small></strong>
        <div class="actions">
          <a class="button" routerLink="/deposit">شحن</a>
          <a class="button button-ghost" routerLink="/transfer">تحويل</a>
        </div>
      </section>

      <section class="panel">
        <header>
          <h2>آخر الحركات</h2>
          <a routerLink="/transactions">كل السجل</a>
        </header>
        @if (transactions().length === 0) {
          <p class="muted">ما في حركات بعد.</p>
        } @else {
          <ul class="ledger">
            @for (item of transactions(); track item.id) {
              <li>
                <div>
                  <strong>{{ movementLabel(item.type) }}</strong>
                  <span>{{ when(item.createdAt) }}</span>
                </div>
                <b [class.out]="item.type === 'TransferOut'">{{ signed(item) }}</b>
              </li>
            }
          </ul>
        }
      </section>
    }
  `,
})
export class DashboardPage {
  private readonly wallets = inject(WalletService);

  protected readonly balance = signal<number | null>(null);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly error = signal('');
  protected readonly money = money;
  protected readonly when = when;
  protected readonly movementLabel = movementLabel;

  constructor() {
    this.wallets.balance().subscribe({
      next: (response) => this.balance.set(response.balance),
      error: (error: unknown) => this.error.set(errorMessage(error, 'تعذر قراءة الرصيد.')),
    });
    this.wallets.transactions(1, 5).subscribe({
      next: (response) => this.transactions.set(response.items),
      error: () => this.transactions.set([]),
    });
  }

  protected signed(item: Transaction): string {
    const prefix = item.type === 'TransferOut' ? '−' : '+';
    return `${prefix}${money(item.amount)}`;
  }
}
