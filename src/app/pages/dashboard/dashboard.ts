import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationsWidget } from './components/notificationswidget';
import { StatsWidget } from './components/statswidget';
import { RecentSalesWidget } from './components/recentsaleswidget';
import { BestSellingWidget } from './components/bestsellingwidget';
import { RevenueStreamWidget } from './components/revenuestreamwidget';
import { SorumluDashboardComponent } from './components/sorumlu-dashboard.component';
import { PatronDashboardComponent } from './components/patron-dashboard.component';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-dashboard',
    imports: [CommonModule, StatsWidget, RecentSalesWidget, BestSellingWidget, RevenueStreamWidget, NotificationsWidget, SorumluDashboardComponent, PatronDashboardComponent],
    template: `
        <div *ngIf="isPatron; else checkSorumlu">
            <app-patron-dashboard />
        </div>
        <ng-template #checkSorumlu>
            <div *ngIf="isSorumlu; else defaultDashboard">
                <app-sorumlu-dashboard />
            </div>
        </ng-template>
        <ng-template #defaultDashboard>
            <div class="grid grid-cols-12 gap-8">
                <app-stats-widget class="contents" />
                <div class="col-span-12 xl:col-span-6">
                    <app-recent-sales-widget />
                    <app-best-selling-widget />
                </div>
                <div class="col-span-12 xl:col-span-6">
                    <app-revenue-stream-widget />
                    <app-notifications-widget />
                </div>
            </div>
        </ng-template>
    `
})
export class Dashboard implements OnInit {
    isSorumlu: boolean = false;
    isPatron: boolean = false;

    constructor(private authService: AuthService) { }

    ngOnInit() {
        const user = this.authService.getCurrentUser();
        this.isSorumlu = user?.role === 'vardiya_sorumlusu' || user?.role === 'market_sorumlusu';
        this.isPatron = user?.role === 'patron' || user?.role === 'admin';
    }
}
