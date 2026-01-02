import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
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
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';

import { MessageService } from 'primeng/api';

import { VardiyaApiService } from '../../operasyon/services/vardiya-api.service';
import { MarketApiService } from '../../operasyon/services/market-api.service';
import { AuthService } from '../../../services/auth.service';
import { VardiyaService } from '../../operasyon/services/vardiya.service';
import {
    Vardiya,
    KarsilastirmaSonuc,
    PompaSatis
} from '../../operasyon/models/vardiya.model';

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
        PanelModule,
        DatePickerModule,
        SelectModule,
        FormsModule
    ],
    providers: [MessageService],
    templateUrl: './karsilastirma.component.html',
    styleUrls: ['./karsilastirma.component.scss']
})
export class Karsilastirma implements OnInit, OnDestroy {
    sonuc: KarsilastirmaSonuc | null = null;
    gruplanmisPompaSatislari: any[] = [];
    enYogunAkaryakitPompaNo: number | null = null;
    enYogunLpgPompaNo: number | null = null;

    loading = false;
    vardiyalarLoading = false;

    karsilastirmaChartData: any;
    chartOptions: any;

    baslangicTarihi: Date = new Date();
    secilenVardiya: any = null;
    vardiyalar: any[] = [];

    // Kıyaslama Özellikleri
    karsilastirmaModu = false;
    baslangicTarihi2: Date = new Date();
    secilenVardiya2: any = null;
    vardiyalar2: any[] = [];
    sonuc2: KarsilastirmaSonuc | null = null;
    loading2 = false;
    vardiyalarLoading2 = false;
    gruplanmisPompaSatislari2: any[] = [];
    enYogunAkaryakitPompaNo2: number | null = null;
    enYogunLpgPompaNo2: number | null = null;
    karsilastirmaChartData2: any;

    isMarketSorumlusu = false;

    private subscriptions = new Subscription();

    constructor(
        private vardiyaApiService: VardiyaApiService,
        private marketApiService: MarketApiService,
        private authService: AuthService,
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private router: Router,
        private cdr: ChangeDetectorRef
    ) { }

