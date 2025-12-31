import { Component, OnInit, HostListener, OnDestroy } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MenuModule } from 'primeng/menu';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { LayoutService } from '../service/layout.service';
import { AuthService, User, SimpleIstasyon } from '../../services/auth.service';
import { DashboardService } from '../../services/dashboard.service';
import { SorumluDashboardDto } from '../../models/dashboard.model';
import { NotificationService, Notification } from '../../services/notification.service';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-topbar',
    standalone: true,
    imports: [RouterModule, CommonModule, MenuModule, ButtonModule, SelectModule, FormsModule],
    template: `
<div class="layout-topbar">
    <div class="layout-topbar-logo-container">
        <button class="layout-menu-button layout-topbar-action" (click)="layoutService.onMenuToggle()">
            <i class="pi pi-bars"></i>
        </button>
        <a class="layout-topbar-logo" routerLink="/">
            <img src="assets/tshift.svg" alt="logo" style="height: 35px;">
        </a>
    </div>

    <!-- User Info Section -->
    <div class="layout-topbar-user-info" *ngIf="currentUser && !isAdmin">
        <div class="user-info-content">
            <div class="user-name">
                <i class="pi pi-user"></i>
                <span>{{currentUser.adSoyad || currentUser.username}}</span>
                <span class="user-role-badge">{{getRoleLabel(currentUser.role)}}</span>
            </div>
            
            <!-- Patron View: Firma Name and Station Dropdown -->
            <div class="user-station" *ngIf="isPatron">
                <i class="pi pi-building"></i>
                <span class="mr-2">{{currentUser.firmaAdi || 'Firma'}}</span>
                <p-select 
                    [options]="currentUser.istasyonlar || []" 
                    [(ngModel)]="selectedIstasyon" 
                    optionLabel="ad" 
                    placeholder="İstasyon Seçiniz"
                    (onChange)="onStationChange($event)"
                    styleClass="station-dropdown"
                    [style]="{'minWidth':'200px'}">
                </p-select>
            </div>

            <!-- Other Roles View: Single Station Name -->
            <div class="user-station" *ngIf="!isPatron && userInfo">
                <i class="pi pi-building"></i>
                <span>{{userInfo.istasyonAdi}}</span>
            </div>
        </div>
    </div>

    <!-- Mobile Context Info (Firma/Istasyon) -->
    <div class="mobile-context-info">
        <span class="context-text" *ngIf="isPatron">
            {{currentUser?.firmaAdi || 'Firma'}}
        </span>
        <span class="context-text" *ngIf="!isPatron">
            {{selectedIstasyon?.ad || userInfo?.istasyonAdi}}
        </span>
    </div>

    <div class="layout-topbar-actions">
        <!-- Notifications -->
        <div class="notification-wrapper">
            <button type="button" class="layout-topbar-action notification-button" (click)="toggleNotifications()">
                <i class="pi pi-bell" [class.has-notifications]="unreadNotifications > 0"></i>
                <span class="notification-badge" *ngIf="unreadNotifications > 0">{{unreadNotifications}}</span>
            </button>
            
            <div class="notification-panel" *ngIf="showNotifications">
                <div class="notification-container">
                    <div class="notification-header">
                        <span class="notification-title">Bildirimler</span>
                        <button pButton type="button" label="Tümünü Okundu İşaretle" 
                            class="p-button-text p-button-sm" 
                            (click)="markAllAsRead()"
                            *ngIf="unreadNotifications > 0"></button>
                    </div>
                    
                    <div class="notification-list">
                        <div *ngFor="let notification of notifications" 
                             class="notification-item"
                             [class.unread]="!notification.read"
                             (click)="markAsRead(notification.id)">
                            <div class="notification-icon" [ngClass]="notification.severity">
                                <i class="pi {{notification.icon}}"></i>
                            </div>
                            <div class="notification-content">
                                <div class="notification-title-text">{{notification.title}}</div>
                                <div class="notification-message">{{notification.message}}</div>
                                <div class="notification-time">{{notification.time}}</div>
                            </div>
                        </div>
                        
                        <div *ngIf="notifications.length === 0" class="notification-empty">
                            <i class="pi pi-inbox"></i>
                            <p>Bildirim yok</p>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- User Menu -->
        <div class="layout-topbar-menu">
            <div class="layout-topbar-menu-content">
                <button type="button" class="layout-topbar-action" (click)="menu.toggle($event)">
                    <i class="pi pi-user"></i>
                    <span>{{currentUser?.adSoyad || currentUser?.username || 'Profil'}}</span>
                </button>
                <p-menu #menu [model]="items" [popup]="true"></p-menu>
            </div>
        </div>
    </div>
</div>
    `,
    styleUrls: ['./app.topbar.scss']
})
export class AppTopbar implements OnInit, OnDestroy {
    items!: MenuItem[];
    currentUser: User | null = null;
    userInfo: SorumluDashboardDto | null = null;
    isAdmin: boolean = false;
    isPatron: boolean = false;
    selectedIstasyon: SimpleIstasyon | null = null;

