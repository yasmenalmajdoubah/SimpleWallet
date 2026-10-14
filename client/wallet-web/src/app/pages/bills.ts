import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WalletService } from '../core/wallet.service';
import { Bill, errorMessage, money } from '../core/models';

@Component({
  selector: 'app-bills',
  imports: [ReactiveFormsModule],
  template: `
    <section class="panel narrow">
      <h1>دفع فاتورة</h1>
      <p class="muted">المبلغ بينخصم من رصيد الصرف.</p>
      <form [formGroup]="form" (ngSubmit)="pay()">
        <label>
          الفاتورة
          <select formControlName="code">
            @for (bill of bills(); track bill.code) {
              <option [value]="bill.code">{{ bill.name }}</option>
            }
          </select>
        </label>
        <label>
          رقم الاشتراك
          <input dir="ltr" type="text" maxlength="40" formControlName="reference" />
        </label>
        <label>
          المبلغ
          <input dir="ltr" type="number" min="0.01" step="0.01" formControlName="amount" />
        </label>
        @if (error()) {
          <p class="alert">{{ error() }}</p>
        }
        @if (notice()) {
          <p class="ok">{{ notice() }}</p>
        }
        <button type="submit" [disabled]="busy()">ادفع</button>
      </form>
    </section>
  `,
})
export class BillsPage {
  private readonly wallets = inject(WalletService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly bills = signal<Bill[]>([]);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly busy = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({
    code: ['electricity', Validators.required],
    reference: ['', Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  constructor() {
    this.wallets.bills().subscribe({
      next: (bills) => {
        this.bills.set(bills);
        if (bills[0]) this.form.controls.code.setValue(bills[0].code);
      },
      error: () => this.error.set('تعذر تحميل أنواع الفواتير.'),
    });
  }

  protected pay(): void {
    const amount = Number(this.form.controls.amount.value);
    const reference = this.form.controls.reference.value.trim();
    if (!reference || !Number.isFinite(amount) || amount <= 0) {
      this.error.set('اكتب رقم الاشتراك والمبلغ.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.wallets.payBill(this.form.controls.code.value, amount, reference).subscribe({
      next: (response) => {
        this.busy.set(false);
        this.notice.set(`اندفعت الفاتورة. رصيد الصرف صار ${money(response.spendingBalance)} د.أ`);
        this.form.controls.reference.reset('');
        this.form.controls.amount.reset(null);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر دفع الفاتورة.'));
      },
    });
  }
}
