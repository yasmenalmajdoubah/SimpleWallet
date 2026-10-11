import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { errorMessage } from '../core/models';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <section class="gate">
      <form class="card" [formGroup]="form" (ngSubmit)="submit()">
        <p class="mark">محفظة بسيطة</p>
        <h1>حساب جديد</h1>
        <label>
          الاسم
          <input type="text" formControlName="fullName" autocomplete="name" />
        </label>
        <label>
          الإيميل
          <input dir="ltr" type="email" formControlName="email" autocomplete="username" />
        </label>
        <label>
          كلمة السر
          <input dir="ltr" type="password" formControlName="password" autocomplete="new-password" />
        </label>
        @if (error()) {
          <p class="alert">{{ error() }}</p>
        }
        <button type="submit" [disabled]="busy()">{{ busy() ? 'لحظات...' : 'إنشاء الحساب' }}</button>
        <a routerLink="/login">عندي حساب</a>
      </form>
    </section>
  `,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.error.set('تأكد من الاسم والإيميل، وكلمة السر 8 أحرف على الأقل.');
      return;
    }
    this.busy.set(true);
    this.error.set('');
    const value = this.form.getRawValue();
    this.auth.register(value.fullName, value.email, value.password).subscribe({
      next: () => void this.router.navigate(['/']),
      error: (error: unknown) => {
        this.busy.set(false);
        this.error.set(errorMessage(error, 'تعذر إنشاء الحساب.'));
      },
    });
  }
}
