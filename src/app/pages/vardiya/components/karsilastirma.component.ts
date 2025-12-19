import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { ProgressBarModule } from 'primeng/progressbar';
import { ChartModule } from 'primeng/chart';
import { ToastModule } from 'primeng/toast';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { PanelModule } from 'primeng/panel';

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../services/vardiya.service';
import {
    Vardiya,
    KarsilastirmaSonuc,
    KarsilastirmaDurum,
    KarsilastirmaDetay,
    OdemeYontemi,
    PompaSatis,
    YakitTuru
} from '../models/vardiya.model';

@Component({
    selector: 'app-karsilastirma',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        DividerModule,
        ProgressBarModule,
        ChartModule,
        ToastModule,
        SkeletonModule,
        TooltipModule,
        PanelModule
    ],
    providers: [MessageService],
    template: `
        <p-toast></p-toast>

        <div class="grid grid-cols-12 gap-6">
            <!-- Başlık -->
            <div class="col-span-12">
                <div class="card">
                    <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                        <div>
                            <h2 class="text-2xl font-bold text-surface-800 dark:text-surface-100 m-0">
                                <i class="pi pi-chart-bar mr-2 text-blue-500"></i>Karşılaştırma Raporu
                            </h2>
                            <p class="text-surface-500 mt-2" *ngIf="aktifVardiya">
                                {{ aktifVardiya.istasyonAdi }} - {{ aktifVardiya.sorumluAdi }} | 
                                {{ aktifVardiya.baslangicTarihi | date:'dd.MM.yyyy HH:mm' }}
                            </p>
                        </div>
                        <div class="flex gap-2">
                            <p-button 
                                label="Yenile" 
                                icon="pi pi-refresh" 
                                severity="secondary"
                                [loading]="loading"
                                (onClick)="karsilastirmaYap()">
                            </p-button>
                            <p-button 
                                label="Yazdır" 
                                icon="pi pi-print" 
                                severity="info"
                                (onClick)="yazdir()">
                            </p-button>
                            <p-button 
                                label="Geri" 
                                icon="pi pi-arrow-left" 
                                severity="secondary"
                                [outlined]="true"
                                [routerLink]="['/vardiya']">
                            </p-button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Loading Durumu -->
            <ng-container *ngIf="loading">
                <div class="col-span-12 md:col-span-4">
                    <div class="card">
                        <p-skeleton height="120px"></p-skeleton>
                    </div>
                </div>
                <div class="col-span-12 md:col-span-4">
                    <div class="card">
                        <p-skeleton height="120px"></p-skeleton>
                    </div>
                </div>
                <div class="col-span-12 md:col-span-4">
                    <div class="card">
                        <p-skeleton height="120px"></p-skeleton>
                    </div>
                </div>
            </ng-container>

            <!-- Sonuç Kartları -->
            <ng-container *ngIf="!loading && sonuc">
                <!-- Sistem Toplamı -->
                <div class="col-span-12 md:col-span-4">
                    <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                         style="background: linear-gradient(135deg, #6366f1 0%, #3b82f6 50%, #2563eb 100%); box-shadow: 0 10px 40px rgba(59, 130, 246, 0.3);">
                        <div class="flex justify-between items-start mb-4">
                            <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                                 style="background: rgba(255,255,255,0.2);">
                                <i class="pi pi-server text-2xl" style="color: white;"></i>
                            </div>
                            <div class="text-right">
                                <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Sistem Verisi</span>
                                <h3 class="text-3xl font-bold m-0" style="color: white !important;">
                                    {{ sonuc.sistemToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </h3>
                            </div>
                        </div>
                        <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                            <div class="flex items-center gap-2">
                                <i class="pi pi-info-circle text-sm" style="color: rgba(255,255,255,0.7);"></i>
                                <span class="text-sm" style="color: rgba(255,255,255,0.9);">OPC/Otomasyon sisteminden alınan veri</span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Tahsilat Toplamı -->
                <div class="col-span-12 md:col-span-4">
                    <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                         style="background: linear-gradient(135deg, #10b981 0%, #22c55e 50%, #16a34a 100%); box-shadow: 0 10px 40px rgba(34, 197, 94, 0.3);">
                        <div class="flex justify-between items-start mb-4">
                            <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                                 style="background: rgba(255,255,255,0.2);">
                                <i class="pi pi-wallet text-2xl" style="color: white;"></i>
                            </div>
                            <div class="text-right">
                                <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Girilen Tahsilat</span>
                                <h3 class="text-3xl font-bold m-0" style="color: white !important;">
                                    {{ sonuc.tahsilatToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </h3>
                            </div>
                        </div>
                        <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                            <div class="flex items-center gap-2">
                                <i class="pi pi-pencil text-sm" style="color: rgba(255,255,255,0.7);"></i>
                                <span class="text-sm" style="color: rgba(255,255,255,0.9);">Operatör tarafından girilen toplam</span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Fark -->
                <div class="col-span-12 md:col-span-4">
                    <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                         [style.background]="getFarkCardBackground()"
                         [style.box-shadow]="getFarkCardShadow()">
                        <div class="flex justify-between items-start mb-4">
                            <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                                 style="background: rgba(255,255,255,0.2);">
                                <i class="pi text-2xl" [ngClass]="getFarkIcon()" style="color: white;"></i>
                            </div>
                            <div class="text-right">
                                <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Fark</span>
                                <h3 class="text-3xl font-bold m-0" style="color: white !important;">
                                    {{ sonuc.fark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </h3>
                            </div>
                        </div>
                        <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                            <div class="flex items-center justify-between mb-2">
                                <span class="text-sm" style="color: rgba(255,255,255,0.9);">Fark Oranı</span>
                                <span class="font-bold text-lg" style="color: white !important;">{{ sonuc.farkYuzde | number:'1.2-2' }}%</span>
                            </div>
                            <p-tag 
                                [value]="getDurumLabel()" 
                                [severity]="getDurumSeverity()"
                                styleClass="w-full text-center">
                            </p-tag>
                        </div>
                    </div>
                </div>
            </ng-container>

            <!-- Detaylı Karşılaştırma Tablosu -->
            <div class="col-span-12 lg:col-span-7" *ngIf="!loading && sonuc">
                <div class="card h-full">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-list mr-2"></i>Ödeme Yöntemi Karşılaştırması
                    </h5>

                    <p-table [value]="sonuc.detaylar" styleClass="p-datatable-striped">
                        <ng-template pTemplate="header">
                            <tr>
                                <th>Ödeme Yöntemi</th>
                                <th class="text-right">Sistem</th>
                                <th class="text-right">Tahsilat</th>
                                <th class="text-right">Fark</th>
                                <th class="text-center">Durum</th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-detay>
                            <tr>
                                <td>
                                    <div class="flex items-center gap-2">
                                        <i [class]="getOdemeIcon(detay.odemeYontemi)" class="text-lg"></i>
                                        <span class="font-semibold">{{ getOdemeLabel(detay.odemeYontemi) }}</span>
                                    </div>
                                </td>
                                <td class="text-right">
                                    {{ detay.sistemTutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-right">
                                    {{ detay.tahsilatTutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-right font-semibold" [ngClass]="getFarkClass(detay.fark)">
                                    {{ detay.fark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-center">
                                    <i class="pi text-lg" 
                                       [ngClass]="getDetayDurumIcon(detay.fark)"
                                       [pTooltip]="getDetayDurumTooltip(detay.fark)">
                                    </i>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="footer">
                            <tr class="font-bold bg-surface-100 dark:bg-surface-800">
                                <td>TOPLAM</td>
                                <td class="text-right">
                                    {{ sonuc.sistemToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-right">
                                    {{ sonuc.tahsilatToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-right" [ngClass]="getFarkClass(sonuc.fark)">
                                    {{ sonuc.fark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td class="text-center">
                                    <p-tag 
                                        [value]="getDurumLabel()" 
                                        [severity]="getDurumSeverity()"
                                        size="small">
                                    </p-tag>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>

                    <!-- Fark Açıklaması -->
                    <div class="mt-4 p-4 rounded-lg" [ngClass]="getFarkAlertClass()" *ngIf="sonuc.fark !== 0">
                        <div class="flex items-start gap-3">
                            <i class="pi pi-exclamation-triangle text-2xl"></i>
                            <div>
                                <h6 class="font-bold m-0">{{ getFarkBaslik() }}</h6>
                                <p class="m-0 mt-1">{{ getFarkAciklama() }}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Grafik -->
            <div class="col-span-12 lg:col-span-5" *ngIf="!loading && sonuc">
                <div class="card h-full">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-chart-pie mr-2"></i>Görsel Karşılaştırma
                    </h5>
                    <p-chart type="bar" [data]="karsilastirmaChartData" [options]="chartOptions" height="300"></p-chart>
                </div>
            </div>

            <!-- Pompa Satışları -->
            <div class="col-span-12" *ngIf="!loading && sonuc">
                <div class="card">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-box mr-2"></i>Pompa Satışları (Sistem)
                    </h5>
                    
                    <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                        <div *ngFor="let grup of gruplanmisPompaSatislari" class="border border-surface-200 dark:border-surface-700 rounded-lg overflow-hidden">
                            <div class="p-3 flex justify-between items-center font-bold transition-colors"
                                 [ngClass]="getPompaHeaderClass(grup.pompaNo)">
                                <div class="flex flex-col">
                                    <div class="flex items-center gap-2">
                                        <i class="pi pi-server" [ngClass]="getPompaIconClass(grup.pompaNo)"></i>
                                        <span>Pompa {{ grup.pompaNo }}</span>
                                    </div>
                                    <span *ngIf="grup.pompaNo === enYogunAkaryakitPompaNo" class="text-xs font-normal opacity-80">En Yoğun Akaryakıt</span>
                                    <span *ngIf="grup.pompaNo === enYogunLpgPompaNo" class="text-xs font-normal opacity-80">En Yoğun LPG</span>
                                </div>
                                <div class="flex items-center gap-3">
                                    <span class="text-sm font-normal opacity-70">{{ grup.toplamLitre | number:'1.2-2' }} Lt</span>
                                    <span class="text-primary font-bold">{{ grup.toplamTutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                </div>
                            </div>
                            <div class="divide-y divide-surface-200 dark:divide-surface-700">
                                <div *ngFor="let satis of grup.satislar" class="p-3 flex items-center gap-3 hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors">
                                    <div class="w-8 h-8 rounded-full flex items-center justify-center text-xs text-white" 
                                            [ngClass]="getYakitBgClass(satis.yakitTuru)">
                                        {{ getYakitLabel(satis.yakitTuru).substring(0, 1) }}
                                    </div>
                                    <div class="flex-1">
                                        <div class="flex justify-between items-center">
                                            <span class="font-medium text-sm">{{ getYakitLabel(satis.yakitTuru) }}</span>
                                            <span class="font-bold text-sm">
                                                {{ satis.toplamTutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                            </span>
                                        </div>
                                        <div class="text-xs text-surface-500 mt-1">
                                            {{ satis.litre | number:'1.2-2' }} Lt
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div *ngIf="gruplanmisPompaSatislari.length === 0" class="text-center py-6 text-surface-500">
                        <i class="pi pi-inbox text-4xl mb-2"></i>
                        <p class="m-0">Pompa satış verisi bulunamadı</p>
                    </div>
                </div>
            </div>

            <!-- Aktif Vardiya Yok -->
            <div class="col-span-12" *ngIf="!loading && !aktifVardiya">
                <div class="card">
                    <div class="text-center py-12">
                        <i class="pi pi-exclamation-triangle text-6xl text-yellow-500 mb-4"></i>
                        <h4 class="text-surface-600">Vardiya Seçilmedi</h4>
                        <p class="text-surface-500 mb-4">
                            Karşılaştırma raporunu görüntülemek için lütfen listeden bir vardiya seçerek mutabakat işlemini başlatın.
                        </p>
                        <p-button 
                            label="Vardiya Listesine Git" 
                            icon="pi pi-list" 
                            severity="primary"
                            [routerLink]="['/vardiya']">
                        </p-button>
                    </div>
                </div>
            </div>
        </div>
    `,
    styles: [`
        :host {
            display: block;
        }
        
        @media print {
            .card {
                box-shadow: none !important;
                border: 1px solid #ddd !important;
            }
        }
    `]
})
export class Karsilastirma implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    sonuc: KarsilastirmaSonuc | null = null;
    pompaSatislari: PompaSatis[] = [];
    gruplanmisPompaSatislari: { pompaNo: number; toplamTutar: number; toplamLitre: number; satislar: PompaSatis[] }[] = [];
    enYogunAkaryakitPompaNo: number | null = null;
    enYogunLpgPompaNo: number | null = null;

    loading = false;

    karsilastirmaChartData: any;
    chartOptions: any;

    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.initChart();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (vardiya) {
                    this.karsilastirmaYap();
                    this.loadPompaSatislari(vardiya.id);
                }
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    initChart(): void {
        this.chartOptions = {
            plugins: {
                legend: {
                    position: 'bottom'
                }
            },
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: (value: number) => value.toLocaleString('tr-TR') + ' ₺'
                    }
                }
            }
        };
    }

    karsilastirmaYap(): void {
        if (!this.aktifVardiya) return;

        this.loading = true;

        this.vardiyaService.karsilastirmaYap(this.aktifVardiya.id).subscribe({
            next: (sonuc) => {
                this.sonuc = sonuc;
                this.updateChart(sonuc);
                this.loading = false;
            },
            error: (err) => {
                this.loading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Karşılaştırma yapılırken hata oluştu'
                });
            }
        });
    }

    loadPompaSatislari(vardiyaId: number): void {
        this.vardiyaService.getPompaSatislari(vardiyaId).subscribe({
            next: (satislar) => {
                this.pompaSatislari = satislar;
                this.gruplaPompaSatislari();
            }
        });
    }

    gruplaPompaSatislari(): void {
        const gruplar = new Map<number, { pompaNo: number; toplamTutar: number; toplamLitre: number; satislar: PompaSatis[] }>();

        this.pompaSatislari.forEach(satis => {
            if (!gruplar.has(satis.pompaNo)) {
                gruplar.set(satis.pompaNo, {
                    pompaNo: satis.pompaNo,
                    toplamTutar: 0,
                    toplamLitre: 0,
                    satislar: []
                });
            }
            const grup = gruplar.get(satis.pompaNo)!;
            grup.toplamTutar += satis.toplamTutar;
            grup.toplamLitre += satis.litre;

            // Yakıt türüne göre grupla
            const existingSatis = grup.satislar.find(s => s.yakitTuru === satis.yakitTuru);
            if (existingSatis) {
                existingSatis.toplamTutar += satis.toplamTutar;
                existingSatis.litre += satis.litre;
            } else {
                // Orijinal veriyi bozmamak için kopyasını ekle
                grup.satislar.push({ ...satis });
            }
        });



        this.gruplanmisPompaSatislari = Array.from(gruplar.values()).sort((a, b) => a.pompaNo - b.pompaNo);

        // En yoğun pompaları bul (Akaryakıt ve LPG ayrı ayrı)
        let maxAkaryakitTutar = 0;
        let maxLpgTutar = 0;
        this.enYogunAkaryakitPompaNo = null;
        this.enYogunLpgPompaNo = null;

        this.gruplanmisPompaSatislari.forEach(grup => {
            let akaryakitTutar = 0;
            let lpgTutar = 0;

            grup.satislar.forEach(s => {
                if (s.yakitTuru === YakitTuru.LPG) {
                    lpgTutar += s.toplamTutar;
                } else {
                    akaryakitTutar += s.toplamTutar;
                }
            });

            if (akaryakitTutar > maxAkaryakitTutar) {
                maxAkaryakitTutar = akaryakitTutar;
                this.enYogunAkaryakitPompaNo = grup.pompaNo;
            }

            if (lpgTutar > maxLpgTutar) {
                maxLpgTutar = lpgTutar;
                this.enYogunLpgPompaNo = grup.pompaNo;
            }
        });
    }

    getPompaHeaderClass(pompaNo: number): string {
        if (pompaNo === this.enYogunAkaryakitPompaNo && pompaNo === this.enYogunLpgPompaNo) {
            return 'bg-purple-100 dark:bg-purple-900/40 text-purple-800 dark:text-purple-200';
        } else if (pompaNo === this.enYogunAkaryakitPompaNo) {
            return 'bg-orange-100 dark:bg-orange-900/40 text-orange-800 dark:text-orange-200';
        } else if (pompaNo === this.enYogunLpgPompaNo) {
            return 'bg-blue-100 dark:bg-blue-900/40 text-blue-800 dark:text-blue-200';
        }
        return 'bg-surface-100 dark:bg-surface-800 text-surface-700 dark:text-surface-200';
    }

    getPompaIconClass(pompaNo: number): string {
        if (pompaNo === this.enYogunAkaryakitPompaNo && pompaNo === this.enYogunLpgPompaNo) {
            return 'text-purple-600 dark:text-purple-300';
        } else if (pompaNo === this.enYogunAkaryakitPompaNo) {
            return 'text-orange-600 dark:text-orange-300';
        } else if (pompaNo === this.enYogunLpgPompaNo) {
            return 'text-blue-600 dark:text-blue-300';
        }
        return 'text-surface-500';
    }

    updateChart(sonuc: KarsilastirmaSonuc): void {
        const labels = sonuc.detaylar.map(d => this.getOdemeLabel(d.odemeYontemi));
        const sistemData = sonuc.detaylar.map(d => d.sistemTutar);
        const tahsilatData = sonuc.detaylar.map(d => d.tahsilatTutar);

        this.karsilastirmaChartData = {
            labels,
            datasets: [
                {
                    label: 'Sistem',
                    data: sistemData,
                    backgroundColor: '#3B82F6'
                },
                {
                    label: 'Tahsilat',
                    data: tahsilatData,
                    backgroundColor: '#10B981'
                }
            ]
        };
    }

    getOdemeLabel(yontem: OdemeYontemi): string {
        const labels: Record<OdemeYontemi, string> = {
            [OdemeYontemi.NAKIT]: 'Nakit',
            [OdemeYontemi.KREDI_KARTI]: 'Kredi Kartı',
            [OdemeYontemi.VERESIYE]: 'Veresiye',
            [OdemeYontemi.FILO_KARTI]: 'Filo Kartı',
            [OdemeYontemi.YEMEK_KARTI]: 'Yemek Kartı'
        };
        return labels[yontem] || yontem;
    }

    getOdemeIcon(yontem: OdemeYontemi): string {
        const icons: Record<OdemeYontemi, string> = {
            [OdemeYontemi.NAKIT]: 'pi pi-money-bill text-green-500',
            [OdemeYontemi.KREDI_KARTI]: 'pi pi-credit-card text-blue-500',
            [OdemeYontemi.VERESIYE]: 'pi pi-clock text-yellow-500',
            [OdemeYontemi.FILO_KARTI]: 'pi pi-car text-purple-500',
            [OdemeYontemi.YEMEK_KARTI]: 'pi pi-shopping-bag text-orange-500'
        };
        return icons[yontem] || 'pi pi-circle';
    }

    getYakitLabel(yakit: YakitTuru): string {
        const labels: Record<YakitTuru, string> = {
            [YakitTuru.BENZIN]: 'Benzin',
            [YakitTuru.MOTORIN]: 'Motorin',
            [YakitTuru.LPG]: 'LPG',
            [YakitTuru.EURO_DIESEL]: 'Euro Diesel'
        };
        return labels[yakit] || yakit;
    }

    getYakitBgClass(yakit: YakitTuru): string {
        const classes: Record<YakitTuru, string> = {
            [YakitTuru.BENZIN]: 'bg-blue-500',
            [YakitTuru.MOTORIN]: 'bg-green-600',
            [YakitTuru.LPG]: 'bg-yellow-500',
            [YakitTuru.EURO_DIESEL]: 'bg-indigo-500'
        };
        return classes[yakit] || 'bg-gray-500';
    }

    getFarkCardBackground(): string {
        if (!this.sonuc) return 'linear-gradient(135deg, #64748b 0%, #475569 100%)';

        switch (this.sonuc.durum) {
            case KarsilastirmaDurum.UYUMLU:
                return 'linear-gradient(135deg, #10b981 0%, #22c55e 50%, #16a34a 100%)';
            case KarsilastirmaDurum.FARK_VAR:
                return 'linear-gradient(135deg, #f59e0b 0%, #f97316 50%, #ea580c 100%)';
            case KarsilastirmaDurum.KRITIK_FARK:
                return 'linear-gradient(135deg, #ef4444 0%, #dc2626 50%, #b91c1c 100%)';
            default:
                return 'linear-gradient(135deg, #64748b 0%, #475569 100%)';
        }
    }

    getFarkCardShadow(): string {
        if (!this.sonuc) return 'none';

        switch (this.sonuc.durum) {
            case KarsilastirmaDurum.UYUMLU:
                return '0 10px 40px rgba(34, 197, 94, 0.3)';
            case KarsilastirmaDurum.FARK_VAR:
                return '0 10px 40px rgba(249, 115, 22, 0.3)';
            case KarsilastirmaDurum.KRITIK_FARK:
                return '0 10px 40px rgba(220, 38, 38, 0.3)';
            default:
                return 'none';
        }
    }

    getFarkIcon(): string {
        if (!this.sonuc) return 'pi-minus';

        if (this.sonuc.fark === 0) return 'pi-check';
        if (this.sonuc.fark > 0) return 'pi-arrow-up';
        return 'pi-arrow-down';
    }

    getFarkClass(fark: number): string {
        if (fark === 0) return 'text-green-500';
        if (fark > 0) return 'text-blue-500';
        return 'text-red-500';
    }

    getDetayDurumIcon(fark: number): string {
        if (fark === 0) return 'pi-check-circle text-green-500';
        if (Math.abs(fark) < 10) return 'pi-exclamation-circle text-yellow-500';
        return 'pi-times-circle text-red-500';
    }

    getDetayDurumTooltip(fark: number): string {
        if (fark === 0) return 'Uyumlu';
        if (Math.abs(fark) < 10) return 'Küçük fark';
        return 'Fark var';
    }

    getDurumLabel(): string {
        if (!this.sonuc) return 'Belirsiz';

        const labels: Record<KarsilastirmaDurum, string> = {
            [KarsilastirmaDurum.UYUMLU]: 'Uyumlu',
            [KarsilastirmaDurum.FARK_VAR]: 'Fark Var',
            [KarsilastirmaDurum.KRITIK_FARK]: 'Kritik Fark'
        };
        return labels[this.sonuc.durum];
    }

    getDurumSeverity(): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        if (!this.sonuc) return 'secondary';

        const severities: Record<KarsilastirmaDurum, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            [KarsilastirmaDurum.UYUMLU]: 'success',
            [KarsilastirmaDurum.FARK_VAR]: 'warn',
            [KarsilastirmaDurum.KRITIK_FARK]: 'danger'
        };
        return severities[this.sonuc.durum];
    }

    getFarkAlertClass(): string {
        if (!this.sonuc) return 'bg-gray-100 text-gray-700';

        if (this.sonuc.fark > 0) {
            return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300';
        }
        return 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300';
    }

    getFarkBaslik(): string {
        if (!this.sonuc) return '';

        if (this.sonuc.fark > 0) {
            return 'Tahsilat Fazlası Tespit Edildi';
        }
        return 'Tahsilat Eksiği Tespit Edildi';
    }

    getFarkAciklama(): string {
        if (!this.sonuc) return '';

        const farkAbs = Math.abs(this.sonuc.fark);

        if (this.sonuc.fark > 0) {
            return `Girilen tahsilatlar, sistem verilerinden ${farkAbs.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })} fazla. 
                    Lütfen fazla ödeme kaynaklarını kontrol edin.`;
        }
        return `Girilen tahsilatlar, sistem verilerinden ${farkAbs.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' })} eksik. 
                Lütfen eksik tahsilatları kontrol edin veya açıklama ekleyin.`;
    }

    yazdir(): void {
        window.print();
    }
}
