import { Component, inject, signal } from '@angular/core';
import { WalletService } from '../core/wallet.service';
import { Transaction, errorMessage, money, movementLabel, when } from '../core/models';

@Component({
  selector: 'app-transactions',
  template: `
    <section class="panel">
      <h1>سجل الحركات</h1>
      @if (error()) {
        <p class="alert">{{ error() }}</p>
      } @else if (items() === null) {
        <p class="muted">يتم تحميل السجل...</p>
      } @else if (items()!.length === 0) {
        <p class="muted">ما في حركات بعد.</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>الحركة</th>
              <th>التفاصيل</th>
              <th>المبلغ</th>
              <th>الرصيد بعدين</th>
            </tr>
          </thead>
          <tbody>
            @for (item of items()!; track item.id) {
              <tr>
                <td>{{ movementLabel(item.type) }}</td>
                <td>
                  <div>{{ detail(item) }}</div>
                  <span class="muted">{{ when(item.createdAt) }}</span>
                </td>
                <td [class.out]="item.type === 'TransferOut'">{{ signed(item) }}</td>
                <td>{{ money(item.balanceAfter) }}</td>
              </tr>
            }
          </tbody>
        </table>
        <div class="pager">
          <button type="button" class="quiet" [disabled]="page() === 1" (click)="go(page() - 1)">السابق</button>
          <span>صفحة {{ page() }} من {{ pages() }}</span>
          <button type="button" class="quiet" [disabled]="page() >= pages()" (click)="go(page() + 1)">التالي</button>
        </div>
      }
    </section>
  `,
})
export class TransactionsPage {
  private readonly wallets = inject(WalletService);
  private readonly pageSize = 10;

  protected readonly items = signal<Transaction[] | null>(null);
  protected readonly page = signal(1);
  protected readonly pages = signal(1);
  protected readonly error = signal('');
  protected readonly money = money;
  protected readonly when = when;
  protected readonly movementLabel = movementLabel;

  constructor() {
    this.load(1);
  }

  protected go(page: number): void {
    this.load(page);
  }

  protected signed(item: Transaction): string {
    const prefix = item.type === 'TransferOut' ? '−' : '+';
    return `${prefix}${money(item.amount)}`;
  }

  protected detail(item: Transaction): string {
    if (item.note) return item.note;
    if (item.counterpartyEmail) return item.counterpartyEmail;
    return '—';
  }

  private load(page: number): void {
    this.wallets.transactions(page, this.pageSize).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.page.set(response.page);
        this.pages.set(Math.max(1, Math.ceil(response.totalCount / response.pageSize)));
      },
      error: (error: unknown) => this.error.set(errorMessage(error, 'تعذر قراءة السجل.')),
    });
  }
}
