import { Routes } from '@angular/router';
import { VardiyaListesi } from './components/vardiya-listesi/vardiya-listesi.component';
import { PompaYonetimi } from './components/pompa-yonetimi/pompa-yonetimi.component';
import { MarketYonetimi } from './components/market-yonetimi/market-yonetimi.component';
import { Karsilastirma } from './components/karsilastirma/karsilastirma.component';
import { OnayBekleyenlerComponent } from './components/onay-bekleyenler/onay-bekleyenler.component';
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
        component: MarketYonetimi,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_MARKET' }
    },
    {
        path: 'karsilastirma',
        component: Karsilastirma,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_KARSILASTIRMA' }
    },
    {
        path: 'onay-bekleyenler',
        component: OnayBekleyenlerComponent,
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_ONAY_BEKLEYENLER' }
    },

    {
        path: 'tanimlamalar/personel',
        loadComponent: () => import('./components/personel/personel-tanimlama.component').then(m => m.PersonelTanimlamaComponent),
        canActivate: [roleGuard],
        data: { resource: 'TANIMLAMA_PERSONEL' }
    },
    {
        path: 'raporlar/vardiya',
        loadComponent: () => import('./components/raporlar/vardiya-raporu.component').then(m => m.VardiyaRaporuComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_VARDIYA' }
    },
    {
        path: 'raporlar/personel',
        loadComponent: () => import('./components/raporlar/personel-karnesi.component').then(m => m.PersonelKarnesiComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_PERSONEL' }
    },
    {
        path: 'raporlar/fark',
        loadComponent: () => import('./components/raporlar/fark-raporu.component').then(m => m.FarkRaporuComponent),
        canActivate: [roleGuard],
        data: { resource: 'RAPOR_FARK' }
    },
    {
        path: 'loglar',
        loadComponent: () => import('./components/vardiya-loglar/vardiya-loglar.component').then(m => m.VardiyaLoglarComponent),
        canActivate: [roleGuard],
        data: { resource: 'VARDIYA_LOGLAR' }
    }
] as Routes;
