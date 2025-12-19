import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { FileUploadModule } from 'primeng/fileupload';
import { DialogModule } from 'primeng/dialog';
import { ProgressBarModule } from 'primeng/progressbar';

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../services/vardiya.service';
import { TxtParserService, ParseSonuc } from '../services/txt-parser.service';
import { VardiyaDurum, OtomasyonSatis, Vardiya } from '../models/vardiya.model';
import { DbService } from '../services/db.service';

interface YuklenenVardiya {
    id: number;
    dosyaAdi: string;
    yuklemeTarihi: Date;
    baslangicTarih: Date | null;
    bitisTarih: Date | null;
    personelSayisi: number;
    islemSayisi: number;
    toplamTutar: number;
    durum: VardiyaDurum;
    satislar: OtomasyonSatis[];
}

@Component({
    selector: 'app-vardiya-listesi',
    standalone: true,
    imports: [
        CommonModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        ToastModule,
        TooltipModule,
        FileUploadModule,
        DialogModule,
        ProgressBarModule
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
                                <i class="pi pi-list mr-2 text-blue-500"></i>Vardiya Mutabakatı
                            </h2>
                            <p class="text-surface-500 mt-2">
                                Otomasyon TXT dosyasını yükleyin ve mutabakat işlemini gerçekleştirin
                            </p>
                        </div>
                        <div class="flex gap-2">
                            <p-button 
                                label="TXT Dosyası Yükle" 
                                icon="pi pi-upload" 
                                severity="success"
                                (onClick)="dosyaDialogAc()">
                            </p-button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Özet Kartları -->
            <div class="col-span-12 md:col-span-6 lg:col-span-3">
                <div class="p-4 rounded-xl h-full cursor-pointer transition-all duration-300 hover:scale-105"
                     style="background: linear-gradient(135deg, #6366f1 0%, #3b82f6 50%, #2563eb 100%); box-shadow: 0 10px 40px rgba(59, 130, 246, 0.3);"
                     (click)="dosyaDialogAc()">
                    <div class="flex justify-between items-start mb-4">
                        <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                             style="background: rgba(255,255,255,0.2);">
                            <i class="pi pi-folder-open text-2xl" style="color: white;"></i>
                        </div>
                        <div class="text-right">
                            <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Yüklenen</span>
                            <h3 class="text-4xl font-bold m-0" style="color: white !important;">{{ vardiyalar.length }}</h3>
                        </div>
                    </div>
                    <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                        <div class="flex items-center gap-2">
                            <i class="pi pi-plus-circle text-sm" style="color: rgba(255,255,255,0.7);"></i>
                            <span class="text-sm" style="color: rgba(255,255,255,0.9);">Yeni dosya yüklemek için tıklayın</span>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-6 lg:col-span-3">
                <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                     style="background: linear-gradient(135deg, #f59e0b 0%, #f97316 50%, #ea580c 100%); box-shadow: 0 10px 40px rgba(249, 115, 22, 0.3);">
                    <div class="flex justify-between items-start mb-4">
                        <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                             style="background: rgba(255,255,255,0.2);">
                            <i class="pi pi-hourglass text-2xl" style="color: white;"></i>
                        </div>
                        <div class="text-right">
                            <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Bekleyen</span>
                            <h3 class="text-4xl font-bold m-0" style="color: white !important;">{{ getMutabakatBekleyenSayisi() }}</h3>
                        </div>
                    </div>
                    <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                        <div class="flex items-center gap-2">
                            <i class="pi pi-exclamation-triangle text-sm" style="color: #fef08a;"></i>
                            <span class="text-sm" style="color: rgba(255,255,255,0.9);">
                                {{ getMutabakatBekleyenSayisi() > 0 ? 'Mutabakat işlemi bekliyor!' : 'Tüm işlemler tamam' }}
                            </span>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-6 lg:col-span-3">
                <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                     style="background: linear-gradient(135deg, #10b981 0%, #22c55e 50%, #16a34a 100%); box-shadow: 0 10px 40px rgba(34, 197, 94, 0.3);">
                    <div class="flex justify-between items-start mb-4">
                        <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                             style="background: rgba(255,255,255,0.2);">
                            <i class="pi pi-check-circle text-2xl" style="color: white;"></i>
                        </div>
                        <div class="text-right">
                            <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Tamamlanan</span>
                            <h3 class="text-4xl font-bold m-0" style="color: white !important;">{{ getTamamlananSayisi() }}</h3>
                        </div>
                    </div>
                    <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm" style="color: rgba(255,255,255,0.9);">Başarı Oranı</span>
                            <span class="font-bold text-lg" style="color: white !important;">{{ getBasariOrani() }}%</span>
                        </div>
                        <div class="w-full rounded-full h-2" style="background: rgba(255,255,255,0.2);">
                            <div class="rounded-full h-2 transition-all" style="background: white;" [style.width.%]="getBasariOrani()"></div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-span-12 md:col-span-6 lg:col-span-3">
                <div class="p-4 rounded-xl h-full transition-all duration-300 hover:scale-105"
                     style="background: linear-gradient(135deg, #8b5cf6 0%, #a855f7 50%, #9333ea 100%); box-shadow: 0 10px 40px rgba(168, 85, 247, 0.3);">
                    <div class="flex justify-between items-start mb-4">
                        <div class="w-14 h-14 rounded-2xl flex items-center justify-center"
                             style="background: rgba(255,255,255,0.2);">
                            <i class="pi pi-wallet text-2xl" style="color: white;"></i>
                        </div>
                        <div class="text-right">
                            <span class="text-xs uppercase tracking-wider" style="color: rgba(255,255,255,0.7);">Toplam Ciro</span>
                            <h3 class="text-3xl font-bold m-0" style="color: white !important;">
                                {{ getToplamCiro() | currency:'TRY':'symbol-narrow':'1.0-0':'tr' }}
                            </h3>
                        </div>
                    </div>
                    <div class="pt-3 mt-3" style="border-top: 1px solid rgba(255,255,255,0.2);">
                        <div class="flex items-center gap-4 text-sm">
                            <div class="flex items-center gap-1">
                                <i class="pi pi-receipt" style="color: rgba(255,255,255,0.7);"></i>
                                <span style="color: rgba(255,255,255,0.9);">{{ getToplamIslem() }} işlem</span>
                            </div>
                            <div class="flex items-center gap-1">
                                <i class="pi pi-users" style="color: rgba(255,255,255,0.7);"></i>
                                <span style="color: rgba(255,255,255,0.9);">{{ getToplamPersonel() }} personel</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>



            <!-- Vardiya Listesi -->
            <div class="col-span-12">
                <div class="card">
                    <div class="flex justify-between items-center mb-4">
                        <h5 class="font-bold m-0 flex items-center gap-2">
                            <i class="pi pi-list text-blue-500"></i>
                            Yüklenen Vardiyalar
                        </h5>
                        <p-button 
                            label="Yeni Dosya Yükle" 
                            icon="pi pi-upload" 
                            severity="primary"
                            (onClick)="dosyaDialogAc()">
                        </p-button>
                    </div>
                    
                    <p-table 
                        [value]="vardiyalar" 
                        [paginator]="vardiyalar.length > 10" 
                        [rows]="10"
                        styleClass="p-datatable-striped">
                        
                        <ng-template pTemplate="header">
                            <tr class="text-sm">
                                <th class="font-bold">Dosya Bilgisi</th>
                                <th class="font-bold">Vardiya Saati</th>
                                <th class="font-bold text-center">Personel</th>
                                <th class="font-bold text-center">İşlem</th>
                                <th class="font-bold text-right">Toplam Tutar</th>
                                <th class="font-bold text-center">Durum</th>
                                <th></th>
                            </tr>
                        </ng-template>
                        
                        <ng-template pTemplate="body" let-vardiya>
                            <tr class="hover:bg-surface-50 dark:hover:bg-surface-800 transition-colors">
                                <td>
                                    <div class="flex items-center gap-3">
                                        <div class="w-12 h-12 rounded-xl flex items-center justify-center"
                                             style="background: linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%);">
                                            <i class="pi pi-file text-white text-lg"></i>
                                        </div>
                                        <div>
                                            <p class="font-bold m-0 text-surface-800 dark:text-surface-100">{{ vardiya.dosyaAdi }}</p>
                                            <p class="text-surface-500 text-xs m-0 flex items-center gap-1">
                                                <i class="pi pi-clock text-xs"></i>
                                                {{ vardiya.yuklemeTarihi | date:'dd.MM.yyyy HH:mm' }}
                                            </p>
                                        </div>
                                    </div>
                                </td>
                                <td>
                                    <div *ngIf="vardiya.baslangicTarih" class="flex flex-col">
                                        <span class="font-bold text-surface-800 dark:text-surface-100">{{ vardiya.baslangicTarih | date:'dd MMMM yyyy':'':'tr' }}</span>
                                        <span class="text-sm flex items-center gap-1" style="color: #6366f1;">
                                            <i class="pi pi-clock text-xs"></i>
                                            {{ vardiya.baslangicTarih | date:'HH:mm' }} - {{ vardiya.bitisTarih | date:'HH:mm' }}
                                        </span>
                                    </div>
                                </td>
                                <td class="text-center">
                                    <div class="inline-flex items-center gap-2 px-3 py-2 rounded-lg" 
                                         style="background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%);">
                                        <i class="pi pi-users" style="color: #22c55e;"></i>
                                        <span class="font-bold" style="color: #16a34a;">{{ vardiya.personelSayisi }}</span>
                                    </div>
                                </td>
                                <td class="text-center">
                                    <div class="inline-flex items-center gap-2 px-3 py-2 rounded-lg"
                                         style="background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);">
                                        <i class="pi pi-receipt" style="color: #f59e0b;"></i>
                                        <span class="font-bold" style="color: #d97706;">{{ vardiya.islemSayisi }}</span>
                                    </div>
                                </td>
                                <td class="text-right">
                                    <div class="inline-block px-4 py-2 rounded-xl"
                                         style="background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%); box-shadow: 0 4px 15px rgba(139, 92, 246, 0.3);">
                                        <span class="font-bold text-lg text-white">
                                            {{ vardiya.toplamTutar | currency:'TRY':'symbol-narrow':'1.0-0':'tr' }}
                                        </span>
                                    </div>
                                </td>
                                <td class="text-center">
                                    <p-tag 
                                        [value]="getDurumLabel(vardiya.durum)" 
                                        [severity]="getDurumSeverity(vardiya.durum)"
                                        styleClass="text-sm px-3 py-1">
                                    </p-tag>
                                </td>
                                <td>
                                    <div class="flex gap-2 justify-end">
                                        <p-button 
                                            icon="pi pi-check-square" 
                                            severity="success" 
                                            size="small"
                                            label="Mutabakat"
                                            [disabled]="vardiya.durum === 'ONAYLANDI'"
                                            (onClick)="mutabakatYap(vardiya)">
                                        </p-button>
                                        <p-button 
                                            icon="pi pi-trash" 
                                            severity="danger" 
                                            size="small"
                                            [outlined]="true"
                                            pTooltip="Sil"
                                            (onClick)="vardiyaSil(vardiya)">
                                        </p-button>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        
                        
                        <ng-template pTemplate="emptymessage">
                            <tr>
                                <td colspan="7" class="text-center py-12">
                                    <div class="flex flex-col items-center">
                                        <div class="w-24 h-24 bg-surface-100 dark:bg-surface-800 rounded-full flex items-center justify-center mb-4">
                                            <i class="pi pi-upload text-4xl text-surface-400"></i>
                                        </div>
                                        <h4 class="text-surface-600 dark:text-surface-300 m-0">Henüz vardiya verisi yok</h4>
                                        <p class="text-surface-400 text-sm m-0 mt-2">
                                            Otomasyon TXT dosyasını yükleyerek başlayın
                                        </p>
                                        <p-button 
                                            label="Dosya Yükle" 
                                            icon="pi pi-upload" 
                                            severity="success"
                                            styleClass="mt-4"
                                            (onClick)="dosyaDialogAc()">
                                        </p-button>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                    </p-table>
                </div>
            </div>
        </div>

        <!-- Dosya Yükleme Dialog -->
        <p-dialog 
            header="Otomasyon Verisi Yükle" 
            [(visible)]="dosyaDialogVisible" 
            [modal]="true"
            [style]="{width: '600px'}">
            
            <div class="flex flex-col gap-4">
                <!-- Dosya Seçme -->
                <div class="border-2 border-dashed border-surface-300 dark:border-surface-600 rounded-xl p-8 text-center"
                     [ngClass]="{'border-green-500 bg-green-50': secilenDosya}">
                    
                    <input 
                        type="file" 
                        accept=".txt,.d1a,.d1b,.d1c,.d1d,.d1e,.d1f,.D1A,.D1B,.D1C,.D1D,.D1E,.D1F"
                        (change)="dosyaSec($event)"
                        #fileInput
                        class="hidden">
                    
                    <div *ngIf="!secilenDosya">
                        <i class="pi pi-cloud-upload text-5xl text-surface-400 mb-4"></i>
                        <p class="text-surface-600 dark:text-surface-300 font-semibold m-0">
                            Otomasyon dosyasını sürükleyin veya seçin
                        </p>
                        <p class="text-surface-400 text-sm m-0 mt-1">
                            Desteklenen uzantılar: .D1A, .D1B, .D1C, .TXT
                        </p>
                        <p-button 
                            label="Dosya Seç" 
                            icon="pi pi-folder-open" 
                            severity="secondary"
                            styleClass="mt-4"
                            (onClick)="fileInput.click()">
                        </p-button>
                    </div>
                    
                    <div *ngIf="secilenDosya" class="text-green-700 dark:text-green-300">
                        <i class="pi pi-file text-5xl mb-4"></i>
                        <p class="font-semibold m-0">{{ secilenDosya.name }}</p>
                        <p class="text-sm m-0 mt-1">{{ (secilenDosya.size / 1024).toFixed(1) }} KB</p>
                        <p-button 
                            label="Değiştir" 
                            icon="pi pi-refresh" 
                            severity="secondary"
                            size="small"
                            styleClass="mt-3"
                            (onClick)="fileInput.click()">
                        </p-button>
                    </div>
                </div>

                <!-- Parse Sonucu -->
                <div *ngIf="parseSonuc" class="p-4 rounded-lg" 
                     [ngClass]="parseSonuc.basarili ? 'bg-green-50 dark:bg-green-900/20' : 'bg-red-50 dark:bg-red-900/20'">
                    
                    <div *ngIf="parseSonuc.basarili" class="text-green-700 dark:text-green-300">
                        <div class="flex items-center gap-2 mb-3">
                            <i class="pi pi-check-circle text-xl"></i>
                            <span class="font-semibold">Dosya başarıyla okundu!</span>
                        </div>
                        <div class="grid grid-cols-2 gap-3 text-sm">
                            <div class="flex justify-between">
                                <span>Kayıt Sayısı:</span>
                                <span class="font-bold">{{ parseSonuc.kayitSayisi }}</span>
                            </div>
                            <div class="flex justify-between">
                                <span>Personel:</span>
                                <span class="font-bold">{{ parseSonuc.personeller.length }}</span>
                            </div>
                            <div class="flex justify-between">
                                <span>Toplam Tutar:</span>
                                <span class="font-bold">{{ parseSonuc.toplamTutar | currency:'TRY':'symbol-narrow':'1.2-2':'tr' }}</span>
                            </div>
                            <div class="flex justify-between">
                                <span>Tarih:</span>
                                <span class="font-bold">{{ parseSonuc.baslangicTarih | date:'dd.MM.yyyy' }}</span>
                            </div>
                        </div>
                        <div class="mt-3 pt-3 border-t border-green-300 dark:border-green-700">
                            <p class="text-xs m-0">
                                <strong>Personeller:</strong> {{ parseSonuc.personeller.join(', ') }}
                            </p>
                        </div>
                    </div>
                    
                    <div *ngIf="!parseSonuc.basarili" class="text-red-700 dark:text-red-300">
                        <div class="flex items-center gap-2">
                            <i class="pi pi-times-circle text-xl"></i>
                            <span class="font-semibold">Dosya okunamadı!</span>
                        </div>
                        <p class="text-sm m-0 mt-2">Lütfen geçerli bir otomasyon TXT dosyası yükleyin.</p>
                    </div>
                </div>

                <!-- İşlem Progress -->
                <div *ngIf="yukleniyor" class="p-4 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
                    <div class="flex items-center gap-3 mb-3">
                        <i class="pi pi-spin pi-spinner text-2xl text-blue-500"></i>
                        <div>
                            <p class="font-semibold text-blue-700 dark:text-blue-300 m-0">Dosya İşleniyor...</p>
                            <p class="text-blue-600 dark:text-blue-400 text-sm m-0">{{ islemDurumu }}</p>
                        </div>
                    </div>
                    <p-progressBar [value]="islemYuzdesi" [showValue]="true" [style]="{'height': '8px'}"></p-progressBar>
                    <div class="flex justify-between text-xs text-blue-500 mt-2">
                        <span>{{ bulunanKayit }} kayıt bulundu</span>
                        <span>{{ gecenSure }}s</span>
                    </div>
                </div>
            </div>

            <ng-template pTemplate="footer">
                <p-button label="İptal" icon="pi pi-times" severity="secondary" (onClick)="dosyaDialogKapat()"></p-button>
                <p-button 
                    label="Yükle ve Devam Et" 
                    icon="pi pi-check" 
                    severity="success"
                    [disabled]="!parseSonuc?.basarili"
                    (onClick)="dosyaYukle()">
                </p-button>
            </ng-template>
        </p-dialog>
    `,
    styles: [`:host { display: block; }`]
})
export class VardiyaListesi implements OnInit {
    vardiyalar: YuklenenVardiya[] = [];

    dosyaDialogVisible = false;
    secilenDosya: File | null = null;
    parseSonuc: ParseSonuc | null = null;
    yukleniyor = false;

    // İlerleme göstergesi için
    islemDurumu = '';
    islemYuzdesi = 0;
    bulunanKayit = 0;
    gecenSure = 0;
    private parseInterval: any = null;

    constructor(
        private vardiyaService: VardiyaService,
        private txtParser: TxtParserService,
        private messageService: MessageService,
        private router: Router,
        private dbService: DbService
    ) { }

    ngOnInit(): void {
        this.vardiyalariYukle();
    }

    async vardiyalariYukle(): Promise<void> {
        const dbVardiyalar = await this.dbService.tumVardiyalariGetir();
        this.vardiyalar = dbVardiyalar.map(v => ({
            id: v.id!,
            dosyaAdi: v.dosyaAdi,
            yuklemeTarihi: v.yuklemeTarihi,
            baslangicTarih: v.baslangicTarih,
            bitisTarih: v.bitisTarih,
            personelSayisi: v.personelSayisi,
            islemSayisi: v.islemSayisi,
            toplamTutar: v.toplamTutar,
            durum: v.durum === 'ACIK' ? VardiyaDurum.ACIK :
                v.durum === 'ONAY_BEKLIYOR' ? VardiyaDurum.ONAY_BEKLIYOR :
                    v.durum === 'ONAYLANDI' ? VardiyaDurum.ONAYLANDI : VardiyaDurum.REDDEDILDI,
            satislar: [] // Listede satış detayına gerek yok
        }));
    }

    dosyaDialogAc(): void {
        this.secilenDosya = null;
        this.parseSonuc = null;
        this.dosyaDialogVisible = true;
    }

    dosyaDialogKapat(): void {
        this.dosyaDialogVisible = false;
        this.secilenDosya = null;
        this.parseSonuc = null;
    }

    dosyaSec(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.secilenDosya = input.files[0];
            this.parseDosya();
        }
    }

    parseDosya(): void {
        if (!this.secilenDosya) return;

        // İlerleme değişkenlerini sıfırla
        this.yukleniyor = true;
        this.islemDurumu = 'Dosya okunuyor...';
        this.islemYuzdesi = 0;
        this.bulunanKayit = 0;
        this.gecenSure = 0;
        this.parseSonuc = null;

        const baslangicZamani = Date.now();

        // Süre sayacı başlat
        this.parseInterval = setInterval(() => {
            this.gecenSure = Math.floor((Date.now() - baslangicZamani) / 1000);
        }, 100);

        const reader = new FileReader();

        reader.onload = (e) => {
            const icerik = e.target?.result as string;

            // İlk aşama: Dosya okundu
            this.islemDurumu = 'Dosya okundu, içerik temizleniyor...';
            this.islemYuzdesi = 20;

            // Async işlem için setTimeout kullan (UI güncellensin)
            setTimeout(() => {
                this.islemDurumu = 'Satırlar birleştiriliyor...';
                this.islemYuzdesi = 30;

                setTimeout(() => {
                    this.islemDurumu = 'Personel satışları aranıyor...';
                    this.islemYuzdesi = 50;

                    setTimeout(() => {
                        // Parser'ı çağır
                        const sonuc = this.txtParser.parseOtomasyonDosyasi(icerik);

                        this.islemDurumu = 'Filo satışları aranıyor...';
                        this.islemYuzdesi = 70;
                        this.bulunanKayit = sonuc.satislar.length;

                        setTimeout(() => {
                            this.islemDurumu = 'Toplamlar hesaplanıyor...';
                            this.islemYuzdesi = 90;
                            this.bulunanKayit = sonuc.kayitSayisi;

                            setTimeout(() => {
                                this.islemDurumu = 'Tamamlandı!';
                                this.islemYuzdesi = 100;
                                this.parseSonuc = sonuc;

                                // Interval'ı temizle
                                if (this.parseInterval) {
                                    clearInterval(this.parseInterval);
                                    this.parseInterval = null;
                                }

                                // Kısa bir gecikme sonra loading'i kapat
                                setTimeout(() => {
                                    this.yukleniyor = false;
                                }, 300);
                            }, 100);
                        }, 100);
                    }, 100);
                }, 100);
            }, 100);
        };

        reader.onerror = () => {
            if (this.parseInterval) {
                clearInterval(this.parseInterval);
                this.parseInterval = null;
            }

            this.parseSonuc = {
                basarili: false,
                kayitSayisi: 0,
                personeller: [],
                toplamTutar: 0,
                baslangicTarih: null,
                bitisTarih: null,
                satislar: [],
                filoSatislari: [],
                hatalar: ['Dosya okunamadı']
            };
            this.yukleniyor = false;
            this.islemDurumu = 'Hata oluştu!';
        };

        // Windows-1254 (Türkçe) encoding kullan
        reader.readAsText(this.secilenDosya, 'windows-1254');
    }

    async dosyaYukle(): Promise<void> {
        if (!this.parseSonuc?.basarili || !this.secilenDosya) return;

        // Mükerrer dosya kontrolü
        const mevcutDosyalar = await this.dbService.tumVardiyalariGetir();
        const dosyaVar = mevcutDosyalar.some(v => v.dosyaAdi === this.secilenDosya!.name);

        if (dosyaVar) {
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Bu dosya daha önce yüklenmiş!'
            });
            return;
        }

        const yeniVardiya: Vardiya = {
            id: 0, // DB tarafından atanacak
            istasyonId: 1,
            istasyonAdi: 'Merkez İstasyon',
            sorumluId: 6,
            sorumluAdi: 'Vardiya Sorumlusu',
            dosyaAdi: this.secilenDosya.name,
            olusturmaTarihi: new Date(),
            baslangicTarihi: this.parseSonuc.baslangicTarih || new Date(),
            bitisTarihi: this.parseSonuc.bitisTarih || undefined,
            durum: VardiyaDurum.ACIK,
            pompaToplam: this.parseSonuc.toplamTutar,
            marketToplam: 0,
            genelToplam: this.parseSonuc.toplamTutar,
            toplamFark: 0,
            kilitli: false
        };

        try {
            await this.vardiyaService.vardiyaEkle(yeniVardiya, this.parseSonuc.satislar, this.parseSonuc.filoSatislari);

            this.messageService.add({
                severity: 'success',
                summary: 'Başarılı',
                detail: `${this.parseSonuc.kayitSayisi} kayıt veritabanına yüklendi`
            });

            await this.vardiyalariYukle();
            this.dosyaDialogKapat();

        } catch (error) {
            console.error('Kayıt hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Veritabanına kayıt sırasında hata oluştu!'
            });
        }
    }

    async vardiyaSil(vardiya: YuklenenVardiya): Promise<void> {
        try {
            await this.dbService.vardiyaSil(vardiya.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Silindi',
                detail: 'Vardiya verisi silindi'
            });
            await this.vardiyalariYukle();
        } catch (error) {
            console.error('Silme hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Silme işlemi başarısız oldu'
            });
        }
    }

    async mutabakatYap(vardiya: YuklenenVardiya): Promise<void> {
        console.log('Mutabakat başlatılıyor...', vardiya);

        try {
            // Aktif vardiyayı ve satışları veritabanından yükle
            await this.vardiyaService.setAktifVardiyaById(vardiya.id);

            console.log('Pompa sayfasına yönlendiriliyor...');
            this.router.navigate(['/vardiya/pompa']);
        } catch (error) {
            console.error('Mutabakat başlatma hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Vardiya detayları yüklenemedi!'
            });
        }
    }

    getMutabakatBekleyenSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ACIK).length;
    }

    getTamamlananSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ONAYLANDI).length;
    }

    getToplamCiro(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.toplamTutar, 0);
    }

    getDurumLabel(durum: VardiyaDurum): string {
        const labels: Record<VardiyaDurum, string> = {
            [VardiyaDurum.ACIK]: 'Mutabakat Bekliyor',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'Onay Bekliyor',
            [VardiyaDurum.ONAYLANDI]: 'Tamamlandı',
            [VardiyaDurum.REDDEDILDI]: 'Reddedildi'
        };
        return labels[durum];
    }

    getDurumSeverity(durum: VardiyaDurum): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        const severities: Record<VardiyaDurum, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
            [VardiyaDurum.ACIK]: 'warn',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'info',
            [VardiyaDurum.ONAYLANDI]: 'success',
            [VardiyaDurum.REDDEDILDI]: 'danger'
        };
        return severities[durum];
    }

    getBasariOrani(): number {
        if (this.vardiyalar.length === 0) return 0;
        return Math.round((this.getTamamlananSayisi() / this.vardiyalar.length) * 100);
    }

    getToplamIslem(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.islemSayisi, 0);
    }

    getToplamPersonel(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.personelSayisi, 0);
    }
}
