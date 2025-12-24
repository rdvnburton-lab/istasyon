import { Routes } from '@angular/router';
import { IstasyonYonetimiComponent } from './istasyon-yonetimi/istasyon-yonetimi.component';
import { KullaniciYonetimiComponent } from './kullanici-yonetimi/kullanici-yonetimi.component';

export const YONETIM_ROUTES: Routes = [
    { path: 'istasyon', component: IstasyonYonetimiComponent },
    { path: 'kullanici', component: KullaniciYonetimiComponent }
];
