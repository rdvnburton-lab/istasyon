import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../services/vardiya.service';
import {
    Vardiya,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    GiderTuru,
    MarketOzet
} from '../models/vardiya.model';

@Component({
    selector: 'app-market-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        DividerModule,
        InputNumberModule,
        TextareaModule,
        DialogModule,
        ToastModule,
        SelectModule,
        MessageModule,
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
                                <i class="pi pi-shopping-bag mr-2 text-purple-500"></i>Market Yönetimi
                            </h2>
                            <p class="text-surface-500 mt-2" *ngIf="aktifVardiya">
                                {{ aktifVardiya.istasyonAdi }} - {{ aktifVardiya.sorumluAdi }}
                            </p>
                        </div>
                        <div class="flex gap-2">
                            <p-button 
                                label="Gider Ekle" 
                                icon="pi pi-minus-circle" 
                                severity="warn"
                                (onClick)="giderDialogAc()">
                            </p-button>
                            <p-button 
                                label="Geri" 
                                icon="pi pi-arrow-left" 
                                severity="secondary"
                                [routerLink]="['/vardiya']">
                            </p-button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Özet Kartları -->
            <div class="col-span-12 md:col-span-3">
                <div class="card h-full bg-gradient-to-br from-purple-500 to-purple-600 text-white">
                    <div class="flex justify-between items-start">
                        <div>
                            <span class="text-white/80 text-sm">Z Raporu</span>
                            <h3 class="text-2xl font-bold m-0 mt-2">
                                {{ marketOzet?.zRaporuToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                            </h3>
                        </div>
                        <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                            <i class="pi pi-file text-xl"></i>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-3">
                <div class="card h-full bg-gradient-to-br from-green-500 to-green-600 text-white">
                    <div class="flex justify-between items-start">
                        <div>
                            <span class="text-white/80 text-sm">Tahsilat</span>
                            <h3 class="text-2xl font-bold m-0 mt-2">
                                {{ marketOzet?.tahsilatToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                            </h3>
                        </div>
                        <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                            <i class="pi pi-wallet text-xl"></i>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-3">
                <div class="card h-full bg-gradient-to-br from-orange-500 to-orange-600 text-white">
                    <div class="flex justify-between items-start">
                        <div>
                            <span class="text-white/80 text-sm">Gider</span>
                            <h3 class="text-2xl font-bold m-0 mt-2">
                                {{ marketOzet?.giderToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                            </h3>
                        </div>
                        <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                            <i class="pi pi-shopping-cart text-xl"></i>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-3">
                <div class="card h-full text-white" [ngClass]="getFarkCardClass()">
                    <div class="flex justify-between items-start">
                        <div>
                            <span class="text-white/80 text-sm">Fark</span>
                            <h3 class="text-2xl font-bold m-0 mt-2">
                                {{ marketOzet?.fark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                            </h3>
                        </div>
                        <div class="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center">
                            <i class="pi pi-chart-line text-xl"></i>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Z Raporu Girişi -->
            <div class="col-span-12 lg:col-span-6">
                <div class="card h-full">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-file mr-2 text-purple-500"></i>Z Raporu Mali Kayıt
                    </h5>

                    <div class="flex flex-col gap-4">
                        <!-- Genel Toplam -->
                        <div class="flex flex-col gap-2">
                            <label class="font-semibold">Genel Toplam (Z Raporu)</label>
                            <p-inputnumber 
                                [(ngModel)]="zRaporuForm.genelToplam" 
                                mode="currency" 
                                currency="TRY" 
                                locale="tr-TR"
                                styleClass="w-full"
                                (onInput)="kdvKontrol()">
                            </p-inputnumber>
                        </div>

                        <!-- KDV Kırılımları -->
                        <div class="grid grid-cols-3 gap-4">
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm">
                                    KDV %1
                                    <span class="text-surface-400 text-xs ml-1">(Gıda)</span>
                                </label>
                                <p-inputnumber 
                                    [(ngModel)]="zRaporuForm.kdv1" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full"
                                    (onInput)="kdvKontrol()">
                                </p-inputnumber>
                            </div>
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm">
                                    KDV %10
                                    <span class="text-surface-400 text-xs ml-1">(İçecek)</span>
                                </label>
                                <p-inputnumber 
                                    [(ngModel)]="zRaporuForm.kdv10" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full"
                                    (onInput)="kdvKontrol()">
                                </p-inputnumber>
                            </div>
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm">
                                    KDV %20
                                    <span class="text-surface-400 text-xs ml-1">(Diğer)</span>
                                </label>
                                <p-inputnumber 
                                    [(ngModel)]="zRaporuForm.kdv20" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full"
                                    (onInput)="kdvKontrol()">
                                </p-inputnumber>
                            </div>
                        </div>

                        <!-- KDV Kontrol -->
                        <div class="p-3 rounded-lg" [ngClass]="kdvKontrolSonuc.sinif">
                            <div class="flex items-center gap-2">
                                <i class="pi" [ngClass]="kdvKontrolSonuc.icon"></i>
                                <span>{{ kdvKontrolSonuc.mesaj }}</span>
                            </div>
                            <div class="flex justify-between mt-2 text-sm">
                                <span>KDV Toplam: {{ getKdvToplam() | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                <span>KDV Hariç: {{ getKdvHaric() | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            </div>
                        </div>

                        <p-button 
                            label="Z Raporu Kaydet" 
                            icon="pi pi-check" 
                            severity="success"
                            styleClass="w-full"
                            [disabled]="!kdvKontrolSonuc.gecerli"
                            (onClick)="zRaporuKaydet()">
                        </p-button>
                    </div>

                    <!-- Kaydedilmiş Z Raporu -->
                    <div *ngIf="zRaporu" class="mt-4 p-4 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
                        <div class="flex justify-between items-center mb-2">
                            <span class="font-semibold text-purple-600">Kayıtlı Z Raporu</span>
                            <p-tag value="Kaydedildi" severity="success" size="small"></p-tag>
                        </div>
                        <div class="grid grid-cols-2 gap-2 text-sm">
                            <span>Genel Toplam:</span>
                            <span class="font-bold text-right">{{ zRaporu.genelToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            <span>KDV Toplam:</span>
                            <span class="font-bold text-right">{{ zRaporu.kdvToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Tahsilat Girişi -->
            <div class="col-span-12 lg:col-span-6">
                <div class="card h-full">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-wallet mr-2 text-green-500"></i>Market Tahsilatı
                    </h5>

                    <div class="flex flex-col gap-4">
                        <div class="grid grid-cols-3 gap-4">
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm text-green-600">Nakit</label>
                                <p-inputnumber 
                                    [(ngModel)]="tahsilatForm.nakit" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full">
                                </p-inputnumber>
                            </div>
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm text-blue-600">Kredi Kartı</label>
                                <p-inputnumber 
                                    [(ngModel)]="tahsilatForm.krediKarti" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full">
                                </p-inputnumber>
                            </div>
                            <div class="flex flex-col gap-2">
                                <label class="font-semibold text-sm text-orange-600">Yemek Kartı</label>
                                <p-inputnumber 
                                    [(ngModel)]="tahsilatForm.yemekKarti" 
                                    mode="currency" 
                                    currency="TRY" 
                                    locale="tr-TR"
                                    styleClass="w-full">
                                </p-inputnumber>
                            </div>
                        </div>

                        <div class="flex justify-between items-center p-3 bg-surface-50 dark:bg-surface-800 rounded-lg">
                            <span class="font-semibold">Tahsilat Toplamı:</span>
                            <span class="font-bold text-xl text-green-600">
                                {{ getTahsilatToplam() | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                            </span>
                        </div>

                        <p-button 
                            label="Tahsilat Kaydet" 
                            icon="pi pi-check" 
                            severity="success"
                            styleClass="w-full"
                            (onClick)="tahsilatKaydet()">
                        </p-button>
                    </div>

                    <!-- Kaydedilmiş Tahsilat -->
                    <div *ngIf="tahsilat" class="mt-4 p-4 bg-green-50 dark:bg-green-900/20 rounded-lg">
                        <div class="flex justify-between items-center mb-2">
                            <span class="font-semibold text-green-600">Kayıtlı Tahsilat</span>
                            <p-tag value="Kaydedildi" severity="success" size="small"></p-tag>
                        </div>
                        <div class="grid grid-cols-2 gap-2 text-sm">
                            <span>Nakit:</span>
                            <span class="font-bold text-right">{{ tahsilat.nakit | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            <span>Kredi Kartı:</span>
                            <span class="font-bold text-right">{{ tahsilat.krediKarti | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            <span>Yemek Kartı:</span>
                            <span class="font-bold text-right">{{ tahsilat.yemekKarti | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            <span class="font-bold">Toplam:</span>
                            <span class="font-bold text-right text-green-600">{{ tahsilat.toplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Giderler -->
            <div class="col-span-12">
                <div class="card">
                    <div class="flex justify-between items-center mb-4">
                        <h5 class="font-semibold m-0">
                            <i class="pi pi-shopping-cart mr-2 text-orange-500"></i>Market Giderleri
                        </h5>
                        <span class="font-bold text-orange-600" *ngIf="giderler.length > 0">
                            Toplam: {{ getGiderToplam() | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                        </span>
                    </div>

                    <p-table [value]="giderler" styleClass="p-datatable-sm" *ngIf="giderler.length > 0">
                        <ng-template pTemplate="header">
                            <tr>
                                <th>Gider Türü</th>
                                <th>Açıklama</th>
                                <th class="text-right">Tutar</th>
                                <th></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-gider>
                            <tr>
                                <td>
                                    <p-tag [value]="getGiderTuruLabel(gider.giderTuru)" severity="warn"></p-tag>
                                </td>
                                <td>{{ gider.aciklama }}</td>
                                <td class="text-right font-bold text-orange-600">
                                    {{ gider.tutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}
                                </td>
                                <td>
                                    <p-button 
                                        icon="pi pi-trash" 
                                        severity="danger" 
                                        size="small" 
                                        [text]="true"
                                        (onClick)="giderSil(gider.id)">
                                    </p-button>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>

                    <div *ngIf="giderler.length === 0" class="text-center py-8 text-surface-500">
                        <i class="pi pi-inbox text-4xl mb-2"></i>
                        <p class="m-0">Henüz gider kaydı yok</p>
                    </div>
                </div>
            </div>

            <!-- Özet Tablosu -->
            <div class="col-span-12" *ngIf="marketOzet && (zRaporu || tahsilat)">
                <div class="card">
                    <h5 class="font-semibold mb-4">
                        <i class="pi pi-chart-pie mr-2 text-indigo-500"></i>Market Özet (Muhasebe Raporu)
                    </h5>

                    <div class="grid grid-cols-12 gap-6">
                        <!-- Sol: Gelir/Gider -->
                        <div class="col-span-12 md:col-span-6">
                            <div class="border border-surface-200 dark:border-surface-700 rounded-lg overflow-hidden">
                                <div class="bg-surface-100 dark:bg-surface-800 p-3 font-bold">Gelir / Gider Özeti</div>
                                <div class="p-4">
                                    <div class="flex justify-between py-2 border-b">
                                        <span>Z Raporu (Ciro)</span>
                                        <span class="font-bold">{{ marketOzet.zRaporuToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 border-b">
                                        <span>Tahsilat Toplamı</span>
                                        <span class="font-bold text-green-600">{{ marketOzet.tahsilatToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 border-b">
                                        <span>Gider Toplamı</span>
                                        <span class="font-bold text-orange-600">-{{ marketOzet.giderToplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 border-b">
                                        <span>Net Kasa</span>
                                        <span class="font-bold">{{ marketOzet.netKasa | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 font-bold text-lg" [ngClass]="marketOzet.fark >= 0 ? 'text-green-600' : 'text-red-600'">
                                        <span>FARK</span>
                                        <span>{{ marketOzet.fark | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Sağ: KDV Dökümü -->
                        <div class="col-span-12 md:col-span-6">
                            <div class="border border-surface-200 dark:border-surface-700 rounded-lg overflow-hidden">
                                <div class="bg-surface-100 dark:bg-surface-800 p-3 font-bold">KDV Dökümü</div>
                                <div class="p-4">
                                    <div class="flex justify-between py-2 border-b">
                                        <span>KDV %1 (Gıda)</span>
                                        <span class="font-bold">{{ marketOzet.kdvDokum.kdv1 | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 border-b">
                                        <span>KDV %10 (İçecek)</span>
                                        <span class="font-bold">{{ marketOzet.kdvDokum.kdv10 | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 border-b">
                                        <span>KDV %20 (Diğer)</span>
                                        <span class="font-bold">{{ marketOzet.kdvDokum.kdv20 | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                    <div class="flex justify-between py-2 font-bold text-lg text-purple-600">
                                        <span>TOPLAM KDV</span>
                                        <span>{{ marketOzet.kdvDokum.toplam | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Gider Ekleme Dialog -->
        <p-dialog 
            header="Market Gideri Ekle" 
            [(visible)]="giderDialogVisible" 
            [modal]="true"
            [style]="{width: '400px'}">
            
            <div class="flex flex-col gap-4">
                <div class="flex flex-col gap-2">
                    <label class="font-semibold">Gider Türü</label>
                    <p-select 
                        [(ngModel)]="giderForm.giderTuru" 
                        [options]="giderTurleri"
                        optionLabel="label"
                        optionValue="value"
                        placeholder="Gider Türü Seçin"
                        styleClass="w-full">
                    </p-select>
                </div>
                <div class="flex flex-col gap-2">
                    <label class="font-semibold">Tutar</label>
                    <p-inputnumber 
                        [(ngModel)]="giderForm.tutar" 
                        mode="currency" 
                        currency="TRY" 
                        locale="tr-TR"
                        styleClass="w-full">
                    </p-inputnumber>
                </div>
                <div class="flex flex-col gap-2">
                    <label class="font-semibold">Açıklama</label>
                    <textarea 
                        pTextarea
                        [(ngModel)]="giderForm.aciklama" 
                        rows="2"
                        placeholder="Açıklama girin..."
                        class="w-full">
                    </textarea>
                </div>
            </div>

            <ng-template pTemplate="footer">
                <p-button label="İptal" icon="pi pi-times" severity="secondary" (onClick)="giderDialogVisible = false"></p-button>
                <p-button label="Ekle" icon="pi pi-check" severity="success" (onClick)="giderEkle()"></p-button>
            </ng-template>
        </p-dialog>
    `,
    styles: [`:host { display: block; }`]
})
export class MarketYonetimi implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    zRaporu: MarketZRaporu | null = null;
    tahsilat: MarketTahsilat | null = null;
    giderler: MarketGider[] = [];
    marketOzet: MarketOzet | null = null;

    zRaporuForm = { genelToplam: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
    tahsilatForm = { nakit: 0, krediKarti: 0, yemekKarti: 0 };

    kdvKontrolSonuc = { gecerli: false, mesaj: 'Z Raporu bilgilerini girin', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };

    giderDialogVisible = false;
    giderTurleri: { label: string; value: GiderTuru }[] = [];
    giderForm = { giderTuru: null as GiderTuru | null, tutar: 0, aciklama: '' };

    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.giderTurleri = this.vardiyaService.getGiderTurleri();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (!vardiya) {
                    this.router.navigate(['/vardiya']);
                }
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketZRaporu().subscribe((zRaporu: MarketZRaporu | null) => {
                this.zRaporu = zRaporu;
                if (zRaporu) {
                    this.zRaporuForm = {
                        genelToplam: zRaporu.genelToplam,
                        kdv1: zRaporu.kdv1,
                        kdv10: zRaporu.kdv10,
                        kdv20: zRaporu.kdv20
                    };
                    this.kdvKontrol();
                }
                this.updateOzet();
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketTahsilat().subscribe((tahsilat: MarketTahsilat | null) => {
                this.tahsilat = tahsilat;
                if (tahsilat) {
                    this.tahsilatForm = {
                        nakit: tahsilat.nakit,
                        krediKarti: tahsilat.krediKarti,
                        yemekKarti: tahsilat.yemekKarti
                    };
                }
                this.updateOzet();
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketGiderler().subscribe((giderler: MarketGider[]) => {
                this.giderler = giderler;
                this.updateOzet();
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    updateOzet(): void {
        this.vardiyaService.getMarketOzet().subscribe((ozet: MarketOzet) => {
            this.marketOzet = ozet;
        });
    }

    // KDV Kontrol
    kdvKontrol(): void {
        const genelToplam = this.zRaporuForm.genelToplam || 0;
        const kdvToplam = this.getKdvToplam();

        if (genelToplam === 0) {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'Genel toplam giriniz', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };
            return;
        }

        // Basit kontrol: KDV toplamı genel toplamdan küçük olmalı ve mantıklı bir aralıkta olmalı
        const kdvOrani = (kdvToplam / genelToplam) * 100;

        if (kdvToplam === 0) {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'KDV kırılımlarını giriniz', sinif: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700', icon: 'pi-exclamation-triangle' };
        } else if (kdvOrani > 0 && kdvOrani <= 25) {
            this.kdvKontrolSonuc = { gecerli: true, mesaj: 'KDV oranları uygun', sinif: 'bg-green-100 dark:bg-green-900/30 text-green-700', icon: 'pi-check-circle' };
        } else {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'KDV oranları tutarsız görünüyor', sinif: 'bg-red-100 dark:bg-red-900/30 text-red-700', icon: 'pi-times-circle' };
        }
    }

    getKdvToplam(): number {
        return (this.zRaporuForm.kdv1 || 0) + (this.zRaporuForm.kdv10 || 0) + (this.zRaporuForm.kdv20 || 0);
    }

    getKdvHaric(): number {
        return (this.zRaporuForm.genelToplam || 0) - this.getKdvToplam();
    }

    getTahsilatToplam(): number {
        return (this.tahsilatForm.nakit || 0) + (this.tahsilatForm.krediKarti || 0) + (this.tahsilatForm.yemekKarti || 0);
    }

    getGiderToplam(): number {
        return this.giderler.reduce((sum, g) => sum + g.tutar, 0);
    }

    // Kayıt işlemleri
    zRaporuKaydet(): void {
        if (!this.aktifVardiya) return;

        this.vardiyaService.marketZRaporuKaydet({
            vardiyaId: this.aktifVardiya.id,
            genelToplam: this.zRaporuForm.genelToplam,
            kdv1: this.zRaporuForm.kdv1,
            kdv10: this.zRaporuForm.kdv10,
            kdv20: this.zRaporuForm.kdv20
        }).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Z Raporu kaydedildi' });
        });
    }

    tahsilatKaydet(): void {
        if (!this.aktifVardiya) return;

        this.vardiyaService.marketTahsilatKaydet({
            vardiyaId: this.aktifVardiya.id,
            nakit: this.tahsilatForm.nakit,
            krediKarti: this.tahsilatForm.krediKarti,
            yemekKarti: this.tahsilatForm.yemekKarti
        }).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tahsilat kaydedildi' });
        });
    }

    // Gider işlemleri
    giderDialogAc(): void {
        this.giderForm = { giderTuru: null, tutar: 0, aciklama: '' };
        this.giderDialogVisible = true;
    }

    giderEkle(): void {
        if (!this.giderForm.giderTuru || !this.giderForm.tutar || !this.aktifVardiya) return;

        this.vardiyaService.marketGiderEkle({
            vardiyaId: this.aktifVardiya.id,
            giderTuru: this.giderForm.giderTuru,
            tutar: this.giderForm.tutar,
            aciklama: this.giderForm.aciklama || this.getGiderTuruLabel(this.giderForm.giderTuru)
        }).subscribe(() => {
            this.giderDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
        });
    }

    giderSil(giderId: number): void {
        this.vardiyaService.marketGiderSil(giderId).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
        });
    }

    getGiderTuruLabel(turu: GiderTuru): string {
        const found = this.giderTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    getFarkCardClass(): string {
        if (!this.marketOzet) return 'bg-gradient-to-br from-gray-500 to-gray-600';
        if (Math.abs(this.marketOzet.fark) < 1) return 'bg-gradient-to-br from-green-500 to-green-600';
        if (this.marketOzet.fark < 0) return 'bg-gradient-to-br from-red-500 to-red-600';
        return 'bg-gradient-to-br from-blue-500 to-blue-600';
    }
}
