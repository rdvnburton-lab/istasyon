import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
import { ChartModule } from 'primeng/chart'; // Added ChartModule
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
        ChartModule
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
                this.xmlStokVerileri = data.xmlKaynakli || [];
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

        // Sort by date just in case
        const sortedData = [...this.vardiyaHareketleri].sort((a, b) => new Date(a.tarih).getTime() - new Date(b.tarih).getTime());

        const labels = sortedData.map(v => {
            const d = new Date(v.tarih);
            return `${d.getDate()}.${d.getMonth() + 1}`;
        });

        // --- Stock Trend Chart (Line) ---
        // Motorin Stocks over time
        const motorinStocks = sortedData.map(v => {
            const tank = v.tanklar.find(t => t.yakitTipi === 'MOTORIN');
            return tank ? tank.bitisStok : 0; // Note: sum if multiple tanks? Assuming simplified view for now or single tank total
            // Actually vardiyaHareketleri returns array of tanks. We should sum them up by fuel type if multiple tanks exist.
        });

        // We need to aggregate tanks by fuel type for each shift
        const dailyMotorinStok = sortedData.map(v =>
            v.tanklar.filter(t => t.yakitTipi === 'MOTORIN').reduce((sum, t) => sum + t.bitisStok, 0)
        );
        const dailyBenzinStok = sortedData.map(v =>
            v.tanklar.filter(t => t.yakitTipi === 'BENZIN').reduce((sum, t) => sum + t.bitisStok, 0)
        );

        this.stockTrendData = {
            labels: labels,
            datasets: [
                {
                    label: 'Motorin Stok',
                    data: dailyMotorinStok,
                    borderColor: '#3b82f6', // blue-500
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    fill: true,
                    tension: 0.4
                },
                {
                    label: 'Benzin Stok',
                    data: dailyBenzinStok,
                    borderColor: '#22c55e', // green-500
                    backgroundColor: 'rgba(34, 197, 94, 0.1)',
                    fill: true,
                    tension: 0.4
                }
            ]
        };

        // --- Daily Sales Chart (Bar) ---
        const dailyMotorinSatis = sortedData.map(v =>
            v.tanklar.filter(t => t.yakitTipi === 'MOTORIN').reduce((sum, t) => sum + t.satilanMiktar, 0)
        );
        const dailyBenzinSatis = sortedData.map(v =>
            v.tanklar.filter(t => t.yakitTipi === 'BENZIN').reduce((sum, t) => sum + t.satilanMiktar, 0)
        );

        this.dailySalesData = {
            labels: labels,
            datasets: [
                {
                    label: 'Motorin Satış',
                    data: dailyMotorinSatis,
                    backgroundColor: '#3b82f6',
                    borderRadius: 4
                },
                {
                    label: 'Benzin Satış',
                    data: dailyBenzinSatis,
                    backgroundColor: '#22c55e',
                    borderRadius: 4
                }
            ]
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
