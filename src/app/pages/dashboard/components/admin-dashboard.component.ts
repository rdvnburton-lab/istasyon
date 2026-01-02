import { Component, OnInit } from '@angular/core';
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

interface AdminDashboard {
    ozet: {
        totalUsers: number;
        totalIstasyonlar: number;
        aktifIstasyonlar: number;
        totalRoles: number;
        onlineUserCount: number;
        dbSize: string;
        tableCount: number;
    };
    usersByRole: { role: string; count: number }[];
    onlineUsers: any[];
    sonSistemLoglari: any[];
    tabloIstatistikleri: {
        vardiyalar: number;
        marketVardiyalar: number;
        otomasyonSatislar: number;
        pusulalar: number;
    };
}

@Component({
    selector: 'app-admin-dashboard',
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
    template: `
        <div class="admin-dashboard p-4">
            <!-- REFINED CORPORATE AI INSIGHT CARD -->
            <div class="ai-insight-card mb-6" *ngIf="aiInsight || aiLoading">
                <div class="relative overflow-hidden p-6 rounded-2xl border border-slate-700 shadow-xl bg-slate-900 text-slate-100">
                    <div class="absolute -top-24 -right-24 w-64 h-64 bg-indigo-500/10 rounded-full blur-[80px]"></div>
                    
                    <div class="relative z-10 flex flex-col md:flex-row items-center gap-6">
                        <div class="ai-mood-wrapper shrink-0">
                            <div class="w-16 h-16 rounded-xl bg-slate-800 flex items-center justify-center text-3xl shadow-inner border border-slate-700">
                                <span *ngIf="!aiLoading">{{ (aiInsight?.mood || '').split(' ')[0] || '⚙️' }}</span>
                                <i *ngIf="aiLoading" class="pi pi-spin pi-cog text-2xl text-indigo-400"></i>
                            </div>
                        </div>

                        <div class="flex-1 text-center md:text-left">
                            <div class="flex items-center justify-center md:justify-start gap-3 mb-2">
                                <span class="bg-indigo-900/40 text-indigo-300 text-[9px] font-bold uppercase tracking-widest px-2.5 py-1 rounded border border-indigo-800/50">
                                    Sistem Analiz Motoru
                                </span>
                                <span class="text-[10px] text-slate-500 font-medium">Motor: Gemini Pro</span>
                            </div>

                            <div *ngIf="aiLoading" class="space-y-3">
                                <p-skeleton width="70%" height="1.2rem" styleClass="bg-slate-800"></p-skeleton>
                                <p-skeleton width="90%" height="0.8rem" styleClass="bg-slate-800"></p-skeleton>
                            </div>

                            <div *ngIf="!aiLoading && aiInsight">
                                <h2 class="text-lg md:text-xl font-bold mb-1 text-white">
                                    {{ aiInsight.mood.includes(' ') ? aiInsight.mood.substring(aiInsight.mood.indexOf(' ') + 1) : aiInsight.mood }}
                                </h2>
                                <p class="text-slate-400 text-xs md:text-sm mb-4 leading-relaxed line-clamp-2 md:line-clamp-none">
                                    <strong>Analitik Rapor:</strong> {{ aiInsight.tespit }}
                                </p>
                                <div class="inline-flex items-center gap-3 px-4 py-2.5 rounded-lg bg-indigo-900/20 border border-indigo-800/30">
                                    <i class="pi pi-bolt text-indigo-400"></i>
                                    <span class="text-xs md:text-sm font-semibold text-indigo-200 uppercase tracking-tight">
                                        Öneri: {{ aiInsight.tavsiye }}
                                    </span>
                                </div>
                            </div>
                        </div>
                        <button *ngIf="!aiLoading" pButton icon="pi pi-refresh" (click)="loadAiAnalysis()" 
                            class="p-button-sm p-button-outlined p-button-secondary border-slate-700 text-slate-400"></button>
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-12 gap-4 mb-4">
                <!-- Stats Cards ... -->
                <div class="col-span-12 md:col-span-6 lg:col-span-3">
                    <div class="card p-4 shadow-sm border-round bg-blue-50 border-blue-100 flex align-items-center">
                        <div class="p-3 border-round bg-blue-500 text-white mr-3">
                            <i class="pi pi-users text-2xl"></i>
                        </div>
                        <div>
                            <span class="block text-500 font-medium mb-1">Toplam Kullanıcı</span>
                            <div class="text-900 font-bold text-2xl">{{dashboard?.ozet?.totalUsers || 0}}</div>
                            <small class="text-green-600 font-medium">{{dashboard?.ozet?.onlineUserCount || 0}} Online</small>
                        </div>
                    </div>
                </div>
                <div class="col-span-12 md:col-span-6 lg:col-span-3">
                    <div class="card p-4 shadow-sm border-round bg-green-50 border-green-100 flex align-items-center">
                        <div class="p-3 border-round bg-green-500 text-white mr-3">
                            <i class="pi pi-database text-2xl"></i>
                        </div>
                        <div>
                            <span class="block text-500 font-medium mb-1">Veritabanı Boyutu</span>
                            <div class="text-900 font-bold text-2xl">{{dashboard?.ozet?.dbSize || 'N/A'}}</div>
                            <small class="text-500">{{dashboard?.ozet?.tableCount || 0}} Tablo</small>
                        </div>
                    </div>
                </div>
                <div class="col-span-12 md:col-span-6 lg:col-span-3">
                    <div class="card p-4 shadow-sm border-round bg-purple-50 border-purple-100 flex align-items-center">
                        <div class="p-3 border-round bg-purple-500 text-white mr-3">
                            <i class="pi pi-id-card text-2xl"></i>
                        </div>
                        <div>
                            <span class="block text-500 font-medium mb-1">Tanımlı Roller</span>
                            <div class="text-900 font-bold text-2xl">{{dashboard?.ozet?.totalRoles || 0}}</div>
                        </div>
                    </div>
                </div>
                <div class="col-span-12 md:col-span-6 lg:col-span-3">
                    <div class="card p-4 shadow-sm border-round bg-orange-50 border-orange-100 flex align-items-center">
                        <div class="p-3 border-round bg-orange-500 text-white mr-3">
                            <i class="pi pi-building text-2xl"></i>
                        </div>
                        <div>
                            <span class="block text-500 font-medium mb-1">Aktif İstasyonlar</span>
                            <div class="text-900 font-bold text-2xl">{{dashboard?.ozet?.aktifIstasyonlar || 0}} / {{dashboard?.ozet?.totalIstasyonlar || 0}}</div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="grid grid-cols-12 gap-4">
                <!-- Charts -->
                <div class="col-span-12 lg:col-span-4">
                    <p-card header="Kullanıcı Dağılımı" subheader="Rollere göre kullanıcı sayıları">
                        <p-chart type="doughnut" [data]="roleChartData" [options]="roleChartOptions" height="300px"></p-chart>
                    </p-card>
                </div>
                <div class="col-span-12 lg:col-span-8">
                    <p-card header="Veri İstatistikleri" subheader="Sistemdeki toplam kayıt sayıları">
                        <p-chart type="bar" [data]="statsChartData" [options]="statsChartOptions" height="300px"></p-chart>
                    </p-card>
                </div>

                <!-- Online Users -->
                <div class="col-span-12 lg:col-span-5">
                    <p-card header="Online Kullanıcılar" subheader="Son 15 dakika içinde aktif olanlar">
                        <p-table [value]="dashboard?.onlineUsers || []" [rows]="5" responsiveLayout="scroll">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th>Kullanıcı</th>
                                    <th>Rol</th>
                                    <th>Son Aktivite</th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-user>
                                <tr>
                                    <td>
                                        <div class="flex align-items-center">
                                            <div class="w-2rem h-2rem border-circle bg-blue-100 text-blue-600 flex align-items-center justify-content-center mr-2">
                                                {{user.username.charAt(0).toUpperCase()}}
                                            </div>
                                            <span>{{user.username}}</span>
                                        </div>
                                    </td>
                                    <td>{{user.role}}</td>
                                    <td>{{user.lastActivity | date:'HH:mm'}}</td>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="emptymessage">
                                <tr>
                                    <td colspan="3" class="text-center p-4 text-500">Şu an online kullanıcı yok.</td>
                                </tr>
                            </ng-template>
                        </p-table>
                    </p-card>
                </div>

                <!-- Recent Logs -->
                <div class="col-span-12 lg:col-span-7">
                    <p-card header="Son Sistem Hareketleri">
                        <p-table [value]="dashboard?.sonSistemLoglari || []" [rows]="5" [paginator]="true" responsiveLayout="scroll">
                            <ng-template pTemplate="header">
                                <tr>
                                    <th>İşlem</th>
                                    <th>Açıklama</th>
                                    <th>Kullanıcı</th>
                                    <th>Tarih</th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-log>
                                <tr>
                                    <td>
                                        <p-tag [value]="log.islem" [severity]="getIslemSeverity(log.islem)"></p-tag>
                                    </td>
                                    <td>{{log.aciklama}}</td>
                                    <td>{{log.kullaniciAdi}}</td>
                                    <td>{{log.islemTarihi | date:'dd.MM HH:mm'}}</td>
                                </tr>
                            </ng-template>
                        </p-table>
                    </p-card>
                </div>
            </div>

            <!-- Quick Actions -->
            <div class="grid grid-cols-12 gap-4 mt-4">
                <div class="col-span-12">
                    <div class="card p-4 shadow-sm border-round bg-white">
                        <h3 class="m-0 mb-3">Hızlı Erişim</h3>
                        <div class="flex flex-wrap gap-3">
                            <button pButton label="Kullanıcı Yönetimi" icon="pi pi-users" class="p-button-outlined" (click)="router.navigate(['/sistem/kullanici'])"></button>
                            <button pButton label="İstasyon Yönetimi" icon="pi pi-building" class="p-button-outlined" (click)="router.navigate(['/sistem/istasyon'])"></button>
                            <button pButton label="Sistem Sağlığı" icon="pi pi-heart-fill" class="p-button-outlined p-button-danger" (click)="router.navigate(['/admin/health'])"></button>
                            <button pButton label="Rol Yönetimi" icon="pi pi-id-card" class="p-button-outlined" (click)="router.navigate(['/sistem/roller'])"></button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    styles: [`
        :host ::ng-deep .p-card {
            height: 100%;
        }
        .card {
            background: var(--surface-card);
            border: 1px solid var(--surface-border);
        }
    `]
})
export class AdminDashboardComponent implements OnInit {
    dashboard: AdminDashboard | null = null;
    loading = true;
    aiLoading = false;
    aiInsight: { mood: string, tespit: string, tavsiye: string } | null = null;

    roleChartData: any;
    roleChartOptions: any;
    statsChartData: any;
    statsChartOptions: any;

    constructor(
        private http: HttpClient,
        public router: Router
    ) { }

    ngOnInit(): void {
        this.loadDashboard();
        this.initChartOptions();
    }

    loadDashboard(): void {
        this.loading = true;
        this.http.get<AdminDashboard>(`${environment.apiUrl}/dashboard/admin-dashboard`).subscribe({
            next: (data) => {
                this.dashboard = data;
                this.updateCharts();
                this.loading = false;
                this.loadAiAnalysis();
            },
            error: (err) => {
                console.error('Admin Dashboard yüklenirken hata:', err);
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
                    this.aiInsight = typeof res === 'string' ? JSON.parse(res) : res;
                } catch (e) {
                    console.error('Admin AI Insight Parse Error:', e);
                }
                this.aiLoading = false;
            },
            error: (err) => {
                console.error('Admin AI Insight Error:', err);
                this.aiLoading = false;
            }
        });
    }

    initChartOptions(): void {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColor = documentStyle.getPropertyValue('--text-color');
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
        const surfaceBorder = documentStyle.getPropertyValue('--surface-border');

        this.roleChartOptions = {
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: textColor
                    }
                }
            }
        };

        this.statsChartOptions = {
            maintainAspectRatio: false,
            aspectRatio: 0.8,
            plugins: {
                legend: {
                    display: false
                }
            },
            scales: {
                x: {
                    ticks: {
                        color: textColorSecondary,
                        font: {
                            weight: 500
                        }
                    },
                    grid: {
                        color: surfaceBorder,
                        drawBorder: false
                    }
                },
                y: {
                    ticks: {
                        color: textColorSecondary
                    },
                    grid: {
                        color: surfaceBorder,
                        drawBorder: false
                    }
                }
            }
        };
    }

    updateCharts(): void {
        if (!this.dashboard) return;

        // Role Chart
        this.roleChartData = {
            labels: this.dashboard.usersByRole.map(r => r.role),
            datasets: [
                {
                    data: this.dashboard.usersByRole.map(r => r.count),
                    backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'],
                    hoverBackgroundColor: ['#2563eb', '#059669', '#d97706', '#dc2626', '#7c3aed', '#db2777']
                }
            ]
        };

        // Stats Chart
        this.statsChartData = {
            labels: ['Vardiyalar', 'Market', 'Satışlar', 'Pusulalar'],
            datasets: [
                {
                    label: 'Kayıt Sayısı',
                    backgroundColor: '#6366f1',
                    borderColor: '#6366f1',
                    data: [
                        this.dashboard.tabloIstatistikleri.vardiyalar,
                        this.dashboard.tabloIstatistikleri.marketVardiyalar,
                        this.dashboard.tabloIstatistikleri.otomasyonSatislar,
                        this.dashboard.tabloIstatistikleri.pusulalar
                    ]
                }
            ]
        };
    }

    getIslemSeverity(islem: string): 'success' | 'info' | 'warn' | 'danger' {
        const severities: Record<string, 'success' | 'info' | 'warn' | 'danger'> = {
            'OLUSTURULDU': 'info',
            'ONAYLANDI': 'success',
            'REDDEDILDI': 'danger',
            'SILINDI': 'danger',
            'SILME_TALEP_EDILDI': 'warn',
            'GIRIS': 'success',
            'CIKIS': 'info'
        };
        return severities[islem] || 'info';
    }
}
