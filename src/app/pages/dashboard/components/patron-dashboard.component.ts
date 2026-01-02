import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CardModule } from 'primeng/card';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { Router } from '@angular/router';

interface PatronDashboard {
    ozet: {
        istasyonSayisi: number;
        personelSayisi: number;
        bugunVardiyaSayisi: number;
        bugunToplamCiro: number;
        buAyVardiyaSayisi: number;
        buAyToplamCiro: number;
        gecenAyToplamCiro: number;
        buyumeOrani: number;
    };
    onayBekleyenler: {
        toplamSayi: number;
        vardiyaOnayi: number;
        silmeOnayi: number;
        liste: any[];
    };
    durumDagilimi: {
        acik: number;
        onayBekliyor: number;
        onaylandi: number;
        reddedildi: number;
    };
    son7GunTrend: any[];
    topIstasyonlar: any[];
    sonIslemler: any[];
}

@Component({
    selector: 'app-patron-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        CardModule,
        ChartModule,
        TableModule,
        TagModule,
        ButtonModule,
        SkeletonModule,
        TooltipModule,
        RippleModule
    ],
    templateUrl: './patron-dashboard.component.html',
    styleUrl: './patron-dashboard.component.scss'
})
export class PatronDashboardComponent implements OnInit {
    dashboard: PatronDashboard | null = null;
    loading = true;
    aiLoading = false;
    aiInsight: { mood: string, tespit: string, tavsiye: string } | null = null;

    trendChartData: any;
    trendChartOptions: any;
    durumChartData: any;
    durumChartOptions: any;

    constructor(
        private http: HttpClient,
        private router: Router
    ) {
        this.initChartOptions();
    }

    ngOnInit(): void {
        this.loadDashboard();
    }

    loadDashboard(): void {
        this.loading = true;
        this.http.get<any>(`${environment.apiUrl}/dashboard/patron-dashboard`).subscribe({
            next: (data) => {
                this.dashboard = {
                    ozet: data.ozet || data.Ozet || {},
                    onayBekleyenler: data.onayBekleyenler || data.OnayBekleyenler || { toplamSayi: 0, vardiyaOnayi: 0, silmeOnayi: 0, liste: [] },
                    durumDagilimi: data.durumDagilimi || data.DurumDagilimi || { acik: 0, onayBekliyor: 0, onaylandi: 0, reddedildi: 0 },
                    son7GunTrend: data.son7GunTrend || data.Son7GunTrend || [],
                    topIstasyonlar: data.topIstasyonlar || data.TopIstasyonlar || [],
                    sonIslemler: data.sonIslemler || data.SonIslemler || []
                };
                this.updateCharts();
                this.loading = false;
                this.loadAiAnalysis();
            },
            error: (err) => {
                console.error('Dashboard yüklenirken hata:', err);
                this.loading = false;
            }
        });
    }

    loadAiAnalysis(): void {
        if (!this.dashboard) return;
        this.aiLoading = true;
        this.http.post<any>(`${environment.apiUrl}/gemini/analyze-dashboard`, this.dashboard).subscribe({
            next: (res) => {
                try {
                    // String to JSON if needed
                    this.aiInsight = typeof res === 'string' ? JSON.parse(res) : res;
                } catch (e) {
                    console.error('AI Insight Parse Error:', e);
                }
                this.aiLoading = false;
            },
            error: (err) => {
                console.error('AI Insight Error:', err);
                this.aiLoading = false;
            }
        });
    }

    initChartOptions(): void {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColor = documentStyle.getPropertyValue('--text-color');
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const surfaceBorder = documentStyle.getPropertyValue('--surface-border');

        this.trendChartOptions = {
            maintainAspectRatio: false,
            aspectRatio: 0.6,
            plugins: {
                legend: {
                    labels: {
                        color: textColor,
                        font: {
                            size: 14,
                            weight: 600
                        }
                    }
                }
            },
            scales: {
                x: {
                    ticks: {
                        color: textColorSecondary,
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: surfaceBorder,
                        drawBorder: false
                    }
                },
                y: {
                    ticks: {
                        color: textColorSecondary,
                        font: {
                            size: 12
                        }
                    },
                    grid: {
                        color: surfaceBorder,
                        drawBorder: false
                    }
                }
            }
        };

        this.durumChartOptions = {
            plugins: {
                legend: {
                    labels: {
                        usePointStyle: true,
                        color: textColor,
                        font: {
                            size: 13,
                            weight: 600
                        },
                        padding: 20
                    },
                    position: 'bottom'
                }
            }
        };
    }

