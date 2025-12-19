import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageModule } from 'primeng/message';
import { StepperModule } from 'primeng/stepper';
import { InputTextModule } from 'primeng/inputtext';

import { VardiyaService } from '../services/vardiya.service';
import { Istasyon, Operator } from '../models/vardiya.model';

@Component({
    selector: 'app-vardiya-baslat',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        SelectModule,
        DatePickerModule,
        MessageModule,
        StepperModule,
        InputTextModule
    ],
    template: `
        <div class="grid grid-cols-12 gap-6">
            <div class="col-span-12">
                <div class="card">
                    <h2 class="text-2xl font-bold text-surface-800 dark:text-surface-100 m-0 mb-4">
                        <i class="pi pi-play-circle mr-2 text-green-500"></i>Yeni Vardiya Başlat
                    </h2>
                    <p class="text-surface-500 mb-6">
                        Vardiya bilgilerini girerek yeni bir vardiya başlatın.
                    </p>

                    <div class="grid grid-cols-12 gap-6">
                        <!-- Sol Panel - Form -->
                        <div class="col-span-12 lg:col-span-7">
                            <div class="surface-border border rounded-lg p-6">
                                <div class="flex flex-col gap-6">
                                    <!-- İstasyon Seçimi -->
                                    <div class="flex flex-col gap-2">
                                        <label for="istasyon" class="font-semibold">
                                            <i class="pi pi-building mr-2"></i>İstasyon
                                        </label>
                                        <p-select 
                                            id="istasyon"
                                            [(ngModel)]="selectedIstasyon" 
                                            [options]="istasyonlar" 
                                            optionLabel="ad"
                                            placeholder="İstasyon Seçin"
                                            styleClass="w-full"
                                            (onChange)="onIstasyonChange()">
                                            <ng-template #item let-istasyon>
                                                <div class="flex items-center gap-2">
                                                    <i class="pi pi-building"></i>
                                                    <span>{{ istasyon.ad }}</span>
                                                    <span class="text-surface-400 text-sm">({{ istasyon.kod }})</span>
                                                </div>
                                            </ng-template>
                                        </p-select>
                                    </div>

                                    <!-- Operatör Seçimi -->
                                    <div class="flex flex-col gap-2">
                                        <label for="operator" class="font-semibold">
                                            <i class="pi pi-user mr-2"></i>Operatör
                                        </label>
                                        <p-select 
                                            id="operator"
                                            [(ngModel)]="selectedOperator" 
                                            [options]="operatorler" 
                                            optionLabel="ad"
                                            placeholder="Operatör Seçin"
                                            styleClass="w-full"
                                            [disabled]="!selectedIstasyon">
                                            <ng-template #item let-operator>
                                                <div class="flex items-center gap-2">
                                                    <i class="pi pi-user"></i>
                                                    <span>{{ operator.ad }} {{ operator.soyad }}</span>
                                                </div>
                                            </ng-template>
                                            <ng-template #selectedItem let-operator>
                                                <span *ngIf="operator">{{ operator.ad }} {{ operator.soyad }}</span>
                                            </ng-template>
                                        </p-select>
                                    </div>

                                    <!-- Tarih/Saat -->
                                    <div class="flex flex-col gap-2">
                                        <label for="tarih" class="font-semibold">
                                            <i class="pi pi-calendar mr-2"></i>Başlangıç Tarihi/Saati
                                        </label>
                                        <p-datepicker 
                                            id="tarih"
                                            [(ngModel)]="baslangicTarihi" 
                                            [showTime]="true" 
                                            [showIcon]="true"
                                            dateFormat="dd.mm.yy"
                                            hourFormat="24"
                                            styleClass="w-full">
                                        </p-datepicker>
                                    </div>

                                    <!-- Uyarı Mesajı -->
                                    <p-message 
                                        *ngIf="!selectedIstasyon || !selectedOperator" 
                                        severity="info" 
                                        text="Vardiya başlatmak için istasyon ve operatör seçimi yapmalısınız.">
                                    </p-message>

                                    <!-- Butonlar -->
                                    <div class="flex gap-3 pt-4">
                                        <p-button 
                                            label="Vardiya Başlat" 
                                            icon="pi pi-check" 
                                            severity="success"
                                            [loading]="loading"
                                            [disabled]="!selectedIstasyon || !selectedOperator"
                                            (onClick)="vardiyaBaslat()">
                                        </p-button>
                                        <p-button 
                                            label="İptal" 
                                            icon="pi pi-times" 
                                            severity="secondary"
                                            [routerLink]="['/vardiya']">
                                        </p-button>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Sağ Panel - Bilgi -->
                        <div class="col-span-12 lg:col-span-5">
                            <div class="bg-gradient-to-br from-primary to-primary-600 rounded-lg p-6 text-white h-full">
                                <h4 class="font-bold text-xl mb-4">
                                    <i class="pi pi-info-circle mr-2"></i>Vardiya Hakkında
                                </h4>
                                
                                <div class="flex flex-col gap-4">
                                    <div class="flex items-start gap-3">
                                        <div class="w-8 h-8 bg-white/20 rounded-full flex items-center justify-center flex-shrink-0">
                                            <i class="pi pi-clock"></i>
                                        </div>
                                        <div>
                                            <h5 class="font-semibold m-0">Süre Takibi</h5>
                                            <p class="text-white/80 text-sm m-0 mt-1">
                                                Vardiya başladığında süre otomatik olarak takip edilir.
                                            </p>
                                        </div>
                                    </div>

                                    <div class="flex items-start gap-3">
                                        <div class="w-8 h-8 bg-white/20 rounded-full flex items-center justify-center flex-shrink-0">
                                            <i class="pi pi-money-bill"></i>
                                        </div>
                                        <div>
                                            <h5 class="font-semibold m-0">Tahsilat Girişi</h5>
                                            <p class="text-white/80 text-sm m-0 mt-1">
                                                Vardiya açıkken tahsilatları ödeme yöntemlerine göre girebilirsiniz.
                                            </p>
                                        </div>
                                    </div>

                                    <div class="flex items-start gap-3">
                                        <div class="w-8 h-8 bg-white/20 rounded-full flex items-center justify-center flex-shrink-0">
                                            <i class="pi pi-chart-bar"></i>
                                        </div>
                                        <div>
                                            <h5 class="font-semibold m-0">Karşılaştırma</h5>
                                            <p class="text-white/80 text-sm m-0 mt-1">
                                                Sistem verileri ile girilen tahsilatlar otomatik karşılaştırılır.
                                            </p>
                                        </div>
                                    </div>

                                    <div class="flex items-start gap-3">
                                        <div class="w-8 h-8 bg-white/20 rounded-full flex items-center justify-center flex-shrink-0">
                                            <i class="pi pi-check-circle"></i>
                                        </div>
                                        <div>
                                            <h5 class="font-semibold m-0">Vardiya Kapanış</h5>
                                            <p class="text-white/80 text-sm m-0 mt-1">
                                                Vardiya sonunda farklar raporlanır ve onaya sunulur.
                                            </p>
                                        </div>
                                    </div>
                                </div>

                                <!-- Seçili Bilgiler -->
                                <div *ngIf="selectedIstasyon" class="mt-6 pt-4 border-t border-white/20">
                                    <h5 class="font-semibold mb-3">Seçilen Bilgiler</h5>
                                    <div class="bg-white/10 rounded-lg p-4">
                                        <div class="flex items-center gap-2 mb-2">
                                            <i class="pi pi-building"></i>
                                            <span>{{ selectedIstasyon.ad }}</span>
                                        </div>
                                        <div class="flex items-center gap-2 mb-2" *ngIf="selectedOperator">
                                            <i class="pi pi-user"></i>
                                            <span>{{ selectedOperator.ad }} {{ selectedOperator.soyad }}</span>
                                        </div>
                                        <div class="flex items-center gap-2">
                                            <i class="pi pi-calendar"></i>
                                            <span>{{ baslangicTarihi | date:'dd.MM.yyyy HH:mm' }}</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
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
export class VardiyaBaslat implements OnInit {
    istasyonlar: Istasyon[] = [];
    operatorler: Operator[] = [];

    selectedIstasyon: Istasyon | null = null;
    selectedOperator: Operator | null = null;
    baslangicTarihi: Date = new Date();

    loading = false;

    constructor(
        private vardiyaService: VardiyaService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.loadIstasyonlar();
    }

    loadIstasyonlar(): void {
        this.vardiyaService.getIstasyonlar().subscribe({
            next: (istasyonlar) => {
                this.istasyonlar = istasyonlar;
            },
            error: (err) => console.error('İstasyonlar yüklenirken hata:', err)
        });
    }

    onIstasyonChange(): void {
        this.selectedOperator = null;
        if (this.selectedIstasyon) {
            this.vardiyaService.getOperatorler(this.selectedIstasyon.id).subscribe({
                next: (operatorler) => {
                    this.operatorler = operatorler;
                },
                error: (err) => console.error('Operatörler yüklenirken hata:', err)
            });
        } else {
            this.operatorler = [];
        }
    }

    vardiyaBaslat(): void {
        if (!this.selectedIstasyon || !this.selectedOperator) return;

        this.loading = true;

        this.vardiyaService.vardiyaBaslat(
            this.selectedOperator.id,
            this.selectedIstasyon.id
        ).subscribe({
            next: () => {
                this.loading = false;
                this.router.navigate(['/vardiya']);
            },
            error: (err) => {
                this.loading = false;
                console.error('Vardiya başlatılırken hata:', err);
            }
        });
    }
}
