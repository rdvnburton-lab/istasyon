import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { ChartModule } from 'primeng/chart';
import { ProgressBarModule } from 'primeng/progressbar';

import { VardiyaService } from '../services/vardiya.service';
import { Vardiya, VardiyaDurum, GenelOzet, PompaOzet, FarkDurum } from '../models/vardiya.model';

@Component({
    selector: 'app-vardiya-dashboard',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TagModule,
        DividerModule,
        ChartModule,
        ProgressBarModule
    ],
    template: `
        <div class="grid grid-cols-12 gap-6">
            <!-- Başlık ve Durum -->
            <div class="col-span-12">
                <div class="card">
                    <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                        <div>
                            <h2 class="text-2xl font-bold text-surface-800 dark:text-surface-100 m-0">
                                <i class="pi pi-clock mr-2"></i>Vardiya Yönetimi
                            </h2>
                            <p class="text-surface-500 mt-2">
                                {{ aktifVardiya ? 'Aktif vardiyayı yönetin' : 'Yeni bir vardiya başlatın' }}
                            </p>
                        </div>
                        <div class="flex gap-2 flex-wrap">
                            <p-button 
                                *ngIf="!aktifVardiya" 
                                label="Vardiya Başlat" 
                                icon="pi pi-play" 
                                severity="success"
                                [routerLink]="['/vardiya/baslat']">
                            </p-button>
                            <ng-container *ngIf="aktifVardiya">
                                <p-button 
                                    label="Pompa" 
                                    icon="pi pi-box"
                                    severity="info"
                                    [routerLink]="['/vardiya/pompa']">
                                </p-button>
                                <p-button 
                                    label="Market" 
                                    icon="pi pi-shopping-bag"
                                    severity="help"
                                    [routerLink]="['/vardiya/market']">
                                </p-button>
                                <p-button 
                                    label="Onaya Gönder" 
                                    icon="pi pi-send" 
                                    severity="warn"
                                    (onClick)="onayaGonder()">
                                </p-button>
                            </ng-container>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Aktif Vardiya Bilgisi -->
            <div class="col-span-12 lg:col-span-4">
                <div class="card h-full">
                    <div class="flex items-center justify-between mb-4">
                        <h5 class="m-0 font-semibold">Vardiya Durumu</h5>
                        <p-tag 
                            [value]="getVardiyaDurumLabel()" 
                            [severity]="getVardiyaDurumSeverity()">
                        </p-tag>
                    </div>
                    
                    <div *ngIf="aktifVardiya; else noVardiya">
                        <div class="flex flex-col gap-4">
                            <div class="flex items-center gap-3 p-3 bg-surface-50 dark:bg-surface-800 rounded-lg">
                                <i class="pi pi-user text-2xl text-primary"></i>
                                <div>
                                    <span class="text-surface-500 text-sm">Sorumlu</span>
                                    <p class="font-semibold m-0">{{ aktifVardiya.sorumluAdi }}</p>
                                </div>
                            </div>
                            <div class="flex items-center gap-3 p-3 bg-surface-50 dark:bg-surface-800 rounded-lg">
                                <i class="pi pi-building text-2xl text-primary"></i>
                                <div>
                                    <span class="text-surface-500 text-sm">İstasyon</span>
                                    <p class="font-semibold m-0">{{ aktifVardiya.istasyonAdi }}</p>
                                </div>
                            </div>
                            <div class="flex items-center gap-3 p-3 bg-surface-50 dark:bg-surface-800 rounded-lg">
                                <i class="pi pi-calendar text-2xl text-primary"></i>
                                <div>
                                    <span class="text-surface-500 text-sm">Başlangıç</span>
                                    <p class="font-semibold m-0">{{ aktifVardiya.baslangicTarihi | date:'dd.MM.yyyy HH:mm' }}</p>
                                </div>
                            </div>
                            <div class="flex items-center gap-3 p-3 bg-surface-50 dark:bg-surface-800 rounded-lg">
                                <i class="pi pi-hourglass text-2xl text-primary"></i>
                                <div>
                                    <span class="text-surface-500 text-sm">Geçen Süre</span>
                                    <p class="font-semibold m-0">{{ getGecenSure() }}</p>
                                </div>
                            </div>
                        </div>

                        <!-- Hızlı İşlemler -->
                        <div class="mt-4 pt-4 border-t border-surface-200 dark:border-surface-700">
                            <h6 class="font-semibold mb-3">Hızlı İşlemler</h6>
                            <div class="flex flex-col gap-2">
                                <p-button 
                                    label="Pompa Yönetimi" 
                                    icon="pi pi-box" 
                                    severity="info"
                                    styleClass="w-full"
                                    [outlined]="true"
                                    [routerLink]="['/vardiya/pompa']">
                                </p-button>
                                <p-button 
                                    label="Market Yönetimi" 
                                    icon="pi pi-shopping-bag" 
                                    severity="help"
                                    styleClass="w-full"
                                    [outlined]="true"
                                    [routerLink]="['/vardiya/market']">
                                </p-button>
                            </div>
                        </div>
                    </div>
                    
                    <ng-template #noVardiya>
                        <div class="flex flex-col items-center justify-center py-8">
                            <i class="pi pi-inbox text-6xl text-surface-300 mb-4"></i>
                            <p class="text-surface-500 text-center">
                                Şu anda aktif bir vardiya bulunmuyor.<br>
                                Yeni bir vardiya başlatmak için butona tıklayın.
                            </p>
                            <p-button 
                                label="Vardiya Başlat" 
                                icon="pi pi-play" 
                                severity="success"
                                styleClass="mt-4"
                                [routerLink]="['/vardiya/baslat']">
                            </p-button>
                        </div>
                    </ng-template>
                </div>
            </div>

            <!-- Özet Kartları -->
            <div class="col-span-12 lg:col-span-8" *ngIf="aktifVardiya">
                <div class="grid grid-cols-12 gap-6">
                    <!-- Pompa Toplam -->
                    <div class="col-span-12 md:col-span-4">
                        <div class="card h-full bg-gradient-to-br from-blue-500 to-blue-600 text-white">
                            <div class="flex justify-between items-start">
                                <div>
                                    <span class="text-white/80 text-sm">Pompa Satışı</span>
                                    <h3 class="text-2xl font-bold m-0 mt-2">
                                        {{ pompaOzet?.toplamOtomasyonSatis | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </h3>
                                </div>
                                <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                                    <i class="pi pi-box text-2xl"></i>
                                </div>
                            </div>
                            <div class="mt-4 pt-4 border-t border-white/20">
                                <span class="text-white/80 text-sm">
                                    <i class="pi pi-users mr-1"></i>
                                    {{ pompaOzet?.personelSayisi || 0 }} Pompacı
                                </span>
                            </div>
                        </div>
                    </div>

                    <!-- Pompa Tahsilat -->
                    <div class="col-span-12 md:col-span-4">
                        <div class="card h-full bg-gradient-to-br from-green-500 to-green-600 text-white">
                            <div class="flex justify-between items-start">
                                <div>
                                    <span class="text-white/80 text-sm">Pompa Tahsilatı</span>
                                    <h3 class="text-2xl font-bold m-0 mt-2">
                                        {{ pompaOzet?.toplamPusulaTahsilat | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </h3>
                                </div>
                                <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                                    <i class="pi pi-wallet text-2xl"></i>
                                </div>
                            </div>
                            <div class="mt-4 pt-4 border-t border-white/20">
                                <span class="text-white/80 text-sm">
                                    <i class="pi pi-pencil mr-1"></i>
                                    Pusula Girişleri
                                </span>
                            </div>
                        </div>
                    </div>

                    <!-- Pompa Fark -->
                    <div class="col-span-12 md:col-span-4">
                        <div class="card h-full text-white" [ngClass]="getPompaFarkCardClass()">
                            <div class="flex justify-between items-start">
                                <div>
                                    <span class="text-white/80 text-sm">Pompa Farkı</span>
                                    <h3 class="text-2xl font-bold m-0 mt-2">
                                        {{ pompaOzet?.toplamFark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </h3>
                                </div>
                                <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                                    <i class="pi text-2xl" [ngClass]="getPompaFarkIcon()"></i>
                                </div>
                            </div>
                            <div class="mt-4 pt-4 border-t border-white/20">
                                <span class="text-white/80 text-sm">
                                    <i class="pi pi-info-circle mr-1"></i>
                                    {{ getPompaFarkAciklama() }}
                                </span>
                            </div>
                        </div>
                    </div>

                    <!-- Genel Özet -->
                    <div class="col-span-12 md:col-span-6">
                        <div class="card h-full">
                            <h5 class="font-semibold mb-4">
                                <i class="pi pi-chart-pie mr-2 text-primary"></i>Genel Özet
                            </h5>
                            <div class="flex flex-col gap-3">
                                <div class="flex justify-between items-center p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                                    <span class="flex items-center gap-2">
                                        <i class="pi pi-box text-blue-500"></i>
                                        <span>Pompa Ciro</span>
                                    </span>
                                    <span class="font-bold text-blue-600">
                                        {{ genelOzet?.pompaToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                                <div class="flex justify-between items-center p-3 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                                    <span class="flex items-center gap-2">
                                        <i class="pi pi-shopping-bag text-purple-500"></i>
                                        <span>Market Ciro</span>
                                    </span>
                                    <span class="font-bold text-purple-600">
                                        {{ genelOzet?.marketToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                                <div class="flex justify-between items-center p-3 bg-surface-100 dark:bg-surface-800 rounded-lg font-bold text-lg">
                                    <span>Genel Toplam</span>
                                    <span class="text-primary">
                                        {{ genelOzet?.genelToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Tahsilat Dağılımı -->
                    <div class="col-span-12 md:col-span-6">
                        <div class="card h-full">
                            <h5 class="font-semibold mb-4">
                                <i class="pi pi-wallet mr-2 text-green-500"></i>Tahsilat Dağılımı
                            </h5>
                            <div class="flex flex-col gap-3">
                                <div class="flex justify-between items-center p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
                                    <span class="flex items-center gap-2">
                                        <i class="pi pi-money-bill text-green-500"></i>
                                        <span>Nakit</span>
                                    </span>
                                    <span class="font-bold text-green-600">
                                        {{ genelOzet?.toplamNakit | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                                <div class="flex justify-between items-center p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                                    <span class="flex items-center gap-2">
                                        <i class="pi pi-credit-card text-blue-500"></i>
                                        <span>Kredi Kartı</span>
                                    </span>
                                    <span class="font-bold text-blue-600">
                                        {{ genelOzet?.toplamKrediKarti | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                                <div class="flex justify-between items-center p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
                                    <span class="flex items-center gap-2">
                                        <i class="pi pi-clock text-yellow-500"></i>
                                        <span>Veresiye</span>
                                    </span>
                                    <span class="font-bold text-yellow-600">
                                        {{ genelOzet?.toplamVeresiye | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Boş durum kartları -->
            <div class="col-span-12 lg:col-span-8" *ngIf="!aktifVardiya">
                <div class="card h-full flex items-center justify-center">
                    <div class="text-center py-8">
                        <i class="pi pi-chart-pie text-6xl text-surface-300 mb-4"></i>
                        <h4 class="text-surface-500">İstatistikler</h4>
                        <p class="text-surface-400">Vardiya başladığında burada satış ve tahsilat istatistikleri görünecek.</p>
                    </div>
                </div>
            </div>
        </div>
    `,
    styles: [`
        :host {
            display: block;
        }
    `]
})
export class VardiyaDashboard implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    pompaOzet: PompaOzet | null = null;
    genelOzet: GenelOzet | null = null;

    private subscriptions = new Subscription();

    constructor(private vardiyaService: VardiyaService) { }

    ngOnInit(): void {
        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (vardiya) {
                    this.loadOzetler(vardiya.id);
                }
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    loadOzetler(vardiyaId: number): void {
        this.vardiyaService.getPompaOzet(vardiyaId).subscribe(ozet => {
            this.pompaOzet = ozet;
        });

        this.vardiyaService.getGenelOzet(vardiyaId).subscribe(ozet => {
            this.genelOzet = ozet;
        });
    }

    getVardiyaDurumLabel(): string {
        if (!this.aktifVardiya) return 'Kapalı';

        const labels: Record<VardiyaDurum, string> = {
            [VardiyaDurum.ACIK]: 'Açık',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'Onay Bekliyor',
            [VardiyaDurum.ONAYLANDI]: 'Onaylandı',
            [VardiyaDurum.REDDEDILDI]: 'Reddedildi'
        };
        return labels[this.aktifVardiya.durum];
    }

    getVardiyaDurumSeverity(): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        if (!this.aktifVardiya) return 'secondary';

        const severities: Record<VardiyaDurum, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            [VardiyaDurum.ACIK]: 'success',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'warn',
            [VardiyaDurum.ONAYLANDI]: 'info',
            [VardiyaDurum.REDDEDILDI]: 'danger'
        };
        return severities[this.aktifVardiya.durum];
    }

    getGecenSure(): string {
        if (!this.aktifVardiya) return '-';

        const now = new Date();
        const start = new Date(this.aktifVardiya.baslangicTarihi);
        const diff = now.getTime() - start.getTime();

        const hours = Math.floor(diff / (1000 * 60 * 60));
        const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

        return `${hours} saat ${minutes} dakika`;
    }

    getPompaFarkCardClass(): string {
        if (!this.pompaOzet) return 'bg-gradient-to-br from-gray-500 to-gray-600';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'bg-gradient-to-br from-green-500 to-green-600';
            case FarkDurum.ACIK:
                return 'bg-gradient-to-br from-red-500 to-red-600';
            case FarkDurum.FAZLA:
                return 'bg-gradient-to-br from-blue-500 to-blue-600';
            default:
                return 'bg-gradient-to-br from-gray-500 to-gray-600';
        }
    }

    getPompaFarkIcon(): string {
        if (!this.pompaOzet) return 'pi-minus';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'pi-check';
            case FarkDurum.ACIK:
                return 'pi-arrow-down';
            case FarkDurum.FAZLA:
                return 'pi-arrow-up';
            default:
                return 'pi-minus';
        }
    }

    getPompaFarkAciklama(): string {
        if (!this.pompaOzet) return 'Veri bekleniyor';

        switch (this.pompaOzet.farkDurum) {
            case FarkDurum.UYUMLU:
                return 'Uyumlu';
            case FarkDurum.ACIK:
                return 'Kasa Açığı!';
            case FarkDurum.FAZLA:
                return 'Kasa Fazlası';
            default:
                return 'Belirsiz';
        }
    }

    async onayaGonder(): Promise<void> {
        if (this.aktifVardiya) {
            try {
                await this.vardiyaService.vardiyaOnayaGonder(this.aktifVardiya.id);
                // Başarılı
            } catch (err) {
                console.error('Onaya gönderilirken hata:', err);
            }
        }
    }
}
