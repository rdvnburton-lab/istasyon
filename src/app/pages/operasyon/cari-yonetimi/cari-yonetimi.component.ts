import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { CheckboxModule } from 'primeng/checkbox';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { VardiyaApiService } from '../services/vardiya-api.service';
import { CariKart, CariHareket } from '../models/vardiya.model';
import { AuthService } from '../../../services/auth.service';

@Component({
    selector: 'app-cari-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        ToastModule,
        TagModule,
        TooltipModule,
        CheckboxModule,
        IconFieldModule,
        InputIconModule,
        InputNumberModule
    ],
    providers: [MessageService],
    templateUrl: './cari-yonetimi.component.html',
    styleUrls: ['./cari-yonetimi.component.scss']
})
export class CariYonetimiComponent implements OnInit {
    cariKartlar: CariKart[] = [];
    selectedCari: CariKart | null = null;
    currentCari: any = {};

    dialogVisible = false;
    ekstreVisible = false;
    tahsilatVisible = false;
    editMode = false;
    tahsilatEditMode = false;
    loading = false;
    hareketlerLoading = false;

    tahsilatData: any = {
        tutar: 0,
        aciklama: '',
        hareketId: null
    };

    hareketler: CariHareket[] = [];
    istasyonId: number = 0;

    constructor(
        private vardiyaApi: VardiyaApiService,
        private messageService: MessageService,
        private authService: AuthService
    ) { }

    ngOnInit(): void {
        const user = this.authService.getCurrentUser() as any;
        this.istasyonId = user?.selectedIstasyonId;

        // Fallback: Check session storage
        if (!this.istasyonId) {
            const storedId = sessionStorage.getItem('selectedIstasyonId');
            if (storedId) {
                this.istasyonId = parseInt(storedId, 10);
            }
        }

        // Final fallback: First station
        if (!this.istasyonId && user?.istasyonlar?.length) {
            this.istasyonId = user.istasyonlar[0].id;
        }

        if (this.istasyonId > 0) {
            this.loadCariKartlar();
        } else {
            console.warn('Istasyon ID bulunamadı.');
        }
    }

    loadCariKartlar(): void {
        if (!this.istasyonId) return;
        this.loading = true;
        this.vardiyaApi.getCariKartlar(this.istasyonId).subscribe({
            next: (data) => {
                this.cariKartlar = data;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Cari kartlar yüklenemedi' });
                this.loading = false;
            }
        });
    }

    openNew(): void {
        this.currentCari = {
            istasyonId: this.istasyonId,
            ad: '',
            aktif: true,
            limit: 0
        };
        this.editMode = false;
        this.dialogVisible = true;
    }

    editCari(cari: CariKart): void {
        this.currentCari = { ...cari };
        this.editMode = true;
        this.dialogVisible = true;
    }

    saveCari(): void {
        if (!this.currentCari.ad) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Ad alanı zorunludur' });
            return;
        }

        this.loading = true;
        if (this.editMode && this.currentCari.id) {
            this.vardiyaApi.updateCariKart(this.currentCari.id, this.currentCari).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Cari kart güncellendi' });
                    this.loadCariKartlar();
                    this.dialogVisible = false;
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Güncelleme yapılamadı' });
                    this.loading = false;
                }
            });
        } else {
            this.vardiyaApi.createCariKart(this.currentCari).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Cari kart oluşturuldu' });
                    this.loadCariKartlar();
                    this.dialogVisible = false;
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kayıt yapılamadı' });
                    this.loading = false;
                }
            });
        }
    }

    deleteCari(cari: CariKart): void {
        if (!confirm(`${cari.ad} kartını silmek istediğinize emin misiniz?`)) return;

        this.vardiyaApi.deleteCariKart(cari.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Cari kart silindi' });
                this.loadCariKartlar();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silme işlemi başarısız' });
            }
        });
    }

    viewEkstre(cari: CariKart): void {
        this.selectedCari = cari;
        this.hareketlerLoading = true;
        this.ekstreVisible = true;
        this.vardiyaApi.getCariHareketler(cari.id).subscribe({
            next: (data) => {
                this.hareketler = data;
                this.hareketlerLoading = false;
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Ekstre yüklenemedi' });
                this.hareketlerLoading = false;
            }
        });
    }

    openTahsilat(cari: CariKart): void {
        this.selectedCari = cari;
        this.tahsilatData = {
            tutar: 0,
            aciklama: '',
            hareketId: null
        };
        this.tahsilatEditMode = false;
        this.tahsilatVisible = true;
    }

    saveTahsilat(): void {
        if (!this.selectedCari || this.tahsilatData.tutar <= 0) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Geçerli bir tutar giriniz' });
            return;
        }

        this.loading = true;

        // Düzenleme mi yoksa yeni kayıt mı?
        const apiCall = this.tahsilatEditMode && this.tahsilatData.hareketId
            ? this.vardiyaApi.updateCariHareket(this.tahsilatData.hareketId, {
                tutar: this.tahsilatData.tutar,
                aciklama: this.tahsilatData.aciklama
            })
            : this.vardiyaApi.addTahsilat(this.selectedCari.id, {
                tutar: this.tahsilatData.tutar,
                aciklama: this.tahsilatData.aciklama
            });

        const successMessage = this.tahsilatEditMode ? 'Tahsilat kaydı güncellendi' : 'Tahsilat kaydı eklendi';

        apiCall.subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: successMessage });
                this.loadCariKartlar();
                this.tahsilatVisible = false;
                this.loading = false;
                // Ekstre açıksa yenile
                if (this.ekstreVisible && this.selectedCari) {
                    this.viewEkstre(this.selectedCari);
                }
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Tahsilat işlenemedi' });
                this.loading = false;
            }
        });
    }

    editHareket(hareket: CariHareket): void {
        if (hareket.islemTipi !== 'TAHSILAT') {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Sadece tahsilat kayıtları düzenlenebilir' });
            return;
        }

        // Tahsilat dialog'unu düzenleme modunda aç
        this.tahsilatData = {
            tutar: hareket.tutar,
            aciklama: hareket.aciklama || '',
            hareketId: hareket.id
        };

        this.tahsilatEditMode = true;
        this.tahsilatVisible = true;
    }

    deleteHareket(hareket: CariHareket): void {
        if (hareket.islemTipi !== 'TAHSILAT') {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Sadece tahsilat kayıtları silinebilir' });
            return;
        }

        if (!confirm(`${hareket.tutar} TL tutarındaki tahsilat kaydını silmek istediğinize emin misiniz?\n\nAçıklama: ${hareket.aciklama || 'Yok'}`)) {
            return;
        }

        this.loading = true;
        this.vardiyaApi.deleteCariHareket(hareket.id).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tahsilat kaydı silindi' });
                this.loadCariKartlar();
                // Ekstre'yi yenile
                if (this.selectedCari) {
                    this.viewEkstre(this.selectedCari);
                }
                this.loading = false;
            },
            error: (err: any) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silme işlemi başarısız' });
                this.loading = false;
            }
        });
    }
}
