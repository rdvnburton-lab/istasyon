import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AppMenuitem } from './app.menuitem';
import { AuthService } from '../../services/auth.service';
import { PermissionService } from '../../services/permission.service';
import { Subscription } from 'rxjs';

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
export class AppMenu implements OnInit, OnDestroy {
    model: MenuItem[] = [];
    private subscription: Subscription = new Subscription();

    constructor(
        private authService: AuthService,
        private permissionService: PermissionService
    ) { }

    ngOnInit() {
        this.subscription = this.permissionService.permissions$.subscribe(() => {
            this.updateMenu();
        });
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    updateMenu() {
        const user = this.authService.getCurrentUser();
        const role = user?.role?.toLowerCase() || '';

        // Helper to check permission
        const hasAccess = (resource: string) => this.permissionService.hasAccess(role, resource);

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

        // Vardiya Yönetimi
        const vardiyaItems = [
            {
                label: 'Vardiya Listesi',
                icon: 'pi pi-fw pi-list',
                routerLink: ['/vardiya'],
                visible: hasAccess('VARDIYA_LISTESI')
            },
            {
                label: 'Pompa Yönetimi', // Genelde parametre ile gidilir ama menüde olması istenirse
                icon: 'pi pi-fw pi-filter',
                routerLink: ['/vardiya/pompa/0'], // Parametre gerektirir, menüden gizlenebilir veya genel bir sayfaya gidebilir
                visible: false // hasAccess('VARDIYA_POMPA') // Genelde listeden gidilir
            },
            {
                label: 'Market Mutabakatı',
                icon: 'pi pi-fw pi-shopping-bag',
                routerLink: ['/vardiya/market'],
                visible: hasAccess('VARDIYA_MARKET')
            },
            {
                label: 'Karşılaştırma Raporu',
                icon: 'pi pi-fw pi-chart-bar',
                routerLink: ['/vardiya/karsilastirma'],
                visible: hasAccess('VARDIYA_KARSILASTIRMA')
            }
        ].filter(item => item.visible !== false);

        if (vardiyaItems.length > 0) {
            this.model.push({
                label: 'Vardiya Yönetimi',
                items: vardiyaItems
            });
        }

        // Raporlar
        const raporItems = [
            {
                label: 'Vardiya Raporu',
                icon: 'pi pi-fw pi-file',
                routerLink: ['/vardiya/raporlar/vardiya'],
                visible: hasAccess('RAPOR_VARDIYA')
            },
            {
                label: 'Personel Karnesi',
                icon: 'pi pi-fw pi-users',
                routerLink: ['/vardiya/raporlar/personel'],
                visible: hasAccess('RAPOR_PERSONEL')
            },
            {
                label: 'Fark Raporu',
                icon: 'pi pi-fw pi-exclamation-triangle',
                routerLink: ['/vardiya/raporlar/fark'],
                visible: hasAccess('RAPOR_FARK')
            }
        ].filter(item => item.visible);

        if (raporItems.length > 0) {
            this.model.push({
                label: 'Raporlar',
                items: raporItems
            });
        }

        // Yönetim
        const yonetimItems = [
            {
                label: 'İşlem Geçmişi',
                icon: 'pi pi-fw pi-history',
                routerLink: ['/vardiya/loglar'],
                visible: hasAccess('VARDIYA_LOGLAR')
            },
            {
                label: 'Onay Bekleyenler',
                icon: 'pi pi-fw pi-check-circle',
                routerLink: ['/vardiya/onay-bekleyenler'],
                visible: hasAccess('VARDIYA_ONAY_BEKLEYENLER')
            },
            {
                label: 'Kullanıcı Yönetimi',
                icon: 'pi pi-fw pi-users',
                routerLink: ['/yonetim/kullanici'],
                visible: hasAccess('YONETIM_KULLANICI')
            },
            {
                label: 'Personel Tanımları',
                icon: 'pi pi-fw pi-user',
                routerLink: ['/vardiya/tanimlamalar/personel'],
                visible: hasAccess('TANIMLAMA_PERSONEL')
            },
        ].filter(item => item.visible);

        if (yonetimItems.length > 0) {
            this.model.push({
                label: 'Yönetim',
                items: yonetimItems
            });
        }

        // Sistem Yönetimi
        const sistemItems = [
            {
                label: 'İstasyon Yönetimi',
                icon: 'pi pi-fw pi-building',
                routerLink: ['/yonetim/istasyon'],
                visible: hasAccess('YONETIM_ISTASYON')
            },
            {
                label: 'Sistem Sağlığı',
                icon: 'pi pi-fw pi-heart-fill',
                routerLink: ['/admin/health'],
                visible: hasAccess('SISTEM_SAGLIK')
            },
            {
                label: 'Rol Yönetimi',
                icon: 'pi pi-fw pi-id-card',
                routerLink: ['/settings/roles'],
                visible: hasAccess('SISTEM_ROLLER')
            },
            {
                label: 'Bildirim Gönder',
                icon: 'pi pi-fw pi-send',
                routerLink: ['/admin/notifications'],
                visible: hasAccess('SISTEM_BILDIRIM')
            },
            {
                label: 'Yetki Yönetimi',
                icon: 'pi pi-fw pi-lock',
                routerLink: ['/yonetim/yetki'],
                visible: hasAccess('YONETIM_YETKI')
            },
            {
                label: 'Ayarlar',
                icon: 'pi pi-fw pi-cog',
                routerLink: ['/sistem/ayarlar'],
                visible: true
            }
        ].filter(item => item.visible);

        if (sistemItems.length > 0) {
            this.model.push({
                label: 'Sistem Yönetimi',
                items: sistemItems
            });
        }
    }
}
