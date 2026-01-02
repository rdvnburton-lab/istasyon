import { Routes } from '@angular/router';
import { roleGuard } from '../../services/role.guard';

export default [
    {
        path: 'personel',
        loadComponent: () => import('./personel/personel-tanimlama.component').then(m => m.PersonelTanimlamaComponent),
        canActivate: [roleGuard],
        data: { resource: 'TANIMLAMA_PERSONEL' }
    },
    {
        path: 'yakit',
        loadComponent: () => import('./yakit/yakit-tanimlari.component').then(m => m.YakitTanimlariComponent),
        canActivate: [roleGuard],
        data: { resource: 'TANIMLAMA_YAKIT' }
    },
    {
        path: 'genel',
        loadComponent: () => import('./definitions/definitions.component').then(m => m.DefinitionsComponent),
        canActivate: [roleGuard],
        data: { resource: 'YONETIM_TANIMLAR' }
    }
] as Routes;
