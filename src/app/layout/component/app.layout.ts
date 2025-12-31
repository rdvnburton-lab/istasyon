import { Component, Renderer2, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { filter, Subscription } from 'rxjs';
import { AppTopbar } from './app.topbar';
import { AppSidebar } from './app.sidebar';
import { AppFooter } from './app.footer';
import { AppBottomNav } from './app.bottomnav';
import { LayoutService } from '../service/layout.service';
import { NotificationService } from '../../services/notification.service';

@Component({
    selector: 'app-layout',
    standalone: true,
    imports: [CommonModule, AppTopbar, AppSidebar, RouterModule, AppFooter, AppBottomNav],
    template: `<div class="layout-wrapper" [ngClass]="containerClass">
        <app-topbar></app-topbar>
        <app-sidebar></app-sidebar>
        
        <!-- Pull to Refresh Indicator -->
        <div class="pull-to-refresh-indicator" [style.transform]="refreshTransform" [class.refreshing]="isRefreshing" [class.active]="pullDeltaY > 20">
            <i class="pi" [ngClass]="isRefreshing ? 'pi-spin pi-spinner' : 'pi-sync'" [style.transform]="'rotate(' + (pullDeltaY * 2) + 'deg)'"></i>
        </div>

        <div class="layout-main-container" 
             (touchstart)="onTouchStart($event)" 
             (touchmove)="onTouchMove($event)" 
             (touchend)="onTouchEnd($event)">
            <div class="layout-main">
                <router-outlet></router-outlet>
            </div>
            <app-footer></app-footer>
        </div>
        <div class="layout-mask animate-fadein"></div>
        <app-bottom-nav></app-bottom-nav>
    </div> `,
    styles: [`
        .pull-to-refresh-indicator {
            position: fixed;
            top: 60px;
            left: 50%;
            transform: translateX(-50%) translateY(-100px);
            width: 40px;
            height: 40px;
            background: var(--primary-color);
            color: var(--primary-color-text);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1001;
            box-shadow: 0 4px 10px rgba(0,0,0,0.2);
            transition: transform 0.2s cubic-bezier(0, 0, 0.2, 1), opacity 0.2s;
            opacity: 0;
            pointer-events: none;

            &.active {
                opacity: 1;
            }

            &.refreshing {
                transform: translateX(-50%) translateY(20px) !important;
                opacity: 1;
            }

            i {
                font-size: 1.25rem;
            }
        }

        @media (min-width: 992px) {
            .pull-to-refresh-indicator {
                display: none;
            }
        }
    `]
})
export class AppLayout {
    overlayMenuOpenSubscription: Subscription;

    menuOutsideClickListener: any;

    @ViewChild(AppSidebar) appSidebar!: AppSidebar;

    @ViewChild(AppTopbar) appTopBar!: AppTopbar;

    // Pull to refresh properties
    private startY = 0;
    pullDeltaY = 0;
    isRefreshing = false;
    refreshTransform = '';
    private isPulling = false;

    constructor(
        public layoutService: LayoutService,
        public renderer: Renderer2,
        public router: Router,
        private notificationService: NotificationService
    ) {
        this.notificationService.initPush();

        this.overlayMenuOpenSubscription = this.layoutService.overlayOpen$.subscribe(() => {
            if (!this.menuOutsideClickListener) {
                this.menuOutsideClickListener = this.renderer.listen('document', 'click', (event) => {
                    if (this.isOutsideClicked(event)) {
                        this.hideMenu();
                    }
                });
            }

            if (this.layoutService.layoutState().staticMenuMobileActive) {
                this.blockBodyScroll();
            }
        });

        this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
            this.hideMenu();
        });
    }

    // Pull to refresh handlers
    onTouchStart(event: TouchEvent) {
        if (window.scrollY === 0) {
            this.startY = event.touches[0].clientY;
            this.isPulling = true;
        }
    }

    onTouchMove(event: TouchEvent) {
        if (!this.isPulling || this.isRefreshing) return;

        const currentY = event.touches[0].clientY;
        const delta = currentY - this.startY;

        if (delta > 0 && window.scrollY === 0) {
            // Resistance effect
            this.pullDeltaY = Math.min(delta * 0.4, 80);
            this.refreshTransform = `translateX(-50%) translateY(${this.pullDeltaY - 60}px)`;

            // If pulling down significantly, prevent scroll
            if (this.pullDeltaY > 10) {
                if (event.cancelable) event.preventDefault();
            }
        } else {
            this.isPulling = false;
            this.pullDeltaY = 0;
            this.refreshTransform = '';
        }
    }

    onTouchEnd(event: TouchEvent) {
        if (!this.isPulling) return;

        if (this.pullDeltaY >= 60) {
            this.triggerRefresh();
        } else {
            this.resetPull();
        }

        this.isPulling = false;
    }

    private triggerRefresh() {
        this.isRefreshing = true;
        this.refreshTransform = 'translateX(-50%) translateY(20px)';

        // Short delay for visual feedback before reload
        setTimeout(() => {
            window.location.reload();
        }, 800);
    }

    private resetPull() {
        this.pullDeltaY = 0;
        this.refreshTransform = '';
    }

    isOutsideClicked(event: MouseEvent) {
        const sidebarEl = document.querySelector('.layout-sidebar');
        const topbarEl = document.querySelector('.layout-menu-button');
        const eventTarget = event.target as Node;

        return !(sidebarEl?.isSameNode(eventTarget) || sidebarEl?.contains(eventTarget) || topbarEl?.isSameNode(eventTarget) || topbarEl?.contains(eventTarget));
    }

    hideMenu() {
        this.layoutService.layoutState.update((prev) => ({ ...prev, overlayMenuActive: false, staticMenuMobileActive: false, menuHoverActive: false }));
        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
            this.menuOutsideClickListener = null;
        }
        this.unblockBodyScroll();
    }

    blockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.add('blocked-scroll');
        } else {
            document.body.className += ' blocked-scroll';
        }
    }

    unblockBodyScroll(): void {
        if (document.body.classList) {
            document.body.classList.remove('blocked-scroll');
        } else {
            document.body.className = document.body.className.replace(new RegExp('(^|\\b)' + 'blocked-scroll'.split(' ').join('|') + '(\\b|$)', 'gi'), ' ');
        }
    }

    get containerClass() {
        return {
            'layout-overlay': this.layoutService.layoutConfig().menuMode === 'overlay',
            'layout-static': this.layoutService.layoutConfig().menuMode === 'static',
            'layout-static-inactive': this.layoutService.layoutState().staticMenuDesktopInactive && this.layoutService.layoutConfig().menuMode === 'static',
            'layout-overlay-active': this.layoutService.layoutState().overlayMenuActive,
            'layout-mobile-active': this.layoutService.layoutState().staticMenuMobileActive
        };
    }

    ngOnDestroy() {
        if (this.overlayMenuOpenSubscription) {
            this.overlayMenuOpenSubscription.unsubscribe();
        }

        if (this.menuOutsideClickListener) {
            this.menuOutsideClickListener();
        }
    }
}
