import { Routes } from '@angular/router';
import { roleGuard } from '../../services/role.guard';

export default [
    {
        path: 'istasyon',
        loadComponent: () => import('./istasyon-yonetimi/istasyon-yonetimi.component').then(m => m.IstasyonYonetimiComponent),
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_ISTASYON' }
    },
    {
        path: 'kullanici',
        loadComponent: () => import('./kullanici-yonetimi/kullanici-yonetimi.component').then(m => m.KullaniciYonetimiComponent),
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_KULLANICI' }
    },
    {
        path: 'yetki',
        loadComponent: () => import('./yetki-yonetimi/yetki-yonetimi.component').then(m => m.YetkiYonetimiComponent),
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_YETKI' }
    },
    {
        path: 'roller',
        loadComponent: () => import('./role-management/role-management.component').then(m => m.RoleManagementComponent),
        canActivate: [roleGuard],
        data: { resource: 'SISTEM_ROLLER' }
    },
    {
        path: 'ayarlar',
        loadComponent: () => import('./ayarlar/ayarlar.component').then(m => m.AyarlarComponent),
        canActivate: [roleGuard],
        data: { resource: 'SISTEM_AYARLAR' }
    },
    {
        path: 'bildirimler',
        loadComponent: () => import('./notification-sender/notification-sender.component').then(m => m.NotificationSenderComponent),
        canActivate: [roleGuard],
        data: { resource: 'SISTEM_BILDIRIM' }
    }
] as Routes;
