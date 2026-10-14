import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WalletService } from '../core/wallet.service';
import { errorMessage, money } from '../core/models';

@Component({
  selector: 'app-deposit',
  imports: [ReactiveFormsModule],
  template: `
    <section class="panel narrow">
      <h1>شحن المحفظة</h1>
      <p class="muted">الشحن تجريبي، ويزيد رصيد الصرف.</p>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          المبلغ
          <input dir="ltr" type="number" min="0.01" step="0.01" formControlName="amount" />
        </label>
        <label>
          ملاحظة
          <input type="text" maxlength="300" formControlName="note" />
        </label>
        @if (error()) {
          <p class="alert">{{ error() }}</p>
        }
        @if (done()) {
          <p class="ok">تم الشحن. رصيد الصرف صار {{ money(done()!) }} د.أ</p>
        }
        <button type="submit" [disabled]="busy()">{{ busy() ? 'يتم الشحن...' : 'شحن' }}</button>
      </form>
    </section>
  `,
})
export class DepositPage {
  private readonly wallets = inject(WalletService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly done = signal<number | null>(null);
  protected readonly money = money;
  protected readonly form = this.formBuilder.nonNullable.group({
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
    note: [''],
  });

  protected submit(): void {
    const amount = Number(this.form.controls.amount.value);
    if (!Number.isFinite(amount) || amount <= 0) {
      this.error.set('اكتب مبلغًا أكبر من صفر.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.done.set(null);
    this.wallets.deposit(amount, this.form.controls.note.value).subscribe({
      next: (response) => {
        this.busy.set(false);
        this.done.set(response.spendingBalance);
        this.form.controls.amount.reset(null);
        this.form.controls.note.reset('');
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر الشحن.'));
      },
    });
  }
}
