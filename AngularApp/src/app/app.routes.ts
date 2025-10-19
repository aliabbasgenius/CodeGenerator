import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Dashboard } from './components/dashboard/dashboard';
import { ProductList } from './components/product-list/product-list';
import { ProductForm } from './components/product-form/product-form';
import { CodeGenerator } from './components/code-generator/code-generator';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'generator', component: CodeGenerator, canActivate: [authGuard] },
  { path: 'products', component: ProductList, canActivate: [authGuard] },
  { path: 'products/new', component: ProductForm, canActivate: [authGuard] },
  { path: 'products/edit/:id', component: ProductForm, canActivate: [authGuard] },
  { path: 'settings', component: Dashboard, canActivate: [authGuard] },
  { path: '**', redirectTo: '/dashboard' } // Wildcard route for 404 errors
];
