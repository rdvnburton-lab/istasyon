import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';
import { DashboardService } from '../../../services/dashboard.service';
import { SorumluDashboardDto } from '../../../models/dashboard.model';

// Dashboard component for supervisors (Pump, Market, or Station)
@Component({
    selector: 'app-sorumlu-dashboard',
    standalone: true,
    imports: [CommonModule, CardModule, ButtonModule],
    template: `
        <div class="grid grid-cols-12 gap-8" *ngIf="data">
            <!-- Welcome Banner -->
            <div class="col-span-12">
                <div class="relative overflow-hidden rounded-2xl p-8 bg-gradient-to-r from-primary-600 to-primary-400 shadow-lg">
                    <div class="relative z-10 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 text-white">
                        <div>
                            <h1 class="text-3xl font-bold mb-2">Hoşgeldiniz, {{data.kullaniciAdi}} 👋</h1>
                            <p class="text-primary-100 text-lg opacity-90">
                                {{ getRoleLabel(data.rol) }} Paneli
                            </p>
                        </div>
                        <div class="flex items-center gap-3 bg-white/10 backdrop-blur-sm rounded-xl p-4 border border-white/20">
                            <div class="w-12 h-12 rounded-full bg-white/20 flex items-center justify-center">
                                <i class="pi pi-building text-2xl"></i>
                            </div>
                            <div>
                                <div class="text-xs text-primary-100 uppercase tracking-wider">Firma / İstasyon</div>
                                <div class="font-semibold text-lg">{{data.firmaAdi}} / {{data.istasyonAdi}}</div>
                            </div>
                        </div>
                    </div>
                    <!-- Decorative Circles -->
                    <div class="absolute top-0 right-0 -mr-16 -mt-16 w-64 h-64 rounded-full bg-white/10 blur-3xl"></div>
                    <div class="absolute bottom-0 left-0 -ml-16 -mb-16 w-48 h-48 rounded-full bg-black/10 blur-2xl"></div>
                </div>
            </div>

            <!-- Main Actions & Status -->
            <div class="col-span-12 lg:col-span-8">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                    <!-- Active Shift Card (Pump) -->
                    <div *ngIf="data.rol === 'vardiya_sorumlusu' || data.rol === 'istasyon_sorumlusu'" 
                        class="card border-0 shadow-md h-full relative overflow-hidden group transition-all hover:shadow-lg">
                        <div class="absolute top-0 right-0 w-32 h-32 bg-green-500/10 rounded-bl-full -mr-8 -mt-8 transition-transform group-hover:scale-110"></div>
                        
                        <div class="flex flex-col h-full justify-between relative z-10">
                            <div class="mb-4">
                                <div class="flex items-center gap-3 mb-3">
                                    <div class="w-10 h-10 rounded-lg bg-green-100 dark:bg-green-900/30 flex items-center justify-center text-green-600 dark:text-green-400">
                                        <i class="pi pi-clock text-xl"></i>
                                    </div>
                                    <span class="text-lg font-semibold text-gray-700 dark:text-gray-200">Pompa Vardiyası</span>
                                </div>
                                
                                <div *ngIf="data.aktifVardiyaId; else noShift">
                                    <h3 class="text-2xl font-bold text-green-600 dark:text-green-400 mb-1">Vardiya Devam Ediyor</h3>
                                    <p class="text-gray-500">Vardiya #{{data.aktifVardiyaId}} şu anda aktif.</p>
                                </div>
                                <ng-template #noShift>
                                    <h3 class="text-2xl font-bold text-gray-700 dark:text-gray-200 mb-1">Aktif Vardiya Yok</h3>
                                    <p class="text-gray-500">Yeni bir vardiya başlatmak için hazırsınız.</p>
                                </ng-template>
                            </div>

                            <div class="mt-4">
                                <button *ngIf="data.aktifVardiyaId" pButton label="Vardiyaya Git" icon="pi pi-arrow-right" 
                                    class="p-button-success w-full justify-center font-bold py-3" (click)="goToVardiya(data.aktifVardiyaId!)">
                                </button>
                                <button *ngIf="!data.aktifVardiyaId" pButton label="Yeni Vardiya Başlat" icon="pi pi-plus" 
                                    class="p-button-primary w-full justify-center font-bold py-3" (click)="startVardiya()">
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Active Market Shift Card -->
                    <div *ngIf="data.rol === 'market_sorumlusu' || data.rol === 'istasyon_sorumlusu'" 
                        class="card border-0 shadow-md h-full relative overflow-hidden group transition-all hover:shadow-lg">
                        <div class="absolute top-0 right-0 w-32 h-32 bg-blue-500/10 rounded-bl-full -mr-8 -mt-8 transition-transform group-hover:scale-110"></div>
                        
                        <div class="flex flex-col h-full justify-between relative z-10">
                            <div class="mb-4">
                                <div class="flex items-center gap-3 mb-3">
                                    <div class="w-10 h-10 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center text-blue-600 dark:text-blue-400">
                                        <i class="pi pi-shopping-cart text-xl"></i>
                                    </div>
                                    <span class="text-lg font-semibold text-gray-700 dark:text-gray-200">Market Vardiyası</span>
                                </div>
                                
                                <div *ngIf="data.aktifMarketVardiyaId; else noMarketShift">
                                    <h3 class="text-2xl font-bold text-blue-600 dark:text-blue-400 mb-1">Market Vardiyası Aktif</h3>
                                    <p class="text-gray-500">Market mutabakatı devam ediyor.</p>
                                </div>
                                <ng-template #noMarketShift>
                                    <h3 class="text-2xl font-bold text-gray-700 dark:text-gray-200 mb-1">Aktif Market Vardiyası Yok</h3>
                                    <p class="text-gray-500">Yeni bir market vardiyası başlatabilirsiniz.</p>
                                </ng-template>
                            </div>

                            <div class="mt-4">
                                <button pButton label="Market Paneline Git" icon="pi pi-shopping-bag" 
                                    class="p-button-info w-full justify-center font-bold py-3" (click)="router.navigate(['/vardiya/market'])">
                                </button>
                            </div>
                        </div>
                    </div>

                    <!-- Pending Approvals Card -->
                    <div class="card border-0 shadow-md h-full relative overflow-hidden group transition-all hover:shadow-lg"
                        [ngClass]="{'md:col-span-2': data.rol !== 'istasyon_sorumlusu'}">
                        <div class="absolute top-0 right-0 w-32 h-32 bg-orange-500/10 rounded-bl-full -mr-8 -mt-8 transition-transform group-hover:scale-110"></div>
                        
                        <div class="flex flex-col h-full justify-between relative z-10">
                            <div>
                                <div class="flex items-center gap-3 mb-3">
                                    <div class="w-10 h-10 rounded-lg bg-orange-100 dark:bg-orange-900/30 flex items-center justify-center text-orange-600 dark:text-orange-400">
                                        <i class="pi pi-exclamation-circle text-xl"></i>
                                    </div>
                                    <span class="text-lg font-semibold text-gray-700 dark:text-gray-200">Bekleyen İşlemler</span>
                                </div>
                                
                                <div class="flex items-baseline gap-2">
                                    <span class="text-4xl font-bold text-gray-900 dark:text-white">{{data.bekleyenOnaySayisi + (data.bekleyenMarketOnaySayisi || 0)}}</span>
                                    <span class="text-gray-500">adet onay bekleyen</span>
                                </div>
                                <p class="text-sm text-gray-500 mt-2">Onay bekleyen vardiya veya işlemleriniz.</p>
                            </div>
                            
                            <div class="mt-4">
                                <button pButton label="Listeyi Görüntüle" icon="pi pi-list" 
                                    class="p-button-outlined p-button-secondary w-full justify-center" 
                                    (click)="router.navigate(['/vardiya'])">
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Stats & Summary -->
            <div class="col-span-12 lg:col-span-4">
                <div class="card border-0 shadow-md h-full bg-surface-0 dark:bg-surface-900">
                    <div class="flex items-center justify-between mb-6">
                        <h3 class="text-lg font-bold text-gray-800 dark:text-white">Son Vardiya Özeti</h3>
                        <span class="px-3 py-1 rounded-full bg-purple-100 dark:bg-purple-900/30 text-purple-600 dark:text-purple-400 text-xs font-bold">
                            Tamamlandı
                        </span>
                    </div>

                    <div class="space-y-8">
                        <!-- Pompa Özeti -->
                        <div *ngIf="data.rol === 'vardiya_sorumlusu' || data.rol === 'istasyon_sorumlusu'" class="space-y-4">
                            <div class="flex items-center gap-2 text-sm font-semibold text-gray-500 uppercase tracking-wider">
                                <i class="pi pi-bolt text-green-500"></i>
                                <span>Pompa Vardiyası</span>
                            </div>
                            <div class="p-4 rounded-xl bg-green-50 dark:bg-green-900/10 border border-green-100 dark:border-green-900/20">
                                <div class="text-sm text-gray-500 mb-1">Toplam Tutar</div>
                                <div class="text-2xl font-bold text-green-600 dark:text-green-400">
                                    {{data.sonVardiyaTutar | currency:'TRY':'symbol-narrow':'1.2-2'}}
                                </div>
                                <div class="mt-2 flex items-center gap-2 text-xs text-gray-500">
                                    <i class="pi pi-calendar"></i>
                                    <span>{{data.sonVardiyaTarihi ? (data.sonVardiyaTarihi | date:'dd.MM.yyyy HH:mm') : 'Veri yok'}}</span>
                                </div>
                            </div>
                        </div>

                        <!-- Market Özeti -->
                        <div *ngIf="data.rol === 'market_sorumlusu' || data.rol === 'istasyon_sorumlusu'" class="space-y-4">
                            <div class="flex items-center gap-2 text-sm font-semibold text-gray-500 uppercase tracking-wider">
                                <i class="pi pi-shopping-bag text-blue-500"></i>
                                <span>Market Vardiyası</span>
                            </div>
                            <div class="p-4 rounded-xl bg-blue-50 dark:bg-blue-900/10 border border-blue-100 dark:border-blue-900/20">
                                <div class="text-sm text-gray-500 mb-1">Toplam Tutar</div>
                                <div class="text-2xl font-bold text-blue-600 dark:text-blue-400">
                                    {{data.sonMarketVardiyaTutar | currency:'TRY':'symbol-narrow':'1.2-2'}}
                                </div>
                                <div class="mt-2 flex items-center gap-2 text-xs text-gray-500">
                                    <i class="pi pi-calendar"></i>
                                    <span>{{data.sonMarketVardiyaTarihi ? (data.sonMarketVardiyaTarihi | date:'dd.MM.yyyy HH:mm') : 'Veri yok'}}</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `
})
export class SorumluDashboardComponent implements OnInit {
    data: SorumluDashboardDto | null = null;

    constructor(
        private dashboardService: DashboardService,
        public router: Router
    ) { }

    ngOnInit() {
        this.dashboardService.getSorumluSummary().subscribe((data: SorumluDashboardDto) => {
            this.data = data;
        });
    }

    goToVardiya(id: number) {
        this.router.navigate(['/vardiya/pompa-yonetimi', id]);
    }

    startVardiya() {
        this.router.navigate(['/vardiya/vardiya-baslat']);
    }

    getRoleLabel(role: string): string {
        switch (role) {
            case 'vardiya_sorumlusu': return 'Vardiya Sorumlusu';
            case 'market_sorumlusu': return 'Market Sorumlusu';
            case 'istasyon_sorumlusu': return 'İstasyon Sorumlusu';
            default: return 'Sorumlu';
        }
    }
}
