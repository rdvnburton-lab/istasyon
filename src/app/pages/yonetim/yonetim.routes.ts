import { Routes } from '@angular/router';
import { IstasyonYonetimiComponent } from './istasyon-yonetimi/istasyon-yonetimi.component';
import { KullaniciYonetimiComponent } from './kullanici-yonetimi/kullanici-yonetimi.component';
import { YetkiYonetimiComponent } from './yetki-yonetimi/yetki-yonetimi.component';

import { roleGuard } from '../../services/role.guard';

export const YONETIM_ROUTES: Routes = [
    {
        path: 'istasyon',
        component: IstasyonYonetimiComponent,
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_ISTASYON' }
    },
    {
        path: 'kullanici',
        component: KullaniciYonetimiComponent,
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_KULLANICI' }
    },
    {
        path: 'yetki',
        component: YetkiYonetimiComponent,
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_YETKI' }
    }
];
