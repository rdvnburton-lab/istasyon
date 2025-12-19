import { Routes } from '@angular/router';
import { VardiyaListesi } from './components/vardiya-listesi.component';
import { PompaYonetimi } from './components/pompa-yonetimi.component';
import { MarketYonetimi } from './components/market-yonetimi.component';
import { Karsilastirma } from './components/karsilastirma.component';

export default [
    { path: '', component: VardiyaListesi },
    { path: 'pompa', component: PompaYonetimi },
    { path: 'market', component: MarketYonetimi },
    { path: 'karsilastirma', component: Karsilastirma }
] as Routes;
