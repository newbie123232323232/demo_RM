import { Routes } from '@angular/router';
import { FakeBillPageComponent } from './pages/fake-bill/fake-bill.component';
import { RevenuePageComponent } from './pages/revenue/revenue.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'lab/bill' },
  {
    path: 'lab/bill',
    component: FakeBillPageComponent,
    title: 'Lập bill (fake buyer)',
  },
  {
    path: 'lab/revenue',
    component: RevenuePageComponent,
    title: 'Quản lý doanh thu',
  },
  { path: '**', redirectTo: 'lab/bill' },
];
