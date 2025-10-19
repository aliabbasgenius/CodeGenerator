import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Dashboard } from './components/dashboard/dashboard';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'login', component: Login },
  { path: 'dashboard', component: Dashboard, canActivate: [authGuard] },
  { path: 'generator', component: Dashboard, canActivate: [authGuard] },
  { path: 'templates', component: Dashboard, canActivate: [authGuard] },
  { path: 'projects', component: Dashboard, canActivate: [authGuard] },
  { path: 'settings', component: Dashboard, canActivate: [authGuard] },
  { path: 'help', component: Dashboard, canActivate: [authGuard] },
  { path: '**', redirectTo: '/dashboard' } // Wildcard route for 404 errors
];
