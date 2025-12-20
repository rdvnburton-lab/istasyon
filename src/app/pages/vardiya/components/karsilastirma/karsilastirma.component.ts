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

import { VardiyaService } from '../../services/vardiya.service';
import {
    Vardiya,
    KarsilastirmaSonuc,
    KarsilastirmaDurum,
    KarsilastirmaDetay,
    OdemeYontemi,
    PompaSatis,
    YakitTuru
} from '../../models/vardiya.model';

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
    templateUrl: './karsilastirma.component.html',
    styleUrls: ['./karsilastirma.component.scss']
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
        };
        return labels[yontem] || yontem;
    }

    getOdemeIcon(yontem: OdemeYontemi): string {
        const icons: Record<OdemeYontemi, string> = {
            [OdemeYontemi.NAKIT]: 'pi pi-money-bill text-green-500',
            [OdemeYontemi.KREDI_KARTI]: 'pi pi-credit-card text-blue-500',
            [OdemeYontemi.VERESIYE]: 'pi pi-clock text-yellow-500',
            [OdemeYontemi.FILO_KARTI]: 'pi pi-car text-purple-500',
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
