import { Routes } from '@angular/router';
import { roleGuard } from '../../services/role.guard';

export default [
    {
        path: 'vardiya',
        loadComponent: () => import('./vardiya-raporu.component').then(m => m.VardiyaRaporuComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_VARDIYA' }
    },
    {
        path: 'personel',
        loadComponent: () => import('./personel-karnesi.component').then(m => m.PersonelKarnesiComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_PERSONEL' }
    },
    {
        path: 'fark',
        loadComponent: () => import('./fark-raporu.component').then(m => m.FarkRaporuComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_FARK' }
    },
    {
        path: 'karsilastirma',
        loadComponent: () => import('./karsilastirma/karsilastirma.component').then(m => m.Karsilastirma),
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_KARSILASTIRMA' }
    },
    {
        path: 'onay-bekleyenler',
        loadComponent: () => import('./onay-bekleyenler/onay-bekleyenler.component').then(m => m.OnayBekleyenlerComponent),
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_ONAY_BEKLEYENLER' }
    },
    {
        path: 'loglar',
        loadComponent: () => import('./vardiya-loglar/vardiya-loglar.component').then(m => m.VardiyaLoglarComponent),
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_LOGLAR' }
    }
] as Routes;
