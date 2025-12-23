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
import { SelectModule } from 'primeng/select';
import { PersonelApiService, Personel } from '../../../../services/personel-api.service';

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
        ToastModule,
        TagModule,
        TooltipModule,
        CheckboxModule,
        SelectModule
    ],
    providers: [MessageService],
    templateUrl: './personel-tanimlama.component.html',
    styles: [`:host { display: block; }`]
})
export class PersonelTanimlamaComponent implements OnInit {
    personeller: Personel[] = [];
    dialogVisible = false;
    editMode = false;
    loading = false;

    currentPersonel: Personel = {
        otomasyonAdi: '',
        adSoyad: '',
        keyId: '',
        rol: 'POMPACI',
        aktif: true
    };

    roller = [
        { label: 'Pompacı', value: 'POMPACI' },
        { label: 'Market Görevlisi', value: 'MARKET_GOREVLISI' },
        { label: 'Vardiya Sorumlusu', value: 'VARDIYA_SORUMLUSU' },
        { label: 'Patron', value: 'PATRON' }
    ];

    constructor(
        private personelService: PersonelApiService,
        private messageService: MessageService
    ) { }

    ngOnInit(): void {
        this.loadPersoneller();
    }

    loadPersoneller(): void {
        this.loading = true;
        this.personelService.getAll().subscribe({
            next: (data) => {
                this.personeller = data;
                this.loading = false;
            },
            error: (err) => {
                console.error('Personeller yüklenirken hata:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Personeller yüklenemedi'
                });
                this.loading = false;
            }
        });
    }

    openNew(): void {
        this.currentPersonel = {
            otomasyonAdi: '',
            adSoyad: '',
            keyId: '',
            rol: 'POMPACI',
            aktif: true
        };
        this.editMode = false;
        this.dialogVisible = true;
    }

    editPersonel(personel: Personel): void {
        this.currentPersonel = { ...personel };
        this.editMode = true;
        this.dialogVisible = true;
    }

    savePersonel(): void {
        if (!this.currentPersonel.otomasyonAdi || !this.currentPersonel.adSoyad) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Otomasyon Adı ve Ad Soyad alanları zorunludur'
            });
            return;
        }

        this.loading = true;

        if (this.editMode && this.currentPersonel.id) {
            this.personelService.update(this.currentPersonel.id, this.currentPersonel).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Başarılı',
                        detail: 'Personel güncellendi'
                    });
                    this.loadPersoneller();
                    this.dialogVisible = false;
                },
                error: (err) => {
                    console.error('Güncelleme hatası:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hata',
                        detail: err.error?.message || 'Personel güncellenemedi'
                    });
                    this.loading = false;
                }
            });
        } else {
            this.personelService.create(this.currentPersonel).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Başarılı',
                        detail: 'Personel eklendi'
                    });
                    this.loadPersoneller();
                    this.dialogVisible = false;
                },
                error: (err) => {
                    console.error('Ekleme hatası:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Hata',
                        detail: err.error?.message || 'Personel eklenemedi'
                    });
                    this.loading = false;
                }
            });
        }
    }

    deletePersonel(personel: Personel): void {
        if (!personel.id) return;

        if (!confirm(`${personel.adSoyad} adlı personeli silmek istediğinize emin misiniz?`)) {
            return;
        }

        this.personelService.delete(personel.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Personel silindi'
                });
                this.loadPersoneller();
            },
            error: (err) => {
                console.error('Silme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: err.error?.message || 'Personel silinemedi'
                });
            }
        });
    }

    toggleAktif(personel: Personel): void {
        if (!personel.id) return;

        this.personelService.toggleAktif(personel.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Personel durumu güncellendi'
                });
                this.loadPersoneller();
            },
            error: (err) => {
                console.error('Durum güncelleme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Durum güncellenemedi'
                });
            }
        });
    }

    getRolLabel(rol: string): string {
        const rolObj = this.roller.find(r => r.value === rol);
        return rolObj ? rolObj.label : rol;
    }

    hideDialog(): void {
        this.dialogVisible = false;
    }
}
