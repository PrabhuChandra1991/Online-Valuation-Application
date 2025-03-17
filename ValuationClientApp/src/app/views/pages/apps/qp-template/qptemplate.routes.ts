import { Routes } from '@angular/router';
import { authGuard } from '../../services/auth.guard';

export default [
    {
        path: '',
        loadComponent: () => import('./template-management/template-management.component').then(c => c.TemplateManagemenComponent),
        canActivate: [authGuard]
    },
    {
        path: 'edit/:id',
        loadComponent: () => import('./template-management/template-management.component').then(c => c.TemplateManagemenComponent),
        canActivate: [authGuard]
    }
] as Routes;