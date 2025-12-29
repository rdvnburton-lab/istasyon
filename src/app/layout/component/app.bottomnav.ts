import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService, User } from '../../services/auth.service';

interface BottomNavItem {
    label: string;
    icon: string;
    route?: string;
    action?: () => void;
}

@Component({
    selector: 'app-bottom-nav',
    standalone: true,
    imports: [CommonModule, RouterModule],
    template: `
        <ng-container *ngIf="isVisible">
            <nav class="mobile-bottom-nav">
                <ng-container *ngFor="let item of navItems">
                    <a *ngIf="item.route" 
                       [routerLink]="item.route" 
                       routerLinkActive="active"
                       [routerLinkActiveOptions]="{exact: item.route === '/dashboard'}"
                       class="nav-item">
                        <i [class]="'pi ' + item.icon"></i>
                        <span>{{ item.label }}</span>
                    </a>
                    <button *ngIf="item.action" 
                            (click)="item.action()"
                            class="nav-item">
                        <i [class]="'pi ' + item.icon"></i>
                        <span>{{ item.label }}</span>
                    </button>
                </ng-container>
            </nav>

            <!-- More Menu Drawer -->
            <div class="profile-drawer" [class.open]="showProfileDrawer" (click)="closeDrawer($event)">
                <div class="drawer-content" (click)="$event.stopPropagation()">
                    <!-- Handle bar -->
                    <div class="drawer-handle"><div class="handle-bar"></div></div>
                    
                    <!-- User Profile Section -->
                    <div class="drawer-header" (click)="goToProfile()">
                        <div class="user-avatar">
                            <i class="pi pi-user"></i>
                        </div>
                        <div class="user-info">
                            <div class="user-name">{{ currentUser?.username }}</div>
                            <div class="user-role">{{ getRoleLabel(currentUser?.role) }}</div>
                        </div>
                        <div class="profile-arrow">
                            <i class="pi pi-chevron-right"></i>
                        </div>
                    </div>

                    <!-- Vardiya Section -->
                    <div class="drawer-section">
                        <div class="section-title">Vardiya İşlemleri</div>
                        <div class="drawer-menu">
                            <a class="drawer-item" routerLink="/vardiya" (click)="showProfileDrawer = false">
                                <i class="pi pi-list"></i>
                                <span>Vardiya Listesi</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                            <a class="drawer-item" routerLink="/vardiya/raporlar/vardiya" (click)="showProfileDrawer = false">
                                <i class="pi pi-chart-bar"></i>
                                <span>Raporlar</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                            <a class="drawer-item" routerLink="/vardiya/tanimlamalar/personel" (click)="showProfileDrawer = false">
                                <i class="pi pi-users"></i>
                                <span>Personel Tanımları</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                        </div>
                    </div>

                    <!-- Management Section -->
                    <div class="drawer-section">
                        <div class="section-title">Yönetim</div>
                        <div class="drawer-menu">
                            <a class="drawer-item" routerLink="/admin/istasyonlar" (click)="showProfileDrawer = false">
                                <i class="pi pi-building"></i>
                                <span>Firma & İstasyonlar</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                            <a class="drawer-item" routerLink="/market/yonetim" (click)="showProfileDrawer = false">
                                <i class="pi pi-shopping-cart"></i>
                                <span>Market Yönetimi</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                            <a class="drawer-item" routerLink="/vardiya/tanimlamalar/istasyon" (click)="showProfileDrawer = false">
                                <i class="pi pi-map-marker"></i>
                                <span>İstasyon Tanımları</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                        </div>
                    </div>

                    <!-- Settings Section -->
                    <div class="drawer-section">
                        <div class="section-title">Ayarlar</div>
                        <div class="drawer-menu">
                            <a class="drawer-item" routerLink="/sistem/ayarlar" (click)="showProfileDrawer = false">
                                <i class="pi pi-cog"></i>
                                <span>Uygulama Ayarları</span>
                                <i class="pi pi-chevron-right arrow"></i>
                            </a>
                            <button class="drawer-item logout" (click)="logout()">
                                <i class="pi pi-sign-out"></i>
                                <span>Çıkış Yap</span>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </ng-container>
    `,
    styles: [`
        .mobile-bottom-nav {
            display: none;
        }

        @media screen and (max-width: 991px) {
            .mobile-bottom-nav {
                display: flex;
                position: fixed;
                bottom: 0;
                left: 0;
                right: 0;
                height: calc(65px + env(safe-area-inset-bottom));
                padding-bottom: env(safe-area-inset-bottom);
                background: var(--surface-card);
                border-top: 1px solid var(--surface-border);
                box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.08);
                z-index: 1000;
                justify-content: space-around;
                align-items: center;
            }

            .nav-item {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                flex: 1;
                height: 100%;
                color: var(--text-color-secondary);
                text-decoration: none;
                background: none;
                border: none;
                cursor: pointer;
                transition: color 0.2s ease;
                gap: 4px;
                padding: 8px 0;

                i {
                    font-size: 1.25rem;
                }

                span {
                    font-size: 0.7rem;
                    font-weight: 500;
                }

                &.active {
                    color: var(--primary-color);
                }

                &:active {
                    opacity: 0.7;
                }
            }

            .profile-drawer {
                display: none;
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(0, 0, 0, 0.5);
                z-index: 1001;
                
                &.open {
                    display: block;
                }
            }

            .drawer-content {
                position: absolute;
                bottom: 0;
                left: 0;
                right: 0;
                background: var(--surface-card);
                border-top-left-radius: 20px;
                border-top-right-radius: 20px;
                padding-bottom: env(safe-area-inset-bottom);
                animation: slideUp 0.3s ease;
            }

            @keyframes slideUp {
                from { transform: translateY(100%); }
                to { transform: translateY(0); }
            }

            .drawer-header {
                display: flex;
                align-items: center;
                gap: 1rem;
                padding: 1.5rem;
                border-bottom: 1px solid var(--surface-border);
                cursor: pointer;
                transition: background 0.2s ease;

                &:active {
                    background: var(--surface-hover);
                }
            }

            .user-avatar {
                width: 48px;
                height: 48px;
                border-radius: 50%;
                background: var(--primary-100);
                display: flex;
                align-items: center;
                justify-content: center;
                
                i {
                    font-size: 1.5rem;
                    color: var(--primary-color);
                }
            }

            .user-info {
                flex: 1;
            }

            .user-name {
                font-weight: 700;
                font-size: 1.1rem;
                color: var(--text-color);
            }

            .user-role {
                font-size: 0.875rem;
                color: var(--text-color-secondary);
            }

            .drawer-menu {
                padding: 0.5rem 0;
            }

            .drawer-item {
                display: flex;
                align-items: center;
                gap: 1rem;
                padding: 1rem 1.5rem;
                color: var(--text-color);
                text-decoration: none;
                background: none;
                border: none;
                width: 100%;
                cursor: pointer;
                font-size: 1rem;
                
                i {
                    font-size: 1.25rem;
                    width: 24px;
                    color: var(--text-color-secondary);
                }
                
                &:active {
                    background: var(--surface-hover);
                }
                
                &.logout {
                    color: #ef4444;
                    i { color: #ef4444; }
                }

                .arrow {
                    margin-left: auto;
                    font-size: 0.75rem;
                    color: var(--text-color-secondary);
                    opacity: 0.5;
                }
            }

            .drawer-handle {
                display: flex;
                justify-content: center;
                padding: 0.75rem 0 0.5rem;
            }

            .handle-bar {
                width: 40px;
                height: 4px;
                background: var(--surface-border);
                border-radius: 2px;
            }

            .drawer-section {
                padding: 0.5rem 0;
            }

            .section-title {
                font-size: 0.7rem;
                font-weight: 600;
                color: var(--text-color-secondary);
                text-transform: uppercase;
                letter-spacing: 0.5px;
                padding: 0.5rem 1.5rem;
            }

            .profile-arrow {
                color: var(--text-color-secondary);
                opacity: 0.5;
                i { font-size: 0.875rem; }
            }
        }
    `]
})
export class AppBottomNav implements OnInit {
    navItems: BottomNavItem[] = [];
    isVisible = false;
    showProfileDrawer = false;
    currentUser: User | null = null;

