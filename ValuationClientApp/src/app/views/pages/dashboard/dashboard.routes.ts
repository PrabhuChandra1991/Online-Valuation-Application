import { Routes } from '@angular/router';
import { authGuard } from '../services/auth.guard';

export default [
    {
        path: '',
        loadComponent: () => import('./dashboard.component').then(c => c.DashboardComponent),
        canActivate:[authGuard]
    }
] as Routes;