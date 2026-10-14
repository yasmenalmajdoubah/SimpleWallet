import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { WalletService } from '../core/wallet.service';
import {
  Transaction,
  WalletHome,
  errorMessage,
  isOutgoing,
  money,
  movementLabel,
  pocketLabel,
  when,
} from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, ReactiveFormsModule],
  template: `
    @if (error()) {
      <p class="alert">{{ error() }}</p>
    } @else if (home() === null) {
      <p class="muted">يتم تحميل المحفظة...</p>
    } @else {
      <section class="balances">
        <article class="balance">
          <p>رصيد الصرف</p>
          <strong>{{ money(home()!.spendingBalance) }} <small>د.أ</small></strong>
        </article>
        <article class="balance">
          <p>رصيد التوفير</p>
          <strong>{{ money(home()!.savingsBalance) }} <small>د.أ</small></strong>
        </article>
      </section>

      <div class="actions home-actions">
        <a class="button" routerLink="/deposit">شحن</a>
        <a class="button button-ghost" routerLink="/transfer">تحويل</a>
        <a class="button button-ghost" routerLink="/bills">فواتير</a>
        <a class="button button-ghost" routerLink="/requests">طلبات</a>
      </div>

      <div class="grid-2">
        <section class="panel">
          <h2>الراتب</h2>
          <p class="muted">ينزل على رصيد الصرف، مرة واحدة كل شهر.</p>
          <form [formGroup]="salaryForm" (ngSubmit)="saveSalary()">
            <label>
              المبلغ الشهري
              <input dir="ltr" type="number" min="0" step="0.01" formControlName="amount" />
            </label>
            <button type="submit" [disabled]="busy()">حفظ الراتب</button>
          </form>
          <button type="button" [disabled]="busy() || !home()!.canCollectSalary" (click)="collect()">
            قبض الراتب
          </button>
        </section>

        <section class="panel">
          <h2>نقل بين الرصيدين</h2>
          <form [formGroup]="moveForm" (ngSubmit)="move()">
            <label>
              الاتجاه
              <select formControlName="direction">
                <option value="savings">من الصرف إلى التوفير</option>
                <option value="spending">من التوفير إلى الصرف</option>
              </select>
            </label>
            <label>
              المبلغ
              <input dir="ltr" type="number" min="0.01" step="0.01" formControlName="amount" />
            </label>
            <button type="submit" [disabled]="busy()">نقل</button>
          </form>
        </section>
      </div>

      @if (home()!.card) {
        <a class="plastic" routerLink="/card">
          <span>محفظة كارد</span>
          <strong dir="ltr">{{ home()!.card!.maskedNumber }}</strong>
          <div>
            <span>{{ home()!.card!.holderName }}</span>
            <span dir="ltr">{{ home()!.card!.expiry }}</span>
          </div>
        </a>
      }

      @if (home()!.incomingRequests > 0) {
        <p class="ok">في {{ home()!.incomingRequests }} طلب حوالة بانتظارك. <a routerLink="/requests">شوفهم</a></p>
      }
      @if (notice()) {
        <p class="ok">{{ notice() }}</p>
      }

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
                  <strong>{{ movementLabel(item.type) }} · {{ pocketLabel(item.pocket) }}</strong>
                  <span>{{ when(item.createdAt) }}</span>
                </div>
                <b [class.out]="isOutgoing(item.type, item.pocket)">{{ signed(item) }}</b>
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
  private readonly formBuilder = inject(FormBuilder);

  protected readonly home = signal<WalletHome | null>(null);
  protected readonly transactions = signal<Transaction[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly busy = signal(false);
  protected readonly money = money;
  protected readonly when = when;
  protected readonly movementLabel = movementLabel;
  protected readonly pocketLabel = pocketLabel;
  protected readonly isOutgoing = isOutgoing;
  protected readonly salaryForm = this.formBuilder.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(0)]],
  });
  protected readonly moveForm = this.formBuilder.nonNullable.group({
    direction: ['savings'],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  constructor() {
    this.load();
  }

  protected saveSalary(): void {
    const amount = Number(this.salaryForm.controls.amount.value);
    if (!Number.isFinite(amount) || amount < 0) {
      this.error.set('مبلغ الراتب غير صحيح.');
      return;
    }
    this.busy.set(true);
    this.wallets.setSalary(amount).subscribe({
      next: () => {
        this.notice.set('انحفظ الراتب.');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر حفظ الراتب.'));
      },
    });
  }

  protected collect(): void {
    this.busy.set(true);
    this.wallets.collectSalary().subscribe({
      next: () => {
        this.notice.set('نزل الراتب على رصيد الصرف.');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر قبض الراتب.'));
      },
    });
  }

  protected move(): void {
    const amount = Number(this.moveForm.controls.amount.value);
    if (!Number.isFinite(amount) || amount <= 0) {
      this.error.set('اكتب مبلغ النقل.');
      return;
    }
    this.busy.set(true);
    this.wallets.move(amount, this.moveForm.controls.direction.value === 'savings').subscribe({
      next: () => {
        this.notice.set('تم النقل.');
        this.moveForm.controls.amount.reset(null);
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر النقل.'));
      },
    });
  }

  protected signed(item: Transaction): string {
    const prefix = isOutgoing(item.type, item.pocket) ? '−' : '+';
    return `${prefix}${money(item.amount)}`;
  }

  private load(): void {
    this.wallets.home().subscribe({
      next: (home) => {
        this.home.set(home);
        this.salaryForm.controls.amount.setValue(home.salaryAmount);
        this.busy.set(false);
        this.error.set('');
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر قراءة المحفظة.'));
      },
    });
    this.wallets.transactions(1, 5).subscribe({
      next: (response) => this.transactions.set(response.items),
      error: () => this.transactions.set([]),
    });
  }
}