    updateCharts(): void {
        if (!this.dashboard) return;

        // Trend Chart
        this.trendChartData = {
            labels: this.dashboard.son7GunTrend.map((item: any) =>
                new Date(item.tarih).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })
            ),
            datasets: [
                {
                    label: 'Günlük Ciro (₺)',
                    data: this.dashboard.son7GunTrend.map((item: any) => item.toplamCiro),
                    fill: true,
                    borderColor: '#667eea',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    tension: 0.4,
                    borderWidth: 3,
                    pointRadius: 5,
                    pointBackgroundColor: '#667eea',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointHoverRadius: 7
                }
            ]
        };

        // Durum Chart
        this.durumChartData = {
            labels: ['Açık', 'Onay Bekliyor', 'Onaylandı', 'Reddedildi'],
            datasets: [
                {
                    data: [
                        this.dashboard.durumDagilimi.acik,
                        this.dashboard.durumDagilimi.onayBekliyor,
                        this.dashboard.durumDagilimi.onaylandi,
                        this.dashboard.durumDagilimi.reddedildi
                    ],
                    backgroundColor: [
                        '#3b82f6',
                        '#f59e0b',
                        '#10b981',
                        '#ef4444'
                    ],
                    hoverBackgroundColor: [
                        '#2563eb',
                        '#d97706',
                        '#059669',
                        '#dc2626'
                    ],
                    borderWidth: 0
                }
            ]
        };
    }

    getDurumSeverity(durum: string): 'success' | 'info' | 'warn' | 'danger' {
        const severities: Record<string, 'success' | 'info' | 'warn' | 'danger'> = {
            'ACIK': 'info',
            'ONAY_BEKLIYOR': 'warn',
            'ONAYLANDI': 'success',
            'REDDEDILDI': 'danger',
            'SILINME_ONAYI_BEKLIYOR': 'warn'
        };
        return severities[durum] || 'info';
    }

    getDurumLabel(durum: string): string {
        const labels: Record<string, string> = {
            'ACIK': 'Açık',
            'ONAY_BEKLIYOR': 'Onay Bekliyor',
            'ONAYLANDI': 'Onaylandı',
            'REDDEDILDI': 'Reddedildi',
            'SILINME_ONAYI_BEKLIYOR': 'Silme Onayı Bekliyor'
        };
        return labels[durum] || durum;
    }

    getIslemIcon(islem: string): string {
        const icons: Record<string, string> = {
            'OLUSTURULDU': 'pi-plus-circle',
            'ONAYLANDI': 'pi-check-circle',
            'REDDEDILDI': 'pi-times-circle',
            'SILME_TALEP_EDILDI': 'pi-trash',
            'SILINDI': 'pi-ban',
            'SILME_REDDEDILDI': 'pi-undo'
        };
        return icons[islem] || 'pi-info-circle';
    }

    getIslemClass(islem: string): string {
        const classes: Record<string, string> = {
            'OLUSTURULDU': 'info',
            'ONAYLANDI': 'success',
            'REDDEDILDI': 'danger',
            'SILME_TALEP_EDILDI': 'warn',
            'SILINDI': 'danger',
            'SILME_REDDEDILDI': 'info'
        };
        return classes[islem] || 'info';
    }

    navigateToOnayBekleyenler(): void {
        this.router.navigate(['/vardiya/onay-bekleyenler']);
    }

    navigateToLoglar(): void {
        this.router.navigate(['/vardiya/loglar']);
    }
}