    // Notifications
    notifications: Notification[] = [];
    unreadNotifications: number = 0;
    showNotifications: boolean = false;
    private notificationSubscription?: Subscription;
    private notificationsLoaded = false;

    constructor(
        public layoutService: LayoutService,
        private authService: AuthService,
        private router: Router,
        private dashboardService: DashboardService,
        private notificationService: NotificationService
    ) { }

    @HostListener('document:click', ['$event'])
    clickout(event: any) {
        const target = event.target as HTMLElement;
        const notificationWrapper = target.closest('.notification-wrapper');

        if (!notificationWrapper && this.showNotifications) {
            this.showNotifications = false;
        }
    }

    ngOnInit() {
        this.authService.currentUser$.subscribe(user => {
            this.currentUser = user;
            const role = user?.role?.toLowerCase();
            this.isAdmin = role === 'admin';
            this.isPatron = role === 'patron';

            if (!this.isAdmin && !this.isPatron && this.currentUser) {
                this.loadUserInfo();
            }
        });

        this.authService.selectedIstasyon$.subscribe(station => {
            this.selectedIstasyon = station;
        });

        // User menu items
        this.items = [
            {
                label: 'Profil',
                icon: 'pi pi-user',
                command: () => {
                    this.router.navigate(['/profile']);
                }
            },
            {
                separator: true
            },
            {
                label: 'Çıkış Yap',
                icon: 'pi pi-power-off',
                command: () => {
                    this.logout();
                }
            }
        ];

        // Subscribe to notifications
        this.notificationSubscription = this.notificationService.notifications$.subscribe(data => {
            this.notifications = data.notifications;
            this.unreadNotifications = data.unreadCount;
        });

        // Start polling for notifications
        this.notificationService.startPolling();
    }

    loadUserInfo() {
        this.dashboardService.getSorumluSummary().subscribe({
            next: (data) => {
                this.userInfo = data;
            },
            error: (err) => {
                console.error('Failed to load user info:', err);
            }
        });
    }

    onStationChange(event: any) {
        if (event.value) {
            this.authService.setSelectedIstasyon(event.value);
            // Reload current page or navigate to dashboard to refresh data
            const currentUrl = this.router.url;
            this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
                this.router.navigate([currentUrl]);
            });
        }
    }

    ngOnDestroy() {
        this.notificationSubscription?.unsubscribe();
        this.notificationService.stopPolling();
    }

    toggleNotifications() {
        this.showNotifications = !this.showNotifications;

        // İlk açılışta bildirimleri yükle (lazy loading)
        if (this.showNotifications && !this.notificationsLoaded) {
            this.notificationService.loadNotifications().subscribe(() => {
                this.notificationsLoaded = true;
            });
        }
    }

    markAsRead(id: number) {
        this.notificationService.markAsRead(id).subscribe();
    }

    markAllAsRead() {
        this.notificationService.markAllAsRead().subscribe();
    }

    logout() {
        this.authService.logout();
        this.router.navigate(['/auth/login']);
    }

    getRoleLabel(role: string): string {
        const roleLower = role?.toLowerCase();
        switch (roleLower) {
            case 'admin':
                return 'Admin';
            case 'patron':
                return 'Patron';
            case 'vardiya sorumlusu':
            case 'vardiya_sorumlusu':
                return 'Vardiya Sorumlusu';
            case 'market sorumlusu':
            case 'market_sorumlusu':
                return 'Market Sorumlusu';
            case 'istasyon sorumlusu':
            case 'istasyon_sorumlusu':
                return 'İstasyon Sorumlusu';
            case 'pompaci':
                return 'Pompacı';
            case 'market_gorevlisi':
            case 'market gorevlisi':
                return 'Market Görevlisi';
            default:
                return role;
        }
    }
}
