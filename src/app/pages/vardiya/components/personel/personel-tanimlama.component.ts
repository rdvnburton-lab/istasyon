import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ToolbarModule } from 'primeng/toolbar';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DbService, DBPersonel, DBIstasyon } from '../../services/db.service';

@Component({
    selector: 'app-personel-tanimlama',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        SelectModule,
        CheckboxModule,
        ToastModule,
        ToolbarModule,
        TagModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './personel-tanimlama.component.html',
    styles: [`:host { display: block; }`]
})
export class PersonelTanimlamaComponent implements OnInit {
    personeller: DBPersonel[] = [];
    istasyonlar: DBIstasyon[] = [];
    personelDialog: boolean = false;
    personel: DBPersonel = this.bosPersonel();
    submitted: boolean = false;

    roller = [
        { label: 'Pompacı', value: 'POMPACI' },
        { label: 'Market Sorumlusu', value: 'MARKET_SORUMLUSU' },
        { label: 'Vardiya Sorumlusu', value: 'VARDIYA_SORUMLUSU' },
        { label: 'Yönetici', value: 'YONETICI' }
    ];

    constructor(
        private dbService: DbService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService
    ) { }

    ngOnInit() {
        this.verileriYukle();
    }

    async verileriYukle() {
        this.personeller = await this.dbService.getPersoneller();
        this.istasyonlar = await this.dbService.getIstasyonlar();
    }

    yeniEkle() {
        this.personel = this.bosPersonel();
        this.submitted = false;
        this.personelDialog = true;
    }

    duzenle(personel: DBPersonel) {
        this.personel = { ...personel };
        this.personelDialog = true;
    }

    sil(personel: DBPersonel) {
        this.confirmationService.confirm({
            message: '"' + personel.tamAd + '" adlı personeli silmek istediğinize emin misiniz?',
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: async () => {
                await this.dbService.personelSil(personel.id!);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel silindi.', life: 3000 });
                this.verileriYukle();
            }
        });
    }

    dialogKapat() {
        this.personelDialog = false;
        this.submitted = false;
    }

    async kaydet() {
        this.submitted = true;

        if (this.personel.ad?.trim() && this.personel.tamAd?.trim()) {
            if (this.personel.id) {
                await this.dbService.personelGuncelle(this.personel.id, this.personel);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel güncellendi.', life: 3000 });
            } else {
                await this.dbService.personelEkle(this.personel);
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel oluşturuldu.', life: 3000 });
            }

            this.verileriYukle();
            this.personelDialog = false;
            this.personel = this.bosPersonel();
        }
    }

    bosPersonel(): DBPersonel {
        // Varsayılan istasyon varsa onu seç
        const varsayilanIstasyonId = this.istasyonlar.length > 0 ? this.istasyonlar[0].id : 1;
        return {
            keyId: '',
            ad: '',
            soyad: '',
            tamAd: '',
            istasyonId: varsayilanIstasyonId!,
            rol: 'POMPACI',
            aktif: true
        };
    }

    getIstasyonAdi(id: number): string {
        const ist = this.istasyonlar.find(i => i.id === id);
        return ist ? ist.ad : '-';
    }
}
