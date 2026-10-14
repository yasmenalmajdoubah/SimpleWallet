import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WalletService } from '../core/wallet.service';
import { CardInfo, errorMessage, money } from '../core/models';

@Component({
  selector: 'app-card',
  imports: [ReactiveFormsModule],
  template: `
    @if (card() === null && !error()) {
      <p class="muted">يتم تحميل البطاقة...</p>
    } @else if (card()) {
      <section class="plastic plastic-static">
        <span>محفظة كارد</span>
        <strong dir="ltr">{{ card()!.maskedNumber }}</strong>
        <div>
          <span>{{ card()!.holderName }}</span>
          <span dir="ltr">{{ card()!.expiry }}</span>
        </div>
      </section>
      <section class="panel narrow">
        <p class="muted">الدفع بينخصم من رصيد الصرف. التجميد بيوقف الدفع لحين ما تفكها.</p>
        <button type="button" class="quiet" [disabled]="busy()" (click)="toggle()">
          {{ card()!.isFrozen ? 'فك التجميد' : 'تجميد البطاقة' }}
        </button>
        <form [formGroup]="form" (ngSubmit)="pay()">
          <label>
            التاجر
            <input type="text" maxlength="80" formControlName="merchant" />
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
          <button type="submit" [disabled]="busy() || card()!.isFrozen">ادفع بالبطاقة</button>
        </form>
      </section>
    } @else {
      <p class="alert">{{ error() }}</p>
    }
  `,
})
export class CardPage {
  private readonly wallets = inject(WalletService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly card = signal<CardInfo | null>(null);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly busy = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({
    merchant: ['', Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  constructor() {
    this.wallets.home().subscribe({
      next: (home) => this.card.set(home.card),
      error: (error: unknown) => this.error.set(errorMessage(error, 'تعذر قراءة البطاقة.')),
    });
  }

  protected toggle(): void {
    const current = this.card();
    if (!current) return;
    this.busy.set(true);
    this.wallets.freezeCard(!current.isFrozen).subscribe({
      next: (card) => {
        this.card.set(card);
        this.busy.set(false);
        this.notice.set(card.isFrozen ? 'البطاقة تجمدت.' : 'البطاقة رجعت تشتغل.');
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر تغيير حالة البطاقة.'));
      },
    });
  }

  protected pay(): void {
    const amount = Number(this.form.controls.amount.value);
    const merchant = this.form.controls.merchant.value.trim();
    if (!merchant || !Number.isFinite(amount) || amount <= 0) {
      this.error.set('اكتب اسم التاجر والمبلغ.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    this.wallets.payWithCard(amount, merchant).subscribe({
      next: (response) => {
        this.busy.set(false);
        this.notice.set(`تم الدفع. رصيد الصرف صار ${money(response.spendingBalance)} د.أ`);
        this.form.reset({ merchant: '', amount: null });
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر الدفع.'));
      },
    });
  }
}
