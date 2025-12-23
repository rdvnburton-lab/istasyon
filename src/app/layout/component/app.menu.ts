import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';

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

    ngOnInit() {
        this.model = [
            {
                label: 'Vardiya Yönetimi',
                items: [
                    {
                        label: 'Vardiya Listesi',
                        icon: 'pi pi-fw pi-list',
                        routerLink: ['/vardiya']
                    },
                    {
                        label: 'Market Mutabakatı',
                        icon: 'pi pi-fw pi-shopping-bag',
                        routerLink: ['/vardiya/market']
                    },
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
                ]
            },
            {
                label: 'Yönetim',
                items: [
                    {
                        label: 'Onay Bekleyenler',
                        icon: 'pi pi-fw pi-check-circle',
                        routerLink: ['/vardiya/onay-bekleyenler']
                    },
                    {
                        label: 'Tanımlamalar',
                        icon: 'pi pi-fw pi-cog',
                        items: [
                            {
                                label: 'İstasyon Tanımları',
                                icon: 'pi pi-fw pi-building',
                                routerLink: ['/vardiya/tanimlamalar/istasyon']
                            },
                            {
                                label: 'Personel Tanımları',
                                icon: 'pi pi-fw pi-user',
                                routerLink: ['/vardiya/tanimlamalar/personel']
                            }
                        ]
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
    }
}
