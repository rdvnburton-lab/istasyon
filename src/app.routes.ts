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
            { path: 'operasyon', loadChildren: () => import('./app/pages/operasyon/operasyon.routes') },
            { path: 'raporlar', loadChildren: () => import('./app/pages/raporlar/raporlar.routes') },
            { path: 'tanimlar', loadChildren: () => import('./app/pages/tanimlar/tanimlar.routes') },
            { path: 'sistem', loadChildren: () => import('./app/pages/sistem/sistem.routes') },
            {
                path: 'profile',
                loadComponent: () => import('./app/pages/profile/profile.component').then(m => m.ProfileComponent)
            },
            // Admin health is now under sistem
            {
                path: 'admin/health',
                redirectTo: 'sistem/health'
            }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
