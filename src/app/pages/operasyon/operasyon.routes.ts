import { Routes } from '@angular/router';
import { VardiyaListesi } from './vardiya-listesi/vardiya-listesi.component';
import { PompaYonetimi } from './pompa-yonetimi/pompa-yonetimi.component';
import { MarketYonetimiComponent } from './market-yonetimi/market-yonetimi.component';
import { roleGuard } from '../../services/role.guard';

export default [
    {
        path: '',
        component: VardiyaListesi,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_LISTESI' }
    },
    {
        path: 'pompa/:id',
        component: PompaYonetimi,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_POMPA' }
    },
    {
        path: 'market',
        component: MarketYonetimiComponent,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_MARKET' }
    },
    {
        path: 'stok',
        loadComponent: () => import('./yakit-stok/yakit-stok.component').then(m => m.YakitStokComponent),
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_STOK' }
    },
    {
        path: 'cari',
        loadComponent: () => import('./cari-yonetimi/cari-yonetimi.component').then(m => m.CariYonetimiComponent),
        canActivate: [roleGuard],
        // data: { resource: 'VARDIYA_CARI' } // Yetki kontrolü eklenene kadar açık bırakıyoruz
    }
] as Routes;
