import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { IstasyonService, Istasyon, CreateIstasyonDto, UpdateIstasyonDto } from '../../../../services/istasyon.service';

@Component({
    selector: 'app-istasyon-tanimlama',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        TextareaModule,
        InputNumberModule,
        CheckboxModule,
        ToastModule,
        ToolbarModule,
        TagModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './istasyon-tanimlama.component.html',
    styles: [`:host { display: block; }`]
})
export class IstasyonTanimlamaComponent implements OnInit {
    istasyonlar: Istasyon[] = [];
    istasyonDialog: boolean = false;
    istasyon: Istasyon = this.bosIstasyon();
    submitted: boolean = false;

    constructor(
        private istasyonService: IstasyonService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.listeyiYenile();
    }

    listeyiYenile() {
        this.istasyonService.getIstasyonlar().subscribe({
            next: (data) => {
                this.istasyonlar = data;
            },
            error: (err) => {
                console.error('İstasyonlar yüklenirken hata:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'İstasyonlar yüklenemedi.' });
            }
        });
    }

    yeniEkle() {
        this.istasyon = this.bosIstasyon();
        this.submitted = false;
        this.istasyonDialog = true;
    }

    duzenle(istasyon: Istasyon) {
        this.istasyon = { ...istasyon };
        this.istasyonDialog = true;
    }

    sil(istasyon: Istasyon) {
        this.confirmationService.confirm({
            message: '"' + istasyon.ad + '" istasyonunu silmek istediğinize emin misiniz?',
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.istasyonService.deleteIstasyon(istasyon.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon silindi.', life: 3000 });
                        this.listeyiYenile();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'İstasyon silinemedi.' });
                    }
                });
            }
        });
    }

    dialogKapat() {
        this.istasyonDialog = false;
        this.submitted = false;
    }

    kaydet() {
        this.submitted = true;

        if (this.istasyon.ad?.trim()) {
            if (this.istasyon.id) {
                const updateDto: UpdateIstasyonDto = {
                    ad: this.istasyon.ad,
                    adres: this.istasyon.adres,
                    aktif: this.istasyon.aktif,
                    istasyonSorumluId: this.istasyon.istasyonSorumluId,
                    vardiyaSorumluId: this.istasyon.vardiyaSorumluId,
                    marketSorumluId: this.istasyon.marketSorumluId
                };

                this.istasyonService.updateIstasyon(this.istasyon.id, updateDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon güncellendi.', life: 3000 });
                        this.listeyiYenile();
                        this.istasyonDialog = false;
                        this.istasyon = this.bosIstasyon();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Güncelleme başarısız.' });
                    }
                });
            } else {
                const createDto: CreateIstasyonDto = {
                    ad: this.istasyon.ad,
                    adres: this.istasyon.adres,
                    firmaId: this.istasyon.firmaId,
                    istasyonSorumluId: this.istasyon.istasyonSorumluId,
                    vardiyaSorumluId: this.istasyon.vardiyaSorumluId,
                    marketSorumluId: this.istasyon.marketSorumluId
                };

                this.istasyonService.createIstasyon(createDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon oluşturuldu.', life: 3000 });
                        this.listeyiYenile();
                        this.istasyonDialog = false;
                        this.istasyon = this.bosIstasyon();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Oluşturma başarısız.' });
                    }
                });
            }
        }
    }

    bosIstasyon(): Istasyon {
        return {
            id: 0,
            ad: '',
            adres: '',
            aktif: true,
            firmaId: 0
        };
    }
}

