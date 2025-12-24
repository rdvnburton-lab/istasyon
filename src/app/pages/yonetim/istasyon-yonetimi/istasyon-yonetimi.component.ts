import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { TextareaModule } from 'primeng/textarea';
import { YonetimService, Istasyon, CreateIstasyonDto, UpdateIstasyonDto } from '../services/yonetim.service';

@Component({
    selector: 'app-istasyon-yonetimi',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, CheckboxModule, ToastModule, TextareaModule],
    providers: [MessageService],
    template: `
        <div class="card">
            <p-toast></p-toast>
            <div class="flex justify-content-between align-items-center mb-4">
                <h2 class="m-0">İstasyon Yönetimi</h2>
                <button pButton label="Yeni İstasyon" icon="pi pi-plus" class="p-button-success" (click)="openNew()"></button>
            </div>

            <p-table [value]="istasyonlar" [tableStyle]="{'min-width': '50rem'}">
                <ng-template pTemplate="header">
                    <tr>
                        <th>ID</th>
                        <th>Ad</th>
                        <th>Adres</th>
                        <th>Durum</th>
                        <th>İşlemler</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-istasyon>
                    <tr>
                        <td>{{istasyon.id}}</td>
                        <td>{{istasyon.ad}}</td>
                        <td>{{istasyon.adres}}</td>
                        <td>
                            <span [class]="'badge ' + (istasyon.aktif ? 'status-active' : 'status-passive')">
                                {{istasyon.aktif ? 'Aktif' : 'Pasif'}}
                            </span>
                        </td>
                        <td>
                            <button pButton icon="pi pi-pencil" class="p-button-rounded p-button-text p-button-warning mr-2" (click)="editIstasyon(istasyon)"></button>
                            <button pButton icon="pi pi-trash" class="p-button-rounded p-button-text p-button-danger" (click)="deleteIstasyon(istasyon)"></button>
                        </td>
                    </tr>
                </ng-template>
            </p-table>

            <p-dialog [(visible)]="dialogVisible" [style]="{width: '450px'}" header="İstasyon Detay" [modal]="true" styleClass="p-fluid">
                <ng-template pTemplate="content">
                    <div class="field">
                        <label for="ad">İstasyon Adı</label>
                        <input type="text" pInputText id="ad" [(ngModel)]="istasyon.ad" required autofocus />
                        <small class="p-error" *ngIf="submitted && !istasyon.ad">Ad gereklidir.</small>
                    </div>
                    <div class="field">
                        <label for="adres">Adres</label>
                        <textarea id="adres" pTextarea [(ngModel)]="istasyon.adres" rows="3" cols="20"></textarea>
                    </div>
                    <div class="field-checkbox" *ngIf="istasyon.id">
                        <p-checkbox [(ngModel)]="istasyon.aktif" [binary]="true" inputId="aktif"></p-checkbox>
                        <label for="aktif">Aktif</label>
                    </div>
                </ng-template>

                <ng-template pTemplate="footer">
                    <button pButton label="İptal" icon="pi pi-times" class="p-button-text" (click)="hideDialog()"></button>
                    <button pButton label="Kaydet" icon="pi pi-check" class="p-button-text" (click)="saveIstasyon()"></button>
                </ng-template>
            </p-dialog>
        </div>
    `,
    styles: [`
        .status-active { color: green; font-weight: bold; }
        .status-passive { color: red; font-weight: bold; }
        .field { margin-bottom: 1rem; }
        .field-checkbox { display: flex; align-items: center; gap: 0.5rem; margin-bottom: 1rem; }
    `]
})
export class IstasyonYonetimiComponent implements OnInit {
    istasyonlar: Istasyon[] = [];
    istasyon: any = {};
    dialogVisible: boolean = false;
    submitted: boolean = false;

    constructor(private yonetimService: YonetimService, private messageService: MessageService) { }

    ngOnInit() {
        this.loadIstasyonlar();
    }

    loadIstasyonlar() {
        this.yonetimService.getIstasyonlar().subscribe(data => {
            this.istasyonlar = data;
        });
    }

    openNew() {
        this.istasyon = {};
        this.submitted = false;
        this.dialogVisible = true;
    }

    editIstasyon(istasyon: Istasyon) {
        this.istasyon = { ...istasyon };
        this.dialogVisible = true;
    }

    deleteIstasyon(istasyon: Istasyon) {
        if (confirm(istasyon.ad + ' istasyonunu silmek istediğinize emin misiniz?')) {
            this.yonetimService.deleteIstasyon(istasyon.id).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon silindi' });
                    this.loadIstasyonlar();
                },
                error: () => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silme işlemi başarısız' });
                }
            });
        }
    }

    hideDialog() {
        this.dialogVisible = false;
        this.submitted = false;
    }

    saveIstasyon() {
        this.submitted = true;

        if (this.istasyon.ad?.trim()) {
            if (this.istasyon.id) {
                const updateDto: UpdateIstasyonDto = {
                    ad: this.istasyon.ad,
                    adres: this.istasyon.adres,
                    aktif: this.istasyon.aktif
                };
                this.yonetimService.updateIstasyon(this.istasyon.id, updateDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon güncellendi' });
                        this.loadIstasyonlar();
                        this.hideDialog();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Güncelleme başarısız' });
                    }
                });
            } else {
                const createDto: CreateIstasyonDto = {
                    ad: this.istasyon.ad,
                    adres: this.istasyon.adres,
                };
                this.yonetimService.createIstasyon(createDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon oluşturuldu' });
                        this.loadIstasyonlar();
                        this.hideDialog();
                    },
                    error: () => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Oluşturma başarısız' });
                    }
                });
            }
        }
    }
}
