import { Routes } from '@angular/router';
import { VardiyaListesi } from './components/vardiya-listesi/vardiya-listesi.component';
import { PompaYonetimi } from './components/pompa-yonetimi/pompa-yonetimi.component';
import { MarketYonetimi } from './components/market-yonetimi/market-yonetimi.component';
import { Karsilastirma } from './components/karsilastirma/karsilastirma.component';
import { OnayBekleyenlerComponent } from './components/onay-bekleyenler/onay-bekleyenler.component';

export default [
    { path: '', component: VardiyaListesi },
    { path: 'pompa', component: PompaYonetimi },
    { path: 'market', component: MarketYonetimi },
    { path: 'karsilastirma', component: Karsilastirma },
    { path: 'onay-bekleyenler', component: OnayBekleyenlerComponent },
    { path: 'tanimlamalar/istasyon', loadComponent: () => import('./components/istasyon/istasyon-tanimlama.component').then(m => m.IstasyonTanimlamaComponent) },
    { path: 'tanimlamalar/personel', loadComponent: () => import('./components/personel/personel-tanimlama.component').then(m => m.PersonelTanimlamaComponent) },
    { path: 'raporlar/vardiya', loadComponent: () => import('./components/raporlar/vardiya-raporu.component').then(m => m.VardiyaRaporuComponent) },
    { path: 'raporlar/personel', loadComponent: () => import('./components/raporlar/personel-karnesi.component').then(m => m.PersonelKarnesiComponent) },
    { path: 'raporlar/fark', loadComponent: () => import('./components/raporlar/fark-raporu.component').then(m => m.FarkRaporuComponent) }
] as Routes;
