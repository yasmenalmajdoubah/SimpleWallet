import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/models';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="gate">
      <form class="card" [formGroup]="form" (ngSubmit)="submit()">
        <p class="mark">محفظة بسيطة</p>
        <h1>دخول</h1>
        <label>
          الإيميل
          <input dir="ltr" type="email" formControlName="email" autocomplete="username" />
        </label>
        <label>
          كلمة السر
          <input dir="ltr" type="password" formControlName="password" autocomplete="current-password" />
        </label>
        @if (error()) {
          <p class="alert">{{ error() }}</p>
        }
        <button type="submit" [disabled]="busy()">{{ busy() ? 'لحظات...' : 'دخول' }}</button>
        <p class="hint">
          للتجربة: ahmed&#64;example.com أو sara&#64;example.com
          <br />
          كلمة السر Wallet123!
        </p>
        <a routerLink="/register">إنشاء حساب جديد</a>
      </form>
    </section>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.error.set('اكتب الإيميل وكلمة السر.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    this.auth.login(value.email, value.password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر تسجيل الدخول.'));
      },
    });
  }
}
