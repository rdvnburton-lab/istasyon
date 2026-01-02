import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ChartModule } from 'primeng/chart';
import { TagModule } from 'primeng/tag';
import { Router } from '@angular/router';
import { DashboardService } from '../../../services/dashboard.service';
import { SorumluDashboardDto } from '../../../models/dashboard.model';

@Component({
    selector: 'app-sorumlu-dashboard',
    standalone: true,
    imports: [CommonModule, CardModule, ButtonModule, ChartModule, TagModule],
    templateUrl: './sorumlu-dashboard.component.html',
    styleUrls: ['./sorumlu-dashboard.component.scss']
})
export class SorumluDashboardComponent implements OnInit {
    data: SorumluDashboardDto | null = null;
    loading: boolean = true;

    // Chart data
    weeklyChartData: any;
    weeklyChartOptions: any;

    constructor(
        private dashboardService: DashboardService,
        public router: Router
    ) { }

    ngOnInit() {
        this.loadDashboardData();
        this.initCharts();
    }

    loadDashboardData() {
        this.loading = true;
        this.dashboardService.getSorumluSummary().subscribe({
            next: (data: SorumluDashboardDto) => {
                this.data = data;
                this.loading = false;
            },
            error: (err) => {
                console.error('Dashboard yüklenirken hata:', err);
                this.loading = false;
            }
        });
    }

    initCharts() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColor = documentStyle.getPropertyValue('--text-color');
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const surfaceBorder = documentStyle.getPropertyValue('--surface-border');

        // Weekly performance chart
        this.weeklyChartData = {
            labels: ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'],
            datasets: [
                {
                    label: 'Pompa Satışları',
                    data: [12000, 15000, 13000, 17000, 14000, 16000, 11000],
                    fill: false,
                    borderColor: documentStyle.getPropertyValue('--green-500'),
                    backgroundColor: documentStyle.getPropertyValue('--green-500'),
                    tension: 0.4
                },
                {
                    label: 'Market Satışları',
                    data: [8000, 9000, 8500, 10000, 9500, 11000, 7500],
                    fill: false,
                    borderColor: documentStyle.getPropertyValue('--blue-500'),
                    backgroundColor: documentStyle.getPropertyValue('--blue-500'),
                    tension: 0.4
                }
            ]
        };

        this.weeklyChartOptions = {
            maintainAspectRatio: false,
            aspectRatio: 0.6,
            plugins: {
                legend: {
                    labels: {
                        color: textColor
                    }
                }
            },
            scales: {
                x: {
                    ticks: {
                        color: textColorSecondary
                    },
                    grid: {
                        color: surfaceBorder
                    }
                },
                y: {
                    ticks: {
                        color: textColorSecondary
                    },
                    grid: {
                        color: surfaceBorder
                    }
                }
            }
        };
    }

    goToVardiya(id: number) {
        this.router.navigate(['/operasyon/pompa', id]);
    }

    startVardiya() {
        this.router.navigate(['/vardiya']);
    }

    getRoleLabel(role: string): string {
        switch (role) {
            case 'vardiya sorumlusu':
            case 'vardiya_sorumlusu':
                return 'Vardiya Sorumlusu';
            case 'market sorumlusu':
            case 'market_sorumlusu':
                return 'Market Sorumlusu';
            case 'istasyon sorumlusu':
            case 'istasyon_sorumlusu':
                return 'İstasyon Sorumlusu';
            default:
                return 'Sorumlu';
        }
    }

    isVardiyaSorumlusu(): boolean {
        return this.data?.rol === 'vardiya sorumlusu' ||
            this.data?.rol === 'vardiya_sorumlusu' ||
            this.data?.rol === 'istasyon sorumlusu' ||
            this.data?.rol === 'istasyon_sorumlusu';
    }

    isMarketSorumlusu(): boolean {
        return this.data?.rol === 'market sorumlusu' ||
            this.data?.rol === 'market_sorumlusu' ||
            this.data?.rol === 'istasyon sorumlusu' ||
            this.data?.rol === 'istasyon_sorumlusu';
    }

    isIstasyonSorumlusu(): boolean {
        return this.data?.rol === 'istasyon sorumlusu' ||
            this.data?.rol === 'istasyon_sorumlusu';
    }

    getTotalPendingApprovals(): number {
        return (this.data?.bekleyenOnaySayisi || 0) + (this.data?.bekleyenMarketOnaySayisi || 0);
    }

    goToPendingApprovals(): void {
        // Sadece market sorumlusu ise market sayfasına git, diğerleri vardiya sayfasına
        const role = this.data?.rol?.toLowerCase();

        if (role === 'market sorumlusu' || role === 'market_sorumlusu') {
            // Market sorumlusu market sayfasına gitsin
            this.router.navigate(['/operasyon/market']);
        } else {
            // Vardiya sorumlusu veya istasyon sorumlusu vardiya sayfasına gitsin
            this.router.navigate(['/vardiya']);
        }
    }
}
