import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { DashboardPage } from './pages/dashboard';
import { DepositPage } from './pages/deposit';
import { LoginPage } from './pages/login';
import { RegisterPage } from './pages/register';
import { ShellPage } from './pages/shell';
import { TransactionsPage } from './pages/transactions';
import { TransferPage } from './pages/transfer';

export const routes: Routes = [
  { path: 'login', component: LoginPage, canActivate: [guestGuard] },
  { path: 'register', component: RegisterPage, canActivate: [guestGuard] },
  {
    path: '',
    component: ShellPage,
    canActivate: [authGuard],
    children: [
      { path: '', component: DashboardPage },
      { path: 'deposit', component: DepositPage },
      { path: 'transfer', component: TransferPage },
      { path: 'transactions', component: TransactionsPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
