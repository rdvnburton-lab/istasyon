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
import { DbService, DBIstasyon } from '../../services/db.service';

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
    istasyonlar: DBIstasyon[] = [];
    istasyonDialog: boolean = false;
    istasyon: DBIstasyon = this.bosIstasyon();
    submitted: boolean = false;

    constructor(
        private dbService: DbService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.listeyiYenile();
    }

    async listeyiYenile() {
        this.istasyonlar = await this.dbService.getIstasyonlar();
    }

    yeniEkle() {
        this.istasyon = this.bosIstasyon();
        this.submitted = false;
        this.istasyonDialog = true;
    }

    duzenle(istasyon: DBIstasyon) {
        this.istasyon = { ...istasyon };
        this.istasyonDialog = true;
    }

    sil(istasyon: DBIstasyon) {
        this.confirmationService.confirm({
            message: '"' + istasyon.ad + '" istasyonunu silmek istediğinize emin misiniz?',
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: async () => {
                await this.dbService.istasyonSil(istasyon.id!);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon silindi.', life: 3000 });
                this.listeyiYenile();
            }
        });
    }

    dialogKapat() {
        this.istasyonDialog = false;
        this.submitted = false;
    }

    async kaydet() {
        this.submitted = true;

        if (this.istasyon.ad?.trim()) {
            if (this.istasyon.id) {
                await this.dbService.istasyonGuncelle(this.istasyon.id, this.istasyon);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon güncellendi.', life: 3000 });
            } else {
                await this.dbService.istasyonEkle(this.istasyon);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İstasyon oluşturuldu.', life: 3000 });
            }

            this.listeyiYenile();
            this.istasyonDialog = false;
            this.istasyon = this.bosIstasyon();
        }
    }

    bosIstasyon(): DBIstasyon {
        return {
            ad: '',
            kod: '',
            adres: '',
            pompaSayisi: 0,
            marketVar: true,
            aktif: true
        };
    }
}
