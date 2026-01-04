import { Injectable } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { BehaviorSubject, combineLatest, map, Observable, of } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { PermissionService } from '../../services/permission.service';
import { VardiyaApiService } from '../../pages/operasyon/services/vardiya-api.service';

@Injectable({
    providedIn: 'root'
})
export class AppMenuService {
    private menuSubject = new BehaviorSubject<MenuItem[]>([]);
    public menu$ = this.menuSubject.asObservable();

    constructor(
        private authService: AuthService,
        private permissionService: PermissionService,
        private vardiyaApiService: VardiyaApiService
    ) {
        // Update menu when permissions or pending count changes
        // Update menu when permissions, pending count, or CURRENT USER changes
        combineLatest([
            this.permissionService.permissions$,
            this.vardiyaApiService.pendingCount$,
            this.authService.currentUser$
        ]).subscribe(([_, pendingCount, user]) => {
            // We don't strictly need access to 'user' here as we call getCurrentUser() inside updateMenu, 
            // but the subscription ensures we trigger when it changes.
            this.updateMenu(pendingCount);
        });

        // Initialize
        this.updateMenu();
    }

    private updateMenu(pendingCount: number = 0) {
        const user = this.authService.getCurrentUser();
        const role = user?.role?.toLowerCase() || '';

        // Helper to check permission
        const hasAccess = (resource: string) => this.permissionService.hasAccess(role, resource);

        const model: MenuItem[] = [
            {
                label: 'Genel',
                items: [
                    {
                        label: 'Anasayfa',
                        data: { shortLabel: 'Özet' },
                        icon: 'pi pi-fw pi-home',
                        routerLink: ['/dashboard']
                    }
                ]
            },
            {
                label: 'Operasyonlar',
                items: [
                    {
                        label: 'Vardiya Listesi',
                        data: { shortLabel: 'Vardiyalar' },
                        icon: 'pi pi-fw pi-list',
                        routerLink: ['/operasyon'],
                        visible: hasAccess('VARDIYA_LISTESI')
                    },
                    {
                        label: 'Market Mutabakatı',
                        data: { shortLabel: 'Market' },
                        icon: 'pi pi-fw pi-shopping-bag',
                        routerLink: ['/operasyon/market'],
                        visible: hasAccess('VARDIYA_MARKET')
                    },
                    {
                        label: 'Cari Yönetimi',
                        data: { shortLabel: 'Cari' },
                        icon: 'pi pi-fw pi-users',
                        routerLink: ['/operasyon/cari'],
                        visible: true // Geçici olarak herkese açık
                    },
                    {
                        label: 'Yakıt Stok Yönetimi',
                        data: { shortLabel: 'Stok' },
                        icon: 'pi pi-fw pi-filter-fill',
                        routerLink: ['/operasyon/stok'],
                        visible: hasAccess('VARDIYA_STOK')
                    },
                    {
                        label: 'Onay Bekleyenler',
                        data: { shortLabel: 'Onaylar' },
                        icon: 'pi pi-fw pi-check-circle',
                        routerLink: ['/raporlar/onay-bekleyenler'],
                        visible: hasAccess('VARDIYA_ONAY_BEKLEYENLER'),
                        badge: pendingCount > 0 ? pendingCount.toString() : undefined,
                        badgeStyleClass: 'pulse-badge'
                    }
                ].filter(i => i.visible !== false)
            },
            {
                label: 'Analiz & Raporlar',
                items: [
                    {
                        label: 'Karşılaştırma Raporu',
                        data: { shortLabel: 'Kıyasla' },
                        icon: 'pi pi-fw pi-chart-bar',
                        routerLink: ['/raporlar/karsilastirma'],
                        visible: hasAccess('VARDIYA_KARSILASTIRMA')
                    },
                    {
                        label: 'Vardiya Raporu',
                        data: { shortLabel: 'Raporlar' },
                        icon: 'pi pi-fw pi-file',
                        routerLink: ['/raporlar/vardiya'],
                        visible: hasAccess('RAPOR_VARDIYA')
                    },
                    {
                        label: 'Personel Karnesi',
                        data: { shortLabel: 'Personel' },
                        icon: 'pi pi-fw pi-users',
                        routerLink: ['/raporlar/personel'],
                        visible: hasAccess('RAPOR_PERSONEL')
                    },
                    {
                        label: 'Fark Raporu',
                        data: { shortLabel: 'Farklar' },
                        icon: 'pi pi-fw pi-exclamation-triangle',
                        routerLink: ['/raporlar/fark'],
                        visible: hasAccess('RAPOR_FARK')
                    },
                    {
                        label: 'İşlem Geçmişi',
                        data: { shortLabel: 'Geçmiş' },
                        icon: 'pi pi-fw pi-history',
                        routerLink: ['/raporlar/loglar'],
                        visible: hasAccess('VARDIYA_LOGLAR')
                    }
                ].filter(i => i.visible !== false)
            },
            {
                label: 'Tanımlamalar',
                items: [
                    {
                        label: 'Personel Tanımları',
                        data: { shortLabel: 'Personel Tnm' },
                        icon: 'pi pi-fw pi-user',
                        routerLink: ['/tanimlar/personel'],
                        visible: hasAccess('TANIMLAMA_PERSONEL')
                    },
                    {
                        label: 'Yakıt Tanımları',
                        data: { shortLabel: 'Yakıt Tnm' },
                        icon: 'pi pi-fw pi-filter',
                        routerLink: ['/tanimlar/yakit'],
                        visible: hasAccess('TANIMLAMA_YAKIT')
                    },
                    {
                        label: 'Sistem Tanımları',
                        data: { shortLabel: 'Sistem Tnm' },
                        icon: 'pi pi-fw pi-cog',
                        routerLink: ['/tanimlar/genel'],
                        visible: hasAccess('YONETIM_TANIMLAR')
                    }
                ].filter(i => i.visible !== false)
            },
            {
                label: 'Sistem Yönetimi',
                items: [
                    {
                        label: 'İstasyon Yönetimi',
                        data: { shortLabel: 'İstasyon' },
                        icon: 'pi pi-fw pi-building',
                        routerLink: ['/sistem/istasyon'],
                        visible: hasAccess('YONETIM_ISTASYON')
                    },
                    {
                        label: 'Kullanıcı Yönetimi',
                        data: { shortLabel: 'Kullanıcılar' },
                        icon: 'pi pi-fw pi-users',
                        routerLink: ['/sistem/kullanici'],
                        visible: hasAccess('YONETIM_KULLANICI')
                    },
                    {
                        label: 'Rol Yönetimi',
                        data: { shortLabel: 'Roller' },
                        icon: 'pi pi-fw pi-id-card',
                        routerLink: ['/sistem/roller'],
                        visible: hasAccess('SISTEM_ROLLER')
                    },
                    {
                        label: 'Bildirim Gönder',
                        data: { shortLabel: 'Bildirim' },
                        icon: 'pi pi-fw pi-send',
                        routerLink: ['/sistem/bildirimler'],
                        visible: hasAccess('SISTEM_BILDIRIM')
                    },
                    {
                        label: 'Yetki Yönetimi',
                        data: { shortLabel: 'Yetkiler' },
                        icon: 'pi pi-fw pi-lock',
                        routerLink: ['/sistem/yetki'],
                        visible: hasAccess('YONETIM_YETKI')
                    },
                    {
                        label: 'Sistem Ayarları',
                        data: { shortLabel: 'Ayarlar' },
                        icon: 'pi pi-fw pi-cog',
                        routerLink: ['/sistem/ayarlar'],
                        visible: true
                    }
                ].filter(i => i.visible !== false)
            }
        ];

        this.menuSubject.next(model);
    }
}
