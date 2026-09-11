import { Routes } from '@angular/router';
import { anonymousGuard, authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'board' },
  {
    path: 'login',
    title: 'Sign in - Notes',
    canActivate: [anonymousGuard],
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'board',
    title: 'Notes',
    canActivate: [authGuard],
    loadComponent: () => import('./features/board/board.component').then((m) => m.BoardComponent)
  },
  { path: '**', redirectTo: 'board' }
];
