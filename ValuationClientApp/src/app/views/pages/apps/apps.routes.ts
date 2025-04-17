import { Routes } from "@angular/router";
import { authGuard } from "../services/auth.guard";

export default [
    { path: '', redirectTo: 'calendar', pathMatch: 'full' },
    {
        path: 'email',
        loadComponent: () => import('./email/email.component').then(c => c.EmailComponent),
        children: [
            { path: '', redirectTo: 'inbox', pathMatch: 'full' },
            {
                path: 'inbox',
                loadComponent: () => import('./email/inbox/inbox.component').then(c => c.InboxComponent)
            },
            {
                path: 'read',
                loadComponent: () => import('./email/read/read.component').then(c => c.ReadComponent)
            },
            {
                path: 'compose',
                loadComponent: () => import('./email/compose/compose.component').then(c => c.ComposeComponent)
            }
        ]
    },
    {
        path: 'chat',
        loadComponent: () => import('./chat/chat.component').then(c => c.ChatComponent)
    },
    {
        path: 'user',
        loadComponent: () => import('./user/user.component').then(c => c.UserComponent),
        canActivate: [authGuard]
    },
    {
        path: 'register',
        loadComponent: () => import('../auth/register/register.component').then(c => c.RegisterComponent),
        canActivate: [authGuard]
    },
    {
        path: 'calendar',
        loadComponent: () => import('./calendar/calendar.component').then(c => c.CalendarComponent)
    },
    {
        path: 'qptemplate',
        loadComponent: () => import('./qp-template/template-management/template-management.component').then(c => c.TemplateManagemenComponent),
        canActivate: [authGuard]
    },
    {
        path: 'assigntemplate',
        loadComponent: () => import('./qp-template/template-assignment/template-assignment.component').then(c => c.TemplateAssignmentComponent),
        canActivate: [authGuard]
    },
    {
        path: 'edit/:id',
        loadComponent: () => import('./qp-template/template-assignment/template-assignment.component').then(c => c.TemplateAssignmentComponent),
        canActivate: [authGuard]
    },
    {
        path: 'master',
        loadComponent: () => import('./master/master.component')
            .then(c => c.MasterComponent),
        canActivate: [authGuard]
    },
    {
        path: 'importhistory',
        loadComponent: () => import('./import-history/import-history.component')
            .then(c => c.ImportHistoryComponent),
        canActivate: [authGuard]
    },
    {
        path: 'valuation',
        loadComponent: () => import('./valuation/answer-valuation/answer-valuation.component')
            .then(c => c.AnswerValuationComponent),
        canActivate: [authGuard]
    },
    {
        path: 'qptemplate',
        loadComponent: () => import('./qp-template/template-management/template-management.component')
            .then(c => c.TemplateManagemenComponent),
        canActivate: [authGuard]
    },
    {
        path: 'answersheet',
        loadComponent: () => import('./answersheet/answersheet-management/answersheet-management.component')
            .then(c => c.AnswersheetManagementComponent)
    },
    {
        path: 'answersheet/consolidatedview',
        loadComponent: () => import('./answersheet/consolidatedview/consolidatedview.component')
            .then(c => c.ConsolidatedviewComponent),
        canActivate: [authGuard]
    },
] as Routes;
