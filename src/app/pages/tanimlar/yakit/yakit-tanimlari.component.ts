import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { ColorPickerModule } from 'primeng/colorpicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { YakitService, Yakit } from '../../../services/yakit.service';

@Component({
    selector: 'app-yakit-tanimlari',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        DialogModule,
        ColorPickerModule,
        InputNumberModule,
        ToastModule,
        ConfirmDialogModule,
        TextareaModule
    ],
    providers: [MessageService, ConfirmationService],
    template: `
    <!-- MAIN LAYOUT -->
    <div class="grid grid-cols-12 gap-6">
        <!-- Header Section -->
        <div class="col-span-12">
            <div class="card">
                <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                    <div>
                        <h2 class="text-2xl font-bold text-[var(--text-color)] m-0">
                            <i class="pi pi-filter mr-2 text-blue-500"></i>Yakıt Türü Tanımları
                        </h2>
                        <p class="text-[var(--text-color-secondary)] mt-2">
                            İstasyon yakıt türleri ve otomasyon eşleşme ayarları
                        </p>
                    </div>
                    <div class="flex gap-2">
                        <p-button icon="pi pi-plus" label="Yeni Ekle" severity="primary" (onClick)="openNew()"></p-button>
                    </div>
                </div>
            </div>
        </div>

        <!-- Table Section -->
        <div class="col-span-12">
            <div class="card">
                <p-table [value]="yakitlar" [loading]="loading" styleClass="p-datatable-striped" [rowHover]="true"
                    [globalFilterFields]="['ad', 'otomasyonUrunAdi']" #dt>
                    
                    <ng-template pTemplate="caption">
                        <div class="flex justify-between items-center">
                            <span class="text-xl font-bold">Tanımlı Yakıtlar ({{ yakitlar.length }})</span>
                            <div class="flex gap-2">
                                <span class="p-input-icon-left">
                                    <i class="pi pi-search"></i>
                                    <input pInputText type="text" (input)="dt.filterGlobal($any($event.target).value, 'contains')" placeholder="Ara..." />
                                </span>
                            </div>
                        </div>
                    </ng-template>

                    <ng-template pTemplate="header">
                        <tr>
                            <th style="width: 80px" class="text-center">Sıra</th>
                            <th>Yakıt Adı</th>
                            <th>Otomasyon Eşleşme Anahtarları</th>
                            <th>Turpak Kodları</th>
                            <th>Renk</th>
                            <th style="width: 120px; text-align: center">İşlemler</th>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="body" let-yakit>
                        <tr class="hover:bg-surface-50">
                            <td class="font-bold text-center text-lg">{{yakit.sira}}</td>
                            <td class="font-bold">{{yakit.ad}}</td>
                            <td>
                                <div class="flex flex-wrap gap-1" *ngIf="yakit.otomasyonUrunAdi">
                                    <span class="bg-gray-100 text-gray-700 px-2 py-1 rounded text-xs border border-gray-200 font-mono" *ngFor="let key of yakit.otomasyonUrunAdi.split(',')">
                                        {{key.trim()}}
                                    </span>
                                </div>
                                <span *ngIf="!yakit.otomasyonUrunAdi" class="text-[var(--text-color-secondary)] italic text-sm">Tanımsız</span>
                            </td>
                            <td>
                                <div class="flex flex-wrap gap-1" *ngIf="yakit.turpakUrunKodu">
                                    <span class="bg-indigo-100 text-indigo-700 px-2 py-1 rounded text-xs border border-indigo-200 font-mono" *ngFor="let code of yakit.turpakUrunKodu.split(',')">
                                        {{code.trim()}}
                                    </span>
                                </div>
                                <span *ngIf="!yakit.turpakUrunKodu" class="text-[var(--text-color-secondary)] italic text-sm">Tanımsız</span>
                            </td>
                            <td>
                                <div class="flex items-center gap-2">
                                    <div [style.background]="yakit.renk" class="w-8 h-8 rounded-lg border-2 border-white shadow-sm ring-1 ring-gray-200"></div>
                                    <span class="text-sm font-mono text-[var(--text-color-secondary)]">{{yakit.renk}}</span>
                                </div>
                            </td>
                            <td class="text-center">
                                <div class="flex gap-2 justify-center">
                                    <button pButton icon="pi pi-pencil" class="p-button-text p-button-sm p-button-secondary rounded-full w-8 h-8" (click)="edit(yakit)"></button>
                                    <button pButton icon="pi pi-trash" class="p-button-text p-button-danger p-button-sm rounded-full w-8 h-8" (click)="delete(yakit)"></button>
                                </div>
                            </td>
                        </tr>
                    </ng-template>
                    <ng-template pTemplate="emptymessage">
                        <tr>
                            <td colspan="5" class="text-center py-8 text-[var(--text-color-secondary)]">Kayıt bulunamadı.</td>
                        </tr>
                    </ng-template>
                </p-table>
            </div>
        </div>

        <!-- MODAL -->
        <p-dialog [(visible)]="dialogVisible" [header]="isEdit ? 'Yakıt Düzenle' : 'Yeni Yakıt Ekle'" [modal]="true" [style]="{width: '450px'}" styleClass="p-fluid">
            <div class="flex flex-col gap-5 py-2 mt-2">
                
                <!-- Ad -->
                <div class="flex flex-col gap-2">
                    <label class="font-bold text-sm text-[var(--text-color)]">Yakıt Adı</label>
                    <input pInputText [(ngModel)]="yakit.ad" placeholder="Örn: Motorin" class="w-full" />
                </div>

                <!-- Sıra -->
                <div class="flex flex-col gap-2">
                    <label class="font-bold text-sm text-[var(--text-color)]">Sıra No (Listeleme Önceliği)</label>
                    <p-inputNumber [(ngModel)]="yakit.sira" [showButtons]="true" [min]="0" buttonLayout="horizontal" 
                        inputId="sira" spinnerMode="horizontal" [step]="1"
                        decrementButtonClass="p-button-secondary" incrementButtonClass="p-button-secondary" 
                        incrementButtonIcon="pi pi-plus" decrementButtonIcon="pi pi-minus" class="w-full">
                    </p-inputNumber>
                </div>

                <!-- Renk -->
                <div class="flex flex-col gap-2">
                    <label class="font-bold text-sm text-[var(--text-color)]">Renk Tanımı</label>
                    <div class="flex items-center gap-4 p-3 rounded-lg border border-[var(--surface-border)] bg-[var(--surface-ground)]">
                        <div class="flex flex-col items-center gap-1">
                            <p-colorPicker [(ngModel)]="yakit.renk" [inline]="false"></p-colorPicker>
                        </div>
                        <div class="flex-1">
                            <div class="text-xs font-semibold mb-1 text-[var(--text-color-secondary)]">Önizleme</div>
                             <div class="bg-white p-3 rounded-lg border-t-4 shadow-sm flex items-center justify-between" [style.border-top-color]="yakit.renk">
                                <span class="font-bold text-gray-800">{{ yakit.ad || 'Yakıt Adı' }}</span>
                                <span class="text-xs px-2 py-1 rounded bg-gray-200 text-gray-600 font-bold">1.250 Lt</span>
                             </div>
                        </div>
                    </div>
                </div>

                <!-- Otomasyon Anahtarları -->
                <div class="flex flex-col gap-2">
                    <label class="font-bold text-sm flex items-center justify-between text-[var(--text-color)]">
                        Otomasyon Eşleşme Anahtarları
                    </label>
                    <textarea pInputTextarea [(ngModel)]="yakit.otomasyonUrunAdi" rows="2" placeholder="Örn: MOTORIN,DIZEL,EURODIESEL" class="w-full"></textarea>
                    
                    <div class="bg-blue-50 text-blue-700 p-3 rounded text-xs border border-blue-100 flex gap-2">
                         <i class="pi pi-info-circle text-lg"></i>
                         <span class="leading-relaxed">Otomasyon sisteminden gelen veri bu anahtarlardan birini içeriyorsa ("DIESEL" gibi) yakıt türü otomatik tanınır. Virgülle ayırarak birden fazla girilebilir.</span>
                    </div>
                </div>

                <!-- Turpak Ürün Kodları -->
                <div class="flex flex-col gap-2">
                    <label class="font-bold text-sm flex items-center justify-between text-[var(--text-color)]">
                        Turpak Ürün Kodları (XML ID)
                    </label>
                    <input pInputText [(ngModel)]="yakit.turpakUrunKodu" placeholder="Örn: 4,5,6" class="w-full" />
                    
                    <div class="bg-indigo-50 text-indigo-700 p-3 rounded text-xs border border-indigo-100 flex gap-2">
                         <i class="pi pi-info-circle text-lg"></i>
                         <span class="leading-relaxed">XML dosyasındaki 'FuelType' alanında gelen sayısal kodlar (4, 5, 6 vb.). Virgülle ayırarak birden fazla girilebilir.</span>
                    </div>
                </div>

            </div>

            <ng-template pTemplate="footer">
                <p-button label="Vazgeç" icon="pi pi-times" [text]="true" severity="secondary" (onClick)="dialogVisible = false"></p-button>
                <p-button label="Kaydet" icon="pi pi-check" severity="primary" (onClick)="save()"></p-button>
            </ng-template>
        </p-dialog>
        
        <p-toast></p-toast>
        <p-confirmDialog [style]="{width: '350px'}"></p-confirmDialog>
    </div>
    `
})
export class YakitTanimlariComponent implements OnInit {
    yakitlar: Yakit[] = [];
    yakit: Partial<Yakit> = {};
    loading: boolean = false;
    dialogVisible: boolean = false;
    isEdit: boolean = false;

