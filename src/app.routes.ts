import { Routes } from '@angular/router';
import { AppLayout } from './app/layout/component/app.layout';
import { Notfound } from './app/pages/notfound/notfound';

import { authGuard } from './app/services/auth.guard';

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
                loadComponent: () => import('./app/pages/sistem/ayarlar/ayarlar.component').then(m => m.AyarlarComponent)
            },
            { path: 'yonetim', loadChildren: () => import('./app/pages/yonetim/yonetim.routes').then(m => m.YONETIM_ROUTES) },
            {
                path: 'admin/istasyonlar',
                loadComponent: () => import('./app/pages/admin/istasyon-yonetimi/istasyon-yonetimi.component').then(m => m.IstasyonYonetimiComponent)
            }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
