import { Routes } from '@angular/router';
import { FakeBillPageComponent } from './pages/fake-bill/fake-bill.component';
import { ProductsPageComponent } from './pages/products/products.component';
import { RevenuePageComponent } from './pages/revenue/revenue.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'lab/bill' },
  {
    path: 'lab/bill',
    component: FakeBillPageComponent,
    title: 'Lập bill (fake buyer)',
  },
  {
    path: 'lab/products',
    component: ProductsPageComponent,
    title: 'Sản phẩm',
  },
  {
    path: 'lab/revenue',
    component: RevenuePageComponent,
    title: 'Quản lý doanh thu',
  },
  { path: '**', redirectTo: 'lab/bill' },
];
