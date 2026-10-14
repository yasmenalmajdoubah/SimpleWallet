import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="frame">
      <header class="top">
        <a class="brand" routerLink="/">محفظة</a>
        <nav>
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">الرئيسية</a>
          <a routerLink="/deposit" routerLinkActive="active">شحن</a>
          <a routerLink="/transfer" routerLinkActive="active">تحويل</a>
          <a routerLink="/requests" routerLinkActive="active">طلبات</a>
          <a routerLink="/bills" routerLinkActive="active">فواتير</a>
          <a routerLink="/card" routerLinkActive="active">البطاقة</a>
          <a routerLink="/transactions" routerLinkActive="active">السجل</a>
        </nav>
        <div class="who">
          <span>{{ auth.name() }}</span>
          <button type="button" class="quiet" (click)="auth.logout()">خروج</button>
        </div>
      </header>
      <main>
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellPage {
  protected readonly auth = inject(AuthService);
}
