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
            { path: '', redirectTo: 'vardiya', pathMatch: 'full' },
            { path: 'vardiya', loadChildren: () => import('./app/pages/vardiya/vardiya.routes') },
            {
                path: 'sistem/ayarlar',
                loadComponent: () => import('./app/pages/sistem/ayarlar/ayarlar.component').then(m => m.AyarlarComponent)
            }
        ]
    },
    { path: 'notfound', component: Notfound },
    { path: 'auth', loadChildren: () => import('./app/pages/auth/auth.routes') },
    { path: '**', redirectTo: '/notfound' }
];