    ngOnInit(): void {
        const user = this.authService.getCurrentUser();
        const role = user?.role?.toLowerCase() || '';
        this.isMarketSorumlusu = role.includes('market');

        this.initChart();
        this.vardiyalariYukle();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                if (vardiya && !this.secilenVardiya) {
                    this.secilenVardiya = vardiya;
                    this.karsilastirmaYap();
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

    vardiyalariYukle(): void {
        this.vardiyalarLoading = true;
        const start = new Date(this.baslangicTarihi);
        start.setHours(0, 0, 0, 0);
        const end = new Date(this.baslangicTarihi);
        end.setHours(23, 59, 59, 999);

        if (this.isMarketSorumlusu) {
            this.marketApiService.getMarketRaporu(start, end).subscribe({
                next: (res) => {
                    const vardiyalarData = res.vardiyalar || res.Vardiyalar || [];
                    this.vardiyalar = vardiyalarData.map((v: any) => ({
                        ...v,
                        vardiyaId: v.id || v.Id,
                        tarih: v.tarih || v.Tarih,
                        dosyaAdi: `Vardiya ${v.vardiyaNo || v.VardiyaNo || ''} - ${new Date(v.tarih || v.Tarih).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`
                    }));
                    this.vardiyalarLoading = false;
                    if (this.vardiyalar.length > 0 && !this.secilenVardiya) {
                        this.secilenVardiya = this.vardiyalar[0];
                        this.karsilastirmaYap();
                    }
                    this.cdr.detectChanges();
                },
                error: () => {
                    this.vardiyalarLoading = false;
                    this.cdr.detectChanges();
                }
            });
        } else {
            this.vardiyaApiService.getVardiyaRaporu(start, end).subscribe({
                next: (res) => {
                    this.vardiyalar = res.vardiyalar;
                    this.vardiyalarLoading = false;
                    if (this.vardiyalar.length > 0 && !this.secilenVardiya) {
                        this.secilenVardiya = this.vardiyalar[0];
                        this.karsilastirmaYap();
                    }
                },
                error: () => {
                    this.vardiyalarLoading = false;
                }
            });
        }
    }

    onTarihChange(): void {
        this.secilenVardiya = null;
        this.sonuc = null;
        this.gruplanmisPompaSatislari = [];
        this.vardiyalariYukle();
    }

    karsilastirmaYap(): void {
        if (!this.secilenVardiya) return;
        this.loading = true;

        if (this.isMarketSorumlusu) {
            const id = this.secilenVardiya.vardiyaId || this.secilenVardiya.id || this.secilenVardiya.Id;
            this.marketApiService.getMarketVardiyaDetay(id).subscribe({
                next: (vardiya) => {
                    const sistemToplam = vardiya.toplamSatisTutari || vardiya.ToplamSatisTutari || 0;
                    const tahsilatToplam = vardiya.toplamTeslimatTutari || vardiya.ToplamTeslimatTutari || 0;
                    const fark = vardiya.toplamFark !== undefined ? vardiya.toplamFark : vardiya.ToplamFark;

                    let durum = 'UYUMLU';
                    if (Math.abs(fark) > 10) durum = 'KRITIK_FARK';
                    else if (Math.abs(fark) > 0.1) durum = 'FARK_VAR';

                    const chartDetaylar: any[] = [{
                        odemeYontemi: 'GENEL_TOPLAM',
                        sistemTutar: sistemToplam,
                        tahsilatTutar: tahsilatToplam,
                        fark: fark
                    }];

                    const farkYuzde = sistemToplam !== 0 ? (fark / sistemToplam) * 100 : 0;

                    this.sonuc = {
                        vardiyaId: id,
                        sistemToplam,
                        tahsilatToplam,
                        fark,
                        farkYuzde,
                        durum: durum as any,
                        detaylar: chartDetaylar as any,
                        pompaSatislari: []
                    };

                    if (this.sonuc) {
                        this.updateChart(this.sonuc);
                    }
                    this.gruplaPompaSatislari([]);
                    this.loading = false;
                    this.cdr.detectChanges();
                },
                error: () => {
                    this.loading = false;
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Karşılaştırma yapılamadı' });
                    this.cdr.detectChanges();
                }
            });
        } else {
            this.vardiyaApiService.getKarsilastirma(this.secilenVardiya.id).subscribe({
                next: (sonuc) => {
                    this.sonuc = sonuc;
                    this.updateChart(sonuc);
                    this.gruplaPompaSatislari(sonuc.pompaSatislari);
                    this.loading = false;
                },
                error: () => {
                    this.loading = false;
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Karşılaştırma yapılamadı' });
                }
            });
        }
    }

    karsilastirmaModuAc(): void {
        this.karsilastirmaModu = !this.karsilastirmaModu;
        if (this.karsilastirmaModu && this.vardiyalar2.length === 0) {
            this.vardiyalariYukle2();
        }
    }

    vardiyalariYukle2(): void {
        this.vardiyalarLoading2 = true;
        const start = new Date(this.baslangicTarihi2);
        start.setHours(0, 0, 0, 0);
        const end = new Date(this.baslangicTarihi2);
        end.setHours(23, 59, 59, 999);

        if (this.isMarketSorumlusu) {
            this.marketApiService.getMarketRaporu(start, end).subscribe({
                next: (res) => {
                    const vardiyalarData = res.vardiyalar || res.Vardiyalar || [];
                    this.vardiyalar2 = vardiyalarData.map((v: any) => ({
                        ...v,
                        vardiyaId: v.id || v.Id,
                        tarih: v.tarih || v.Tarih,
                        dosyaAdi: `Vardiya ${v.vardiyaNo || v.VardiyaNo || ''} - ${new Date(v.tarih || v.Tarih).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })}`
                    }));
                    this.vardiyalarLoading2 = false;
                    if (this.vardiyalar2.length > 0 && !this.secilenVardiya2) {
                        this.secilenVardiya2 = this.vardiyalar2[0];
                        this.karsilastirmaYap2();
                    }
                    this.cdr.detectChanges();
                },
                error: () => {
                    this.vardiyalarLoading2 = false;
                    this.cdr.detectChanges();
                }
            });
        } else {
            this.vardiyaApiService.getVardiyaRaporu(start, end).subscribe({
                next: (res) => {
                    this.vardiyalar2 = res.vardiyalar;
                    this.vardiyalarLoading2 = false;
                    if (this.vardiyalar2.length > 0 && !this.secilenVardiya2) {
                        this.secilenVardiya2 = this.vardiyalar2[0];
                        this.karsilastirmaYap2();
                    }
                },
                error: () => {
                    this.vardiyalarLoading2 = false;
                }
            });
        }
    }

    onTarihChange2(): void {
        this.secilenVardiya2 = null;
        this.sonuc2 = null;
        this.gruplanmisPompaSatislari2 = [];
        this.vardiyalariYukle2();
    }

    karsilastirmaYap2(): void {
        if (!this.secilenVardiya2) return;
        this.loading2 = true;

        if (this.isMarketSorumlusu) {
            const id = this.secilenVardiya2.vardiyaId || this.secilenVardiya2.id || this.secilenVardiya2.Id;
            this.marketApiService.getMarketVardiyaDetay(id).subscribe({
                next: (vardiya) => {
                    const sistemToplam = vardiya.toplamSatisTutari || vardiya.ToplamSatisTutari || 0;
                    const tahsilatToplam = vardiya.toplamTeslimatTutari || vardiya.ToplamTeslimatTutari || 0;
                    const fark = vardiya.toplamFark !== undefined ? vardiya.toplamFark : vardiya.ToplamFark;

                    let durum = 'UYUMLU';
                    if (Math.abs(fark) > 10) durum = 'KRITIK_FARK';
                    else if (Math.abs(fark) > 0.1) durum = 'FARK_VAR';

                    const chartDetaylar: any[] = [{
                        odemeYontemi: 'GENEL_TOPLAM',
                        sistemTutar: sistemToplam,
                        tahsilatTutar: tahsilatToplam,
                        fark: fark
                    }];

                    const farkYuzde = sistemToplam !== 0 ? (fark / sistemToplam) * 100 : 0;

                    this.sonuc2 = {
                        vardiyaId: id,
                        sistemToplam,
                        tahsilatToplam,
                        fark,
                        farkYuzde,
                        durum: durum as any,
                        detaylar: chartDetaylar as any,
                        pompaSatislari: []
                    };

                    if (this.sonuc2) {
                        this.updateChart2(this.sonuc2);
                    }
                    this.gruplaPompaSatislari2([]);
                    this.loading2 = false;
                    this.cdr.detectChanges();
                },
                error: () => {
                    this.loading2 = false;
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kıyaslama yapılamadı' });
                    this.cdr.detectChanges();
                }
            });
        } else {
            this.vardiyaApiService.getKarsilastirma(this.secilenVardiya2.id).subscribe({
                next: (sonuc) => {
                    this.sonuc2 = sonuc;
                    this.updateChart2(sonuc);
                    this.gruplaPompaSatislari2(sonuc.pompaSatislari);
                    this.loading2 = false;
                },
                error: () => {
                    this.loading2 = false;
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kıyaslama yapılamadı' });
                }
            });
        }
    }

    gruplaPompaSatislari(satislar: any[]): void {
        const gruplar = this.processPompaSatislari(satislar);
        this.gruplanmisPompaSatislari = gruplar.sorted;
        this.enYogunAkaryakitPompaNo = gruplar.enYogunAkaryakit;
        this.enYogunLpgPompaNo = gruplar.enYogunLpg;
    }

    gruplaPompaSatislari2(satislar: any[]): void {
        const gruplar = this.processPompaSatislari(satislar);
        this.gruplanmisPompaSatislari2 = gruplar.sorted;
        this.enYogunAkaryakitPompaNo2 = gruplar.enYogunAkaryakit;
        this.enYogunLpgPompaNo2 = gruplar.enYogunLpg;
    }

    private processPompaSatislari(satislar: any[]) {
        const gruplar = new Map<number, any>();
        satislar.forEach(satis => {
            if (!gruplar.has(satis.pompaNo)) {
                gruplar.set(satis.pompaNo, { pompaNo: satis.pompaNo, toplamTutar: 0, toplamLitre: 0, aracSayisi: 0, satislar: [] });
            }
            const grup = gruplar.get(satis.pompaNo);
            grup.toplamTutar += satis.toplamTutar;
            grup.toplamLitre += satis.litre;
            grup.aracSayisi += (satis.islemSayisi || 0);
            grup.satislar.push(satis);
        });

        const sorted = Array.from(gruplar.values()).sort((a, b) => a.pompaNo - b.pompaNo);
        let maxAkaryakitTutar = 0, maxLpgTutar = 0, enYogunAkaryakit = null, enYogunLpg = null;

        sorted.forEach(grup => {
            let akaryakit = 0, lpg = 0;
            grup.satislar.forEach((s: any) => s.yakitTuru === 'LPG' ? lpg += s.toplamTutar : akaryakit += s.toplamTutar);
            if (akaryakit > maxAkaryakitTutar) { maxAkaryakitTutar = akaryakit; enYogunAkaryakit = grup.pompaNo; }
            if (lpg > maxLpgTutar) { maxLpgTutar = lpg; enYogunLpg = grup.pompaNo; }
        });

        return { sorted, enYogunAkaryakit, enYogunLpg };
    }

    updateChart(sonuc: KarsilastirmaSonuc): void {
        this.karsilastirmaChartData = this.prepareChartData(sonuc);
    }

    updateChart2(sonuc: KarsilastirmaSonuc): void {
        this.karsilastirmaChartData2 = this.prepareChartData(sonuc);
    }

    private prepareChartData(sonuc: KarsilastirmaSonuc) {
        return {
            labels: sonuc.detaylar.map(d => this.getOdemeLabel(d.odemeYontemi)),
            datasets: [
                { label: 'Sistem', data: sonuc.detaylar.map(d => d.sistemTutar), backgroundColor: '#3B82F6' },
                { label: 'Tahsilat', data: sonuc.detaylar.map(d => d.tahsilatTutar), backgroundColor: '#10B981' }
            ]
        };
    }

    getOdemeLabel(yontem: any): string {
        const labels: any = { 'NAKIT': 'Nakit', 'KREDI_KARTI': 'Kredi Kartı', 'PARO_PUAN': 'Paro Puan', 'MOBIL_ODEME': 'Mobil Ödeme', 'FILO': 'Filo', 'GENEL_TOPLAM': 'Genel Toplam' };
        return labels[yontem] || yontem;
    }

    getOdemeIcon(yontem: any): string {
        const icons: any = { 'NAKIT': 'pi pi-money-bill text-green-500', 'KREDI_KARTI': 'pi pi-credit-card text-blue-500', 'PARO_PUAN': 'pi pi-ticket text-yellow-500', 'MOBIL_ODEME': 'pi pi-mobile text-purple-500', 'FILO': 'pi pi-car text-orange-500', 'GENEL_TOPLAM': 'pi pi-chart-bar text-blue-500' };
        return icons[yontem] || 'pi pi-circle';
    }

    getYakitLabel(yakit: any): string {
        const labels: any = { 'BENZIN': 'Benzin', 'MOTORIN': 'Motorin', 'LPG': 'LPG', 'EURO_DIESEL': 'Euro Diesel' };
        return labels[yakit] || yakit;
    }

    getYakitBgClass(yakit: any): string {
        const classes: any = { 'BENZIN': 'bg-blue-500', 'MOTORIN': 'bg-green-600', 'LPG': 'bg-yellow-500', 'EURO_DIESEL': 'bg-indigo-500' };
        return classes[yakit] || 'bg-gray-500';
    }

    getFarkCardBackground(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return 'linear-gradient(135deg, #64748b 0%, #475569 100%)';
        switch (sonuc.durum) {
            case 'UYUMLU': return 'linear-gradient(135deg, #10b981 0%, #22c55e 50%, #16a34a 100%)';
            case 'FARK_VAR': return 'linear-gradient(135deg, #f59e0b 0%, #f97316 50%, #ea580c 100%)';
            case 'KRITIK_FARK': return 'linear-gradient(135deg, #ef4444 0%, #dc2626 50%, #b91c1c 100%)';
            default: return 'linear-gradient(135deg, #64748b 0%, #475569 100%)';
        }
    }

    getFarkCardShadow(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return 'none';
        switch (sonuc.durum) {
            case 'UYUMLU': return '0 10px 40px rgba(34, 197, 94, 0.3)';
            case 'FARK_VAR': return '0 10px 40px rgba(249, 115, 22, 0.3)';
            case 'KRITIK_FARK': return '0 10px 40px rgba(220, 38, 38, 0.3)';
            default: return 'none';
        }
    }

    getFarkIcon(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return 'pi-minus';
        if (sonuc.fark === 0) return 'pi-check';
        return sonuc.fark > 0 ? 'pi-arrow-up' : 'pi-arrow-down';
    }

    getDurumLabel(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return 'Belirsiz';
        const labels: any = { 'UYUMLU': 'Uyumlu', 'FARK_VAR': 'Fark Var', 'KRITIK_FARK': 'Kritik Fark' };
        return labels[sonuc.durum] || sonuc.durum;
    }

    getDurumSeverity(sonuc: KarsilastirmaSonuc | null): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        if (!sonuc) return 'secondary';
        const severities: any = { 'UYUMLU': 'success', 'FARK_VAR': 'warn', 'KRITIK_FARK': 'danger' };
        return severities[sonuc.durum] || 'info';
    }

    getFarkClass(fark: number): string {
        if (fark === 0) return 'text-green-500';
        return fark > 0 ? 'text-blue-500' : 'text-red-500';
    }

    getDetayDurumIcon(fark: number): string {
        if (fark === 0) return 'pi-check-circle text-green-500';
        return Math.abs(fark) < 10 ? 'pi-exclamation-circle text-yellow-500' : 'pi-times-circle text-red-500';
    }

    getDetayDurumTooltip(fark: number): string {
        if (fark === 0) return 'Uyumlu';
        return Math.abs(fark) < 10 ? 'Küçük fark' : 'Fark var';
    }

    getFarkAlertClass(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return 'bg-gray-100 text-gray-700';
        return sonuc.fark > 0 ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300' : 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300';
    }

    getFarkBaslik(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return '';
        return sonuc.fark > 0 ? 'Tahsilat Fazlası' : 'Tahsilat Eksiği';
    }

    getFarkAciklama(sonuc: KarsilastirmaSonuc | null): string {
        if (!sonuc) return '';
        const farkAbs = Math.abs(sonuc.fark).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });
        return sonuc.fark > 0 ? `Tahsilatlar sistemden ${farkAbs} fazla.` : `Tahsilatlar sistemden ${farkAbs} eksik.`;
    }

    getPompaHeaderClass(pompaNo: number, enYogunAkaryakit: number | null, enYogunLpg: number | null): string {
        if (pompaNo === enYogunAkaryakit && pompaNo === enYogunLpg) return 'bg-purple-600 text-white';
        if (pompaNo === enYogunAkaryakit) return 'bg-orange-500 text-white';
        if (pompaNo === enYogunLpg) return 'bg-blue-500 text-white';
        return 'bg-surface-700 text-white';
    }

    getPompaIconClass(pompaNo: number, enYogunAkaryakit: number | null, enYogunLpg: number | null): string {
        return 'text-white/90';
    }

    yazdir(): void {
        window.print();
    }
}
