import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { AuthResponse } from './models';

const tokenKey = 'wallet_token';
const nameKey = 'wallet_name';
const emailKey = 'wallet_email';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  token(): string | null {
    return localStorage.getItem(tokenKey);
  }

  name(): string {
    return localStorage.getItem(nameKey) ?? '';
  }

  isLoggedIn(): boolean {
    return !!this.token();
  }

  login(email: string, password: string) {
    return this.http
      .post<AuthResponse>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.persist(response)));
  }

  register(fullName: string, email: string, password: string) {
    return this.http
      .post<AuthResponse>('/api/auth/register', { fullName, email, password })
      .pipe(tap((response) => this.persist(response)));
  }

  logout(): void {
    localStorage.removeItem(tokenKey);
    localStorage.removeItem(nameKey);
    localStorage.removeItem(emailKey);
    void this.router.navigate(['/login']);
  }

  private persist(response: AuthResponse): void {
    localStorage.setItem(tokenKey, response.token);
    localStorage.setItem(nameKey, response.fullName);
    localStorage.setItem(emailKey, response.email);
  }
}
