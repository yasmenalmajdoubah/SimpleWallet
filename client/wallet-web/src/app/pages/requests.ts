import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WalletService } from '../core/wallet.service';
import { MoneyRequestItem, errorMessage, money, requestStatusLabel, when } from '../core/models';

@Component({
  selector: 'app-requests',
  imports: [ReactiveFormsModule],
  template: `
    <section class="panel narrow">
      <h1>اطلب حوالة</h1>
      <p class="muted">الشخص الثاني بوافق، والمبلغ بينخصم من رصيد صرفه.</p>
      <form [formGroup]="form" (ngSubmit)="create()">
        <label>
          إيميل الشخص
          <input dir="ltr" type="email" formControlName="payerEmail" />
        </label>
        <label>
          المبلغ
          <input dir="ltr" type="number" min="0.01" step="0.01" formControlName="amount" />
        </label>
        <label>
          ملاحظة
          <input type="text" maxlength="300" formControlName="note" />
        </label>
        <button type="submit" [disabled]="busy()">إرسال الطلب</button>
      </form>
    </section>

    @if (error()) {
      <p class="alert">{{ error() }}</p>
    }
    @if (notice()) {
      <p class="ok">{{ notice() }}</p>
    }

    <section class="panel">
      <h2>الطلبات</h2>
      @if (items() === null) {
        <p class="muted">يتم التحميل...</p>
      } @else if (items()!.length === 0) {
        <p class="muted">ما في طلبات.</p>
      } @else {
        <ul class="ledger">
          @for (item of items()!; track item.id) {
            <li>
              <div>
                <strong>
                  {{ item.direction === 'incoming' ? 'مطلوب منك' : 'طلبته أنت' }}
                  · {{ requestStatusLabel(item.status) }}
                </strong>
                <span>{{ item.counterpartyName }} · {{ item.counterpartyEmail }}</span>
                <span>{{ item.note || 'بدون ملاحظة' }} · {{ when(item.createdAt) }}</span>
              </div>
              <div class="request-actions">
                <b>{{ money(item.amount) }}</b>
                @if (item.status === 'Pending' && item.direction === 'incoming') {
                  <button type="button" [disabled]="busy()" (click)="accept(item)">قبول</button>
                  <button type="button" class="quiet" [disabled]="busy()" (click)="reject(item)">رفض</button>
                }
                @if (item.status === 'Pending' && item.direction === 'outgoing') {
                  <button type="button" class="quiet" [disabled]="busy()" (click)="cancel(item)">إلغاء</button>
                }
              </div>
            </li>
          }
        </ul>
      }
    </section>
  `,
})
export class RequestsPage {
  private readonly wallets = inject(WalletService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly items = signal<MoneyRequestItem[] | null>(null);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly busy = signal(false);
  protected readonly money = money;
  protected readonly when = when;
  protected readonly requestStatusLabel = requestStatusLabel;
  protected readonly form = this.formBuilder.nonNullable.group({
    payerEmail: ['', [Validators.required, Validators.email]],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    note: [''],
  });

  constructor() {
    this.load();
  }

  protected create(): void {
    const amount = Number(this.form.controls.amount.value);
    const payerEmail = this.form.controls.payerEmail.value.trim();
    if (this.form.controls.payerEmail.invalid || !Number.isFinite(amount) || amount <= 0) {
      this.error.set('اكتب الإيميل والمبلغ.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.wallets.createRequest(payerEmail, amount, this.form.controls.note.value).subscribe({
      next: () => {
        this.notice.set('انبعت الطلب.');
        this.form.controls.amount.reset(null);
        this.form.controls.note.reset('');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر إرسال الطلب.'));
      },
    });
  }

  protected accept(item: MoneyRequestItem): void {
    this.busy.set(true);
    this.wallets.acceptRequest(item.id).subscribe({
      next: () => {
        this.notice.set('تم التحويل.');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر قبول الطلب.'));
      },
    });
  }

  protected reject(item: MoneyRequestItem): void {
    this.busy.set(true);
    this.wallets.rejectRequest(item.id).subscribe({
      next: () => {
        this.notice.set('انرفض الطلب.');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر رفض الطلب.'));
      },
    });
  }

  protected cancel(item: MoneyRequestItem): void {
    this.busy.set(true);
    this.wallets.cancelRequest(item.id).subscribe({
      next: () => {
        this.notice.set('انلغى الطلب.');
        this.load();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر إلغاء الطلب.'));
      },
    });
  }

  private load(): void {
    this.wallets.requests().subscribe({
      next: (items) => {
        this.items.set(items);
        this.busy.set(false);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر قراءة الطلبات.'));
      },
    });
  }
}
