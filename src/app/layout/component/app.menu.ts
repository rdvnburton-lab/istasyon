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
        const isAdminOrPatron = user?.role === 'admin' || user?.role === 'patron';

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
            },
            {
                label: 'Vardiya Yönetimi',
                items: [
                    ...(user?.role !== 'market_sorumlusu' ? [{
                        label: 'Vardiya Listesi',
                        icon: 'pi pi-fw pi-list',
                        routerLink: ['/vardiya']
                    }] : []),
                    ...(user?.role !== 'vardiya_sorumlusu' ? [{
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
            },
            {
                label: 'Raporlar',
                items: [
                    {
                        label: 'Vardiya Raporu',
                        icon: 'pi pi-fw pi-file',
                        routerLink: ['/vardiya/raporlar/vardiya']
                    },
                    ...(user?.role !== 'market_sorumlusu' ? [
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
            },
            {
                label: 'Yönetim',
                items: [
                    ...(isAdminOrPatron ? [
                        {
                            label: 'İşlem Geçmişi',
                            icon: 'pi pi-fw pi-history',
                            routerLink: ['/vardiya/loglar']
                        },
                        {
                            label: 'Onay Bekleyenler',
                            icon: 'pi pi-fw pi-check-circle',
                            routerLink: ['/vardiya/onay-bekleyenler']
                        },
                        {
                            label: 'Kullanıcı Yönetimi',
                            icon: 'pi pi-fw pi-users',
                            routerLink: ['/yonetim/kullanici']
                        }
                    ] : []),
                    {
                        label: 'Personel Tanımları',
                        icon: 'pi pi-fw pi-user',
                        routerLink: ['/vardiya/tanimlamalar/personel']
                    }
                ]
            },
            {
                label: 'Sistem',
                items: [
                    {
                        label: 'Ayarlar',
                        icon: 'pi pi-fw pi-cog',
                        routerLink: ['/sistem/ayarlar']
                    }
                ]
            }
        ];

        if (isAdminOrPatron) {
            this.model.splice(3, 0, {
                label: 'Admin Paneli',
                items: [
                    {
                        label: 'İstasyon Yönetimi',
                        icon: 'pi pi-fw pi-building',
                        routerLink: ['/admin/istasyonlar']
                    }
                ]
            });
        }
    }
}
