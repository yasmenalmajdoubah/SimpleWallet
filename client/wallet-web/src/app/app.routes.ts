import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';
import { BillsPage } from './pages/bills';
import { CardPage } from './pages/card';
import { DashboardPage } from './pages/dashboard';
import { DepositPage } from './pages/deposit';
import { LoginPage } from './pages/login';
import { RegisterPage } from './pages/register';
import { RequestsPage } from './pages/requests';
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
      { path: 'requests', component: RequestsPage },
      { path: 'bills', component: BillsPage },
      { path: 'card', component: CardPage },
      { path: 'transactions', component: TransactionsPage },
    ],
  },
  { path: '**', redirectTo: '' },
];
