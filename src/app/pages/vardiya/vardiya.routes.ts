import { Routes } from '@angular/router';
import { VardiyaListesi } from './components/vardiya-listesi/vardiya-listesi.component';
import { PompaYonetimi } from './components/pompa-yonetimi/pompa-yonetimi.component';
import { MarketYonetimi } from './components/market-yonetimi/market-yonetimi.component';
import { Karsilastirma } from './components/karsilastirma/karsilastirma.component';

export default [
    { path: '', component: VardiyaListesi },
    { path: 'pompa', component: PompaYonetimi },
    { path: 'market', component: MarketYonetimi },
    { path: 'karsilastirma', component: Karsilastirma }
] as Routes;
