import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { StokService, KarmaStokOzet, XmlStokOzet, VardiyaTankHareket } from '../services/stok.service';

@Component({
    selector: 'app-yakit-stok',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        CardModule,
        SelectModule,
        ToastModule,
        ChartModule,
        TableModule
    ],
    providers: [MessageService],
    templateUrl: './yakit-stok.component.html',
    styleUrls: ['./yakit-stok.component.scss']
})
export class YakitStokComponent implements OnInit {
    // Date Filters
    selectedMonth: any;
    selectedYear: any;

    years: any[] = [];
    months = [
        { label: 'Ocak', value: 0 }, { label: 'Şubat', value: 1 }, { label: 'Mart', value: 2 },
        { label: 'Nisan', value: 3 }, { label: 'Mayıs', value: 4 }, { label: 'Haziran', value: 5 },
        { label: 'Temmuz', value: 6 }, { label: 'Ağustos', value: 7 }, { label: 'Eylül', value: 8 },
        { label: 'Ekim', value: 9 }, { label: 'Kasım', value: 10 }, { label: 'Aralık', value: 11 }
    ];

    loading: boolean = false;

    // Data
    karmaStokOzet: KarmaStokOzet | null = null;
    xmlStokVerileri: XmlStokOzet[] = [];
    vardiyaHareketleri: VardiyaTankHareket[] = [];

    // KPI Metrics
    totalStockCapaicty: number = 0; // Optional if we knew tank sizes
    totalCurrentStock: number = 0;
    totalMonthlySales: number = 0;
    estimatedDaysLeft: number = 0;
    stockHealthScore: number = 0; // 0-100 score based on stock vs sales

    // Chart Data
    stockTrendData: any;
    dailySalesData: any;
    chartOptions: any;

    constructor(
        private stokService: StokService
    ) {
        const currentYear = new Date().getFullYear();
        for (let i = currentYear; i >= currentYear - 5; i--) {
            this.years.push({ label: i.toString(), value: i });
        }
    }

    ngOnInit() {
        this.selectedMonth = this.months[new Date().getMonth()];
        this.selectedYear = this.years[0]; // Current Year
        this.initChartOptions();
        this.loadData();
    }

    loadData() {
        this.loading = true;

        this.stokService.getKarmaStokOzeti(this.selectedYear.value, this.selectedMonth.value + 1).subscribe({
            next: (data) => {
                this.karmaStokOzet = data;

                // Merge XML and Manual (LPG) data for display
                const xmlData = data.xmlKaynakli || [];
                const manuelData = (data.manuelKaynakli || []).map(m => ({
                    yakitTipi: m.yakitTipi,
                    renk: m.renk || '#3b82f6', // Use backend color or default LPG color
                    toplamSevkiyat: m.toplamGiris,
                    toplamSatis: m.toplamSatis,
                    sonStok: 0, // Not tracked via XML
                    ilkStok: 0,
                    toplamFark: 0,
                    kayitSayisi: xmlData.length > 0 ? xmlData[0].kayitSayisi : 0
                }));

                this.xmlStokVerileri = [...xmlData, ...manuelData];
                this.vardiyaHareketleri = data.vardiyaHareketleri || [];

                this.calculateKPIs();
                this.prepareCharts();

                this.loading = false;
            },
            error: (err) => {
                console.error('Karma stok özeti yüklenemedi:', err);
                this.loading = false;
            }
        });
    }

    calculateKPIs() {
        if (!this.xmlStokVerileri.length) {
            this.totalCurrentStock = 0;
            this.totalMonthlySales = 0;
            this.estimatedDaysLeft = 0;
            return;
        }

        // 1. Total Current Stock
        this.totalCurrentStock = this.xmlStokVerileri.reduce((acc, item) => acc + item.sonStok, 0);

        // 2. Total Monthly Sales
        this.totalMonthlySales = this.xmlStokVerileri.reduce((acc, item) => acc + item.toplamSatis, 0);

        // 3. Estimated Days Remaining (Simple Avg Calculation)
        // Avg Daily Sales = Total Sales / Days Passed (or record count)
        // If we have proper shift dates, we can count distinct days. 
        // For approximation, we'll use vardiyaHareketleri length or day of month.

        let daysPassed = this.vardiyaHareketleri.length > 0 ? this.vardiyaHareketleri.length : 1;
        // Or strictly use current day of month if current month
        const now = new Date();
        if (this.selectedYear.value === now.getFullYear() && this.selectedMonth.value === now.getMonth()) {
            daysPassed = Math.max(1, now.getDate());
        } else {
            // For past months, use ~30 days or actual count
            daysPassed = Math.max(1, this.vardiyaHareketleri.length);
        }

        const avgDailySales = this.totalMonthlySales / daysPassed;

        if (avgDailySales > 0) {
            this.estimatedDaysLeft = Math.round(this.totalCurrentStock / avgDailySales);
        } else {
            this.estimatedDaysLeft = 999; // Plenty of stock / no sales
        }

        // Cap reasonable display
        if (this.estimatedDaysLeft > 90) this.estimatedDaysLeft = 90;
    }

    prepareCharts() {
        if (!this.vardiyaHareketleri || this.vardiyaHareketleri.length === 0) return;

        // Sort by date
        const sortedData = [...this.vardiyaHareketleri].sort((a, b) => new Date(a.tarih).getTime() - new Date(b.tarih).getTime());

        const labels = sortedData.map(v => {
            const d = new Date(v.tarih);
            return `${d.getDate()}.${d.getMonth() + 1}`;
        });

        // Get all unique fuel types across all shifts
        const fuelTypes = new Set<string>();
        const fuelColors: { [key: string]: string } = {};

        sortedData.forEach(v => {
            v.tanklar.forEach(t => {
                fuelTypes.add(t.yakitTipi);
                if (t.renk) fuelColors[t.yakitTipi] = t.renk;
            });
        });

        const fuelTypeList = Array.from(fuelTypes);

        // --- Stock Trend Chart (Line) ---
        const stockDatasets = fuelTypeList.map(ft => {
            const color = fuelColors[ft] || '#666';
            const data = sortedData.map(v =>
                v.tanklar.filter(t => t.yakitTipi === ft).reduce((sum, t) => sum + t.bitisStok, 0)
            );

            return {
                label: ft + ' Stok',
                data: data,
                borderColor: color,
                backgroundColor: color + '1A', // 10% opacity
                fill: true,
                tension: 0.4
            };
        });

        this.stockTrendData = {
            labels: labels,
            datasets: stockDatasets
        };

        // --- Daily Sales Chart (Bar) ---
        const salesDatasets = fuelTypeList.map(ft => {
            const color = fuelColors[ft] || '#666';
            const data = sortedData.map(v =>
                v.tanklar.filter(t => t.yakitTipi === ft).reduce((sum, t) => sum + t.satilanMiktar, 0)
            );

            return {
                label: ft + ' Satış',
                data: data,
                backgroundColor: color,
                borderRadius: 4
            };
        });

        this.dailySalesData = {
            labels: labels,
            datasets: salesDatasets
        };
    }

    initChartOptions() {
        const documentStyle = getComputedStyle(document.documentElement);
        const textColor = documentStyle.getPropertyValue('--text-color') || '#4b5563';
        const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary') || '#9ca3af';
        const surfaceBorder = documentStyle.getPropertyValue('--surface-border') || '#e5e7eb';

        this.chartOptions = {
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

    onDateChange() {
        this.loadData();
    }
}
