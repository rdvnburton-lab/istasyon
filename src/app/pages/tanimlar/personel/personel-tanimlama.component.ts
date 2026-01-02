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
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { PersonelApiService, Personel } from '../../../services/personel-api.service';
import { AuthService } from '../../../services/auth.service';

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
        SelectModule,
        IconFieldModule,
        InputIconModule
    ],
    providers: [MessageService],
    templateUrl: './personel-tanimlama.component.html',
    styleUrls: ['./personel-tanimlama.component.scss']
})
export class PersonelTanimlamaComponent implements OnInit {
    personeller: Personel[] = [];
    dialogVisible = false;
    editMode = false;
    loading = false;
    isMarketSorumlusu = false;
    isVardiyaSorumlusu = false;
    isIstasyonSorumlusu = false;
    filterFields: string[] = ['otomasyonAdi', 'adSoyad', 'keyId'];

    currentPersonel: Personel = {
        otomasyonAdi: '',
        adSoyad: '',
        keyId: '',
        rol: 'POMPACI',
        aktif: true,
        telefon: ''
    };

    roller = [
        { label: 'Pompacı', value: 'POMPACI' },
        { label: 'Market Görevlisi', value: 'MARKET_GOREVLISI' },
        { label: 'Market Sorumlusu', value: 'MARKET_SORUMLUSU' },
        { label: 'Vardiya Sorumlusu', value: 'VARDIYA_SORUMLUSU' },
        { label: 'Patron', value: 'PATRON' }
    ];

    constructor(
        private personelService: PersonelApiService,
        private messageService: MessageService,
        private authService: AuthService
    ) { }

    ngOnInit(): void {
        const user = this.authService.getCurrentUser();
        const role = user?.role?.toLowerCase();
        this.isMarketSorumlusu = role === 'market sorumlusu' || role === 'market_sorumlusu';
        this.isVardiyaSorumlusu = role === 'vardiya sorumlusu' || role === 'vardiya_sorumlusu';
        this.isIstasyonSorumlusu = role === 'istasyon sorumlusu' || role === 'istasyon_sorumlusu';

        if (this.isMarketSorumlusu) {
            this.filterFields = ['adSoyad', 'telefon'];
        }

        this.loadPersoneller();
        this.filterRoles();
    }

    filterRoles(): void {
        const user = this.authService.getCurrentUser();
        const role = user?.role?.toLowerCase();
        const isAdminOrPatron = role === 'admin' || role === 'patron';

        if (this.isMarketSorumlusu) {
            // Market sorumlusu sadece market personeli ekleyebilir
            this.roller = this.roller.filter(r => r.value === 'MARKET_GOREVLISI' || r.value === 'MARKET_SORUMLUSU');
        } else if (this.isVardiyaSorumlusu) {
            // Vardiya sorumlusu sadece pompacı/vardiya sorumlusu ekleyebilir
            this.roller = this.roller.filter(r => r.value === 'POMPACI' || r.value === 'VARDIYA_SORUMLUSU');
        } else if (this.isIstasyonSorumlusu) {
            // İstasyon sorumlusu tüm rolleri ekleyebilir (patron hariç)
            this.roller = this.roller.filter(r => r.value !== 'PATRON');
        } else if (!isAdminOrPatron) {
            // Diğer roller sadece temel personeli görebilir
            this.roller = this.roller.filter(r => r.value === 'POMPACI' || r.value === 'MARKET_GOREVLISI');
        }
    }

    loadPersoneller(): void {
        this.loading = true;

        this.personelService.getAll().subscribe({
            next: (data) => {
                if (this.isMarketSorumlusu) {
                    // Market sorumlusu sadece market personelini görür
                    this.personeller = data.filter(p => p.rol && (p.rol === 'MARKET_GOREVLISI' || p.rol === 'MARKET_SORUMLUSU'));
                } else if (this.isVardiyaSorumlusu) {
                    // Vardiya sorumlusu sadece pompacı ve vardiya sorumlusunu görür
                    this.personeller = data.filter(p => p.rol === 'POMPACI' || p.rol === 'VARDIYA_SORUMLUSU');
                } else if (this.isIstasyonSorumlusu) {
                    // İstasyon sorumlusu tüm personeli görür
                    this.personeller = data;
                } else {
                    // Admin/Patron tüm personeli görür
                    this.personeller = data;
                }
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
            rol: this.isMarketSorumlusu ? 'MARKET_GOREVLISI' : 'POMPACI',
            aktif: true,
            telefon: ''
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
        if (this.currentPersonel.rol === 'MARKET_GOREVLISI') {
            // Market personeli için otomatik otomasyon adı üret (örn: MKT-123456)
            if (!this.currentPersonel.otomasyonAdi) {
                const uniqueSuffix = Date.now().toString().slice(-6);
                this.currentPersonel.otomasyonAdi = `MKT-${uniqueSuffix}`;
            }
        } else if (!this.currentPersonel.otomasyonAdi) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Otomasyon Adı alanı zorunludur'
            });
            return;
        }

        if (!this.currentPersonel.adSoyad) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Ad Soyad alanı zorunludur'
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
        if (this.isVardiyaSorumlusu) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Yetkisiz İşlem',
                detail: 'Vardiya sorumlusu personel silemez.'
            });
            return;
        }

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

    getRolSeverity(rol: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        switch (rol) {
            case 'PATRON': return 'danger';
            case 'VARDIYA_SORUMLUSU': return 'warn';
            case 'MARKET_SORUMLUSU': return 'warn';
            case 'MARKET_GOREVLISI': return 'info';
            case 'POMPACI': return 'success';
            default: return 'secondary';
        }
    }

    hideDialog(): void {
        this.dialogVisible = false;
    }
}
