import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-menu',
    standalone: true,
    imports: [CommonModule, AppMenuitem, RouterModule],
    template: `<ul class="layout-menu">
        <ng-container *ngFor="let item of model; let i = index">
            <li app-menuitem *ngIf="!item.separator" [item]="item" [index]="i" [root]="true"></li>
            <li *ngIf="item.separator" class="menu-separator"></li>
        </ng-container>
    </ul> `
})
export class AppMenu {
    model: MenuItem[] = [];

    constructor(private authService: AuthService) { }

    ngOnInit() {
        const user = this.authService.getCurrentUser();
        const isAdmin = user?.role === 'admin';
        const isPatron = user?.role === 'patron';
        const isAdminOrPatron = isAdmin || isPatron;

        this.model = [
            {
                label: 'Ana Menü',
                items: [
                    {
                        label: 'Anasayfa',
                        icon: 'pi pi-fw pi-home',
                        routerLink: ['/dashboard']
                    }
                ]
            }
        ];

        // Vardiya ve Raporlar (Admin görmez)
        if (!isAdmin) {
            const isIstasyonSorumlusu = user?.role === 'istasyon sorumlusu';
            const isVardiyaSorumlusu = user?.role === 'vardiya sorumlusu';
            const isMarketSorumlusu = user?.role === 'market sorumlusu';

            this.model.push({
                label: 'Vardiya Yönetimi',
                items: [
                    // İstasyon sorumlusu ve vardiya sorumlusu vardiya listesini görebilir
                    ...(!isMarketSorumlusu ? [{
                        label: 'Vardiya Listesi',
                        icon: 'pi pi-fw pi-list',
                        routerLink: ['/vardiya']
                    }] : []),
                    // İstasyon sorumlusu ve market sorumlusu market mutabakatını görebilir
                    ...(!isVardiyaSorumlusu ? [{
                        label: 'Market Mutabakatı',
                        icon: 'pi pi-fw pi-shopping-bag',
                        routerLink: ['/vardiya/market']
                    }] : []),
                    {
                        label: 'Karşılaştırma Raporu',
                        icon: 'pi pi-fw pi-chart-bar',
                        routerLink: ['/vardiya/karsilastirma']
                    }
                ]
            });


            this.model.push({
                label: 'Raporlar',
                items: [
                    {
                        label: 'Vardiya Raporu',
                        icon: 'pi pi-fw pi-file',
                        routerLink: ['/vardiya/raporlar/vardiya']
                    },
                    ...(!isMarketSorumlusu ? [
                        {
                            label: 'Personel Karnesi',
                            icon: 'pi pi-fw pi-users',
                            routerLink: ['/vardiya/raporlar/personel']
                        },
                        {
                            label: 'Fark Raporu',
                            icon: 'pi pi-fw pi-exclamation-triangle',
                            routerLink: ['/vardiya/raporlar/fark']
                        }
                    ] : [])
                ]
            });
        }

        // Yönetim Menüsü
        const yonetimItems = [];
        if (isAdminOrPatron) {
            yonetimItems.push({
                label: 'İşlem Geçmişi',
                icon: 'pi pi-fw pi-history',
                routerLink: ['/vardiya/loglar']
            });

            if (isPatron) {
                yonetimItems.push({
                    label: 'Onay Bekleyenler',
                    icon: 'pi pi-fw pi-check-circle',
                    routerLink: ['/vardiya/onay-bekleyenler']
                });
            }

            yonetimItems.push({
                label: 'Kullanıcı Yönetimi',
                icon: 'pi pi-fw pi-users',
                routerLink: ['/yonetim/kullanici']
            });
        }

        if (!isAdmin) {
            yonetimItems.push({
                label: 'Personel Tanımları',
                icon: 'pi pi-fw pi-user',
                routerLink: ['/vardiya/tanimlamalar/personel']
            });
        }

        if (yonetimItems.length > 0) {
            this.model.push({
                label: 'Yönetim',
                items: yonetimItems
            });
        }

        // Admin Paneli (Sadece Admin ve Patron)
        if (isAdminOrPatron) {
            this.model.push({
                label: 'Sistem Yönetimi',
                items: [
                    {
                        label: 'İstasyon Yönetimi',
                        icon: 'pi pi-fw pi-building',
                        routerLink: ['/admin/istasyonlar']
                    },
                    {
                        label: 'Sistem Sağlığı',
                        icon: 'pi pi-fw pi-heart-fill',
                        routerLink: ['/admin/health'],
                        visible: isAdmin
                    },
                    {
                        label: 'Rol Yönetimi',
                        icon: 'pi pi-fw pi-id-card',
                        routerLink: ['/settings/roles'],
                        visible: isAdmin
                    },
                    {
                        label: 'Bildirim Gönder',
                        icon: 'pi pi-fw pi-send',
                        routerLink: ['/admin/notifications'],
                        visible: isAdmin
                    },
                    {
                        label: 'Ayarlar',
                        icon: 'pi pi-fw pi-cog',
                        routerLink: ['/sistem/ayarlar']
                    }
                ]
            });
        }
    }
}
