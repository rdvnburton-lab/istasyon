import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Notfound } from './app/pages/notfound/notfound';

import { authGuard } from './app/services/auth.guard';
import { roleGuard } from './app/services/role.guard';

export const appRoutes: Routes = [
    {
        path: '',
        component: AppLayout,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
            { path: 'dashboard', loadChildren: () => import('./app/pages/dashboard/dashboard.routes') },
            { path: 'vardiya', loadChildren: () => import('./app/pages/vardiya/vardiya.routes') },
            {
                path: 'sistem/ayarlar',
                loadComponent: () => import('./app/pages/sistem/ayarlar/ayarlar.component').then(m => m.AyarlarComponent),
                canActivate: [roleGuard]
            },
            { path: 'yonetim', loadChildren: () => import('./app/pages/yonetim/yonetim.routes').then(m => m.YONETIM_ROUTES) },

            {
                path: 'admin/health',
                loadComponent: () => import('./app/pages/dashboard/components/system-health/system-health.component').then(m => m.SystemHealthComponent),
                canActivate: [roleGuard],
                data: { resource: 'SISTEM_SAGLIK' }
            },
            {
                path: 'settings/roles',
                loadComponent: () => import('./app/pages/settings/role-management/role-management.component').then(m => m.RoleManagementComponent),
                canActivate: [roleGuard],
                data: { resource: 'SISTEM_ROLLER' }
            },
            {
                path: 'profile',
                loadComponent: () => import('./app/pages/profile/profile.component').then(m => m.ProfileComponent)
            },
            {
                path: 'admin/notifications',
                loadComponent: () => import('./app/pages/admin/notification-sender/notification-sender.component').then(m => m.NotificationSenderComponent),
                canActivate: [roleGuard],
                data: { resource: 'SISTEM_BILDIRIM' }
            }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
