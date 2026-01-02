import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService, User } from '../../services/auth.service';
import { PermissionService } from '../../services/permission.service';
import { SettingsService } from '../../services/settings.service';
import { Subscription } from 'rxjs';

interface BottomNavItem {
    label: string;
    icon: string;
    route?: string;
    action?: () => void;
}

interface DrawerSection {
    label: string;
    items: DrawerItem[];
}

interface DrawerItem {
    label: string;
    icon: string;
    route?: string;
    action?: () => void;
}

import { AppMenuService } from '../service/app.menu.service';

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
                       [routerLinkActiveOptions]="{exact: true}"
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
                <div class="drawer-content" 
                     [class.dragging]="isDragging"
                     [style.transform]="drawerTransform"
                     (click)="$event.stopPropagation()"
                     (touchstart)="onTouchStart($event)"
                     (touchmove)="onTouchMove($event)"
                     (touchend)="onTouchEnd($event)">
                    <!-- Handle bar -->
                    <div class="drawer-handle"><div class="handle-bar"></div></div>
                    
                    <!-- User Profile Section -->
                    <div class="drawer-header" (click)="goToProfile()">
                        <div class="user-avatar" [style.background]="getAvatarColor(currentUser?.role)">
                            <i class="pi pi-user"></i>
                        </div>
                        <div class="user-info">
                            <div class="user-name">{{ currentUser?.adSoyad || currentUser?.username }}</div>
                            <div class="user-role">{{ getRoleLabel(currentUser?.role) }}</div>
                        </div>
                        <div class="profile-arrow">
                            <i class="pi pi-chevron-right"></i>
                        </div>
                    </div>

                    <div class="drawer-scroll-area">
                        <!-- Dynamic Sections -->
                        <div class="drawer-section" *ngFor="let section of drawerSections">
                            <div class="section-title">{{ section.label }}</div>
                            <div class="drawer-menu">
                                <ng-container *ngFor="let item of section.items">
                                    <a *ngIf="item.route" class="drawer-item" [routerLink]="item.route" (click)="showProfileDrawer = false">
                                        <i [class]="'pi ' + item.icon"></i>
                                        <span>{{ item.label }}</span>
                                        <i class="pi pi-chevron-right arrow"></i>
                                    </a>
                                    <button *ngIf="item.action" class="drawer-item" (click)="item.action()">
                                        <i [class]="'pi ' + item.icon"></i>
                                        <span>{{ item.label }}</span>
                                        <i class="pi pi-chevron-right arrow"></i>
                                    </button>
                                </ng-container>
                            </div>
                        </div>

                        <!-- Logout Section (Always present) -->
                        <div class="drawer-section">
                            <div class="section-title">Hesap</div>
                            <div class="drawer-menu">
                                <button class="drawer-item logout" (click)="logout()">
                                    <i class="pi pi-sign-out"></i>
                                    <span>Çıkış Yap</span>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </ng-container>
    `,
    styles: [`
        .mobile-bottom-nav,
        .profile-drawer {
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
                max-height: 85vh;
                display: flex;
                flex-direction: column;
                transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                will-change: transform;

                &.dragging {
                    transition: none;
                }
            }

            .drawer-scroll-area {
                overflow-y: auto;
                flex: 1;
                -webkit-overflow-scrolling: touch;
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
                cursor: grab;
                touch-action: none;
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
export class AppBottomNav implements OnInit, OnDestroy {
    navItems: BottomNavItem[] = [];
    drawerSections: DrawerSection[] = [];
    isVisible = false;
    showProfileDrawer = false;
    currentUser: User | null = null;
    private subscription: Subscription = new Subscription();

    // Swipe properties
    private startY = 0;
    private currentDeltaY = 0;
    isDragging = false;
    drawerTransform = '';

    constructor(
        private authService: AuthService,
        private router: Router,
        private permissionService: PermissionService,
        private settingsService: SettingsService,
        private appMenuService: AppMenuService
    ) { }

    ngOnInit() {
        this.subscription = this.permissionService.permissions$.subscribe(() => {
            this.updateMenu();
        });

        // Listen to settings changes too (e.g. if user updates menu in Ayarlar)
        this.settingsService.settings$.subscribe(() => {
            this.updateMenu();
        });

        this.updateMenu();
    }

    ngOnDestroy() {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    updateMenu() {
        this.currentUser = this.authService.getCurrentUser();
        if (!this.currentUser) {
            this.isVisible = false;
            return;
        }

        const role = this.currentUser.role?.toLowerCase() || '';
        // Admin check removed to allow mobile menu
        // if (role === 'admin') {
        //     this.isVisible = false;
        //     return;
        // }

        this.isVisible = true;

        // Subscribe to menu service
        this.appMenuService.menu$.subscribe((menuModel: any[]) => {
            // Flatten the menu for easier processing
            // Level 0: Group (e.g. 'Operasyonlar')
            // Level 1: Items (e.g. 'Vardiya Listesi')

            // Map to internal format
            const allItems: any[] = [];

            menuModel.forEach((group: any) => {
                if (group.items) {
                    group.items.forEach((item: any) => {
                        // Use short label for bottom nav if available
                        const shortLabel = item.data?.shortLabel || item.label;
                        const route = item.routerLink ? item.routerLink[0] : null;

                        allItems.push({
                            label: shortLabel, // Short label for bottom nav
                            drawerLabel: item.label, // Full label for drawer
                            icon: item.icon ? item.icon.replace('pi pi-fw ', '') : '', // clean up icon class
                            route: route,
                            visible: item.visible !== false,
                            group: group.label
                        });
                    });
                }
            });

            // Filter items accessible by the user (already filtered in service, but check visible flag)
            const accessibleItems = allItems.filter(i => i.visible);

            // Get user preferences
            let userMenuRoutes = this.settingsService.getMobileMenu();

            if (!userMenuRoutes || userMenuRoutes.length === 0) {
                // Smart defaults based on role if no custom setting
                userMenuRoutes = ['/dashboard'];
                // Hardcoded defaults based on what's available
                if (accessibleItems.find(i => i.route === '/operasyon')) userMenuRoutes.push('/operasyon');
                if (accessibleItems.find(i => i.route === '/raporlar/onay-bekleyenler')) userMenuRoutes.push('/raporlar/onay-bekleyenler');
                if (accessibleItems.find(i => i.route === '/raporlar/vardiya') && userMenuRoutes.length < 4) userMenuRoutes.push('/raporlar/vardiya');
            }

            // Limit to 4 checks done in settings
            const preferredRoutes = userMenuRoutes.slice(0, 4);

            // 1. POPULATE BOTTOM NAV 
            this.navItems = [];

            preferredRoutes.forEach(route => {
                const item = accessibleItems.find(i => i.route === route);
                if (item) {
                    this.navItems.push({ label: item.label, icon: item.icon, route: item.route });
                }
            });

            // Always add 'Diğer'
            this.navItems.push({
                label: 'Diğer',
                icon: 'pi-ellipsis-h',
                action: () => {
                    this.showProfileDrawer = true;
                    this.drawerTransform = 'translateY(0)';
                }
            });

            // 2. POPULATE DRAWER
            const bottomNavPacket = this.navItems.map(i => i.route);
            const drawerItems = accessibleItems.filter(i => !bottomNavPacket.includes(i.route));

            this.drawerSections = [];
            // Preserve the order from the service which is naturally logical
            // Group by group name
            const groups: { [key: string]: any[] } = {};
            drawerItems.forEach(item => {
                if (!groups[item.group]) {
                    groups[item.group] = [];
                }
                groups[item.group].push(item);
            });

            // Iterate through original service groups to maintain order
            menuModel.forEach((groupModel: any) => {
                if (groups[groupModel.label!] && groups[groupModel.label!].length > 0) {
                    this.drawerSections.push({
                        label: groupModel.label!,
                        items: groups[groupModel.label!].map(i => ({
                            label: i.drawerLabel,
                            icon: i.icon,
                            route: i.route
                        }))
                    });
                }
            });

            // Always add Ayarlar (if not already in menu model, currently it is but kept separate in drawer usually?)
            // Actually 'Sistem Ayarları' is in the 'Sistem Yönetimi' group in AppMenuService.
            // But if it was moved to BottomNav, it shouldn't show up here twice.
            // Our logic handles that.
        });
    }

    // Touch handlers for swipe to close
    onTouchStart(event: TouchEvent) {
        event.stopPropagation();
        this.startY = event.touches[0].clientY;
        this.isDragging = true;
    }

    onTouchMove(event: TouchEvent) {
        event.stopPropagation();
        if (!this.isDragging) return;

        const currentY = event.touches[0].clientY;
        this.currentDeltaY = currentY - this.startY;

        // Only allow pulling down (deltaY > 0)
        if (this.currentDeltaY > 0) {
            this.drawerTransform = `translateY(${this.currentDeltaY}px)`;
            // Prevent body scroll
            event.preventDefault();
        } else {
            this.drawerTransform = 'translateY(0)';
            this.currentDeltaY = 0;
        }
    }

    onTouchEnd(event: TouchEvent) {
        event.stopPropagation();
        this.isDragging = false;

        // If pulled down more than 100px, close it
        if (this.currentDeltaY > 100) {
            this.closeDrawer(event);
        } else {
            // Reset position
            this.drawerTransform = 'translateY(0)';
        }
        this.currentDeltaY = 0;
    }

    closeDrawer(event: Event) {
        this.showProfileDrawer = false;
        this.drawerTransform = 'translateY(100%)';
    }

    getAvatarColor(role: string | undefined): string {
        if (!role) return 'var(--primary-100)';
        const r = role.toLowerCase();
        if (r === 'patron') return 'var(--purple-100)';
        if (r === 'istasyon sorumlusu') return 'var(--blue-100)';
        if (r === 'market sorumlusu') return 'var(--green-100)';
        if (r === 'vardiya sorumlusu') return 'var(--orange-100)';
        return 'var(--primary-100)';
    }

    getRoleLabel(role: string | undefined): string {
        if (!role) return '';
        const lowerRole = role.toLowerCase();
        const labels: { [key: string]: string } = {
            'patron': 'Patron',
            'istasyon sorumlusu': 'İstasyon Sorumlusu',
            'vardiya sorumlusu': 'Vardiya Sorumlusu',
            'market sorumlusu': 'Market Sorumlusu',
            'admin': 'Admin'
        };
        return labels[lowerRole] || role;
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
