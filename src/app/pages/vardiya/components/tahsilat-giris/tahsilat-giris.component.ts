import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { TooltipModule } from 'primeng/tooltip';

import { MessageService, ConfirmationService } from 'primeng/api';

import { VardiyaService } from '../../services/vardiya.service';
import { Vardiya, Tahsilat, OdemeYontemi } from '../../models/vardiya.model';

@Component({
    selector: 'app-tahsilat-giris',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        SelectModule,
        InputNumberModule,
        InputTextModule,
        TextareaModule,
        TableModule,
        ToastModule,
        ConfirmDialogModule,
        DialogModule,
        TagModule,
        DividerModule,
        TooltipModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './tahsilat-giris.component.html',
    styleUrls: ['./tahsilat-giris.component.scss']
})
export class TahsilatGiris implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    tahsilatlar: Tahsilat[] = [];

    odemeYontemleri: { label: string; value: OdemeYontemi }[] = [];
    hizliTutarlar = [100, 200, 500, 1000, 2000, 5000];

    yeniTahsilat: { odemeYontemi: OdemeYontemi | null; tutar: number; aciklama: string } = {
        odemeYontemi: null,
        tutar: 0,
        aciklama: ''
    };

    duzenlenenTahsilat: Tahsilat | null = null;
    duzenleDialogVisible = false;

    loading = false;

    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.odemeYontemleri = this.vardiyaService.getOdemeYontemleri();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                this.aktifVardiya = vardiya;
                if (!vardiya) {
                    this.router.navigate(['/vardiya']);
                }
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getTahsilatlar().subscribe(tahsilatlar => {
                this.tahsilatlar = tahsilatlar;
            })
        );
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    get toplamTahsilat(): number {
        return this.tahsilatlar.reduce((sum, t) => sum + t.tutar, 0);
    }

    get tahsilatOzeti(): { label: string; value: OdemeYontemi; toplam: number }[] {
        const ozet: { label: string; value: OdemeYontemi; toplam: number }[] = [];

        this.odemeYontemleri.forEach(yontem => {
            const toplam = this.tahsilatlar
                .filter(t => t.odemeYontemi === yontem.value)
                .reduce((sum, t) => sum + t.tutar, 0);

            if (toplam > 0) {
                ozet.push({
                    label: yontem.label,
                    value: yontem.value,
                    toplam
                });
            }
        });

        return ozet;
    }

    getOdemeLabel(yontem: OdemeYontemi): string {
        const found = this.odemeYontemleri.find(o => o.value === yontem);
        return found?.label || yontem;
    }

    getOdemeSeverity(yontem: OdemeYontemi): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const severities: Record<OdemeYontemi, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            [OdemeYontemi.NAKIT]: 'success',
            [OdemeYontemi.KREDI_KARTI]: 'info',
            [OdemeYontemi.PARO_PUAN]: 'warn',
            [OdemeYontemi.MOBIL_ODEME]: 'secondary',
        };
        return severities[yontem] || 'secondary';
    }

    formatTutar(tutar: number): string {
        return tutar.toLocaleString('tr-TR') + ' ₺';
    }

    hizliTutarEkle(tutar: number): void {
        this.yeniTahsilat.tutar = (this.yeniTahsilat.tutar || 0) + tutar;
    }

    tahsilatEkle(): void {
        if (!this.yeniTahsilat.odemeYontemi || !this.yeniTahsilat.tutar || !this.aktifVardiya) return;

        this.loading = true;

        this.vardiyaService.tahsilatEkle({
            vardiyaId: this.aktifVardiya.id,
            odemeYontemi: this.yeniTahsilat.odemeYontemi,
            tutar: this.yeniTahsilat.tutar,
            aciklama: this.yeniTahsilat.aciklama || undefined
        }).subscribe({
            next: () => {
                this.loading = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Tahsilat eklendi'
                });

                // Formu temizle
                this.yeniTahsilat = {
                    odemeYontemi: null,
                    tutar: 0,
                    aciklama: ''
                };
            },
            error: (err) => {
                this.loading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Tahsilat eklenirken hata oluştu'
                });
            }
        });
    }

    tahsilatDuzenle(tahsilat: Tahsilat): void {
        this.duzenlenenTahsilat = { ...tahsilat };
        this.duzenleDialogVisible = true;
    }

    tahsilatKaydet(): void {
        if (!this.duzenlenenTahsilat) return;

        this.vardiyaService.tahsilatGuncelle(this.duzenlenenTahsilat).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Tahsilat güncellendi'
                });
                this.duzenleDialogVisible = false;
                this.duzenlenenTahsilat = null;
            },
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Tahsilat güncellenirken hata oluştu'
                });
            }
        });
    }

    tahsilatSil(tahsilat: Tahsilat): void {
        this.confirmationService.confirm({
            message: 'Bu tahsilatı silmek istediğinize emin misiniz?',
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Sil',
            rejectLabel: 'İptal',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vardiyaService.tahsilatSil(tahsilat.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Başarılı',
                            detail: 'Tahsilat silindi'
                        });
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Hata',
                            detail: 'Tahsilat silinirken hata oluştu'
                        });
                    }
                });
            }
        });
    }

    tumunuTemizle(): void {
        this.confirmationService.confirm({
            message: 'Tüm tahsilatları silmek istediğinize emin misiniz? Bu işlem geri alınamaz.',
            header: 'Toplu Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Tümünü Sil',
            rejectLabel: 'İptal',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.vardiyaService.tahsilatlariTemizle();
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: 'Tüm tahsilatlar silindi'
                });
            }
        });
    }
}