    constructor(
        private yakitService: YakitService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.loadData();
    }

    loadData() {
        this.loading = true;
        this.yakitService.getAll().subscribe({
            next: (data) => {
                this.yakitlar = data;
                this.loading = false;
            },
            error: () => this.loading = false
        });
    }

    openNew() {
        this.yakit = { sira: this.yakitlar.length + 1, renk: '#3b82f6' };
        this.isEdit = false;
        this.dialogVisible = true;
    }

    edit(item: Yakit) {
        this.yakit = { ...item };
        this.isEdit = true;
        this.dialogVisible = true;
    }

    save() {
        if (!this.yakit.ad) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Ad alanı zorunludur.' });
            return;
        }

        const obs = this.isEdit
            ? this.yakitService.update(this.yakit.id!, this.yakit)
            : this.yakitService.add(this.yakit);

        obs.subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kaydedildi.' });
                this.dialogVisible = false;
                this.loadData();
            },
            error: (err) => {
                console.error('Kaydetme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: err.error?.message || err.error || 'Kaydetme başarısız. Sunucu hatası.'
                });
            }
        });
    }

    delete(item: Yakit) {
        this.confirmationService.confirm({
            message: `${item.ad} yakıt türünü silmek istediğinize emin misiniz?`,
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.yakitService.delete(item.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Silindi.' });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Silinemedi.' });
                    }
                });
            }
        });
    }
}