    constructor(private authService: AuthService, private router: Router) { }

    ngOnInit() {
        this.currentUser = this.authService.getCurrentUser();
        if (!this.currentUser) {
            this.isVisible = false;
            return;
        }

        const isPatron = this.currentUser.role === 'patron';
        const isAdmin = this.currentUser.role === 'admin';

        if (isAdmin) {
            this.isVisible = false;
            return;
        }

        this.isVisible = true;

        if (isPatron) {
            this.navItems = [
                { label: 'Özet', icon: 'pi-home', route: '/dashboard' },
                { label: 'Onaylar', icon: 'pi-check-circle', route: '/vardiya/onay-bekleyenler' },
                { label: 'İşlemler', icon: 'pi-history', route: '/vardiya/loglar' },
                { label: 'İstasyonlar', icon: 'pi-building', route: '/admin/istasyonlar' },
                {
                    label: 'Diğer',
                    icon: 'pi-ellipsis-h',
                    action: () => this.showProfileDrawer = true
                }
            ];
        } else {
            this.navItems = [
                { label: 'Anasayfa', icon: 'pi-home', route: '/dashboard' },
                { label: 'Vardiyalar', icon: 'pi-list', route: '/vardiya' },
                { label: 'Raporlar', icon: 'pi-chart-bar', route: '/vardiya/raporlar/vardiya' },
                {
                    label: 'Diğer',
                    icon: 'pi-ellipsis-h',
                    action: () => this.showProfileDrawer = true
                }
            ];
        }
    }

    closeDrawer(event: Event) {
        this.showProfileDrawer = false;
    }

    getRoleLabel(role: string | undefined): string {
        const labels: { [key: string]: string } = {
            'patron': 'Patron',
            'istasyon sorumlusu': 'İstasyon Sorumlusu',
            'vardiya sorumlusu': 'Vardiya Sorumlusu',
            'market sorumlusu': 'Market Sorumlusu'
        };
        return role ? labels[role] || role : '';
    }

    logout() {
        this.showProfileDrawer = false;
        this.authService.logout();
        this.router.navigate(['/auth/login']);
    }

    goToProfile() {
        this.showProfileDrawer = false;
        this.router.navigate(['/profile']);
    }
}
