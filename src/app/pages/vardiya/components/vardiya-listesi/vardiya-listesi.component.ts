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

import { VardiyaService } from '../../services/vardiya.service';
import { VardiyaApiService } from '../../services/vardiya-api.service';
import { TxtParserService, ParseSonuc } from '../../services/txt-parser.service';
import { VardiyaDurum, OtomasyonSatis, Vardiya } from '../../models/vardiya.model';
import { DbService } from '../../services/db.service';

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
    redNedeni?: string;
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
    templateUrl: './vardiya-listesi.component.html',
    styleUrls: ['./vardiya-listesi.component.scss']
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

    summary: any = {
        toplamCiro: 0,
        toplamIslem: 0,
        benzersizPersonelSayisi: 0
    };

    constructor(
        private vardiyaService: VardiyaService,
        private vardiyaApiService: VardiyaApiService,
        private txtParser: TxtParserService,
        private messageService: MessageService,
        private router: Router,
        private dbService: DbService
    ) { }

    ngOnInit(): void {
        this.vardiyalariYukle();
    }

    vardiyalariYukle(): void {
        this.vardiyaApiService.getVardiyalar().subscribe({
            next: (response: any) => {
                const data = response.items || [];
                this.summary = response.summary || this.summary;

                this.vardiyalar = data.map((v: any) => ({
                    id: v.id,
                    dosyaAdi: v.dosyaAdi,
                    yuklemeTarihi: new Date(v.olusturmaTarihi),
                    baslangicTarih: new Date(v.baslangicTarihi),
                    bitisTarih: v.bitisTarihi ? new Date(v.bitisTarihi) : null,
                    personelSayisi: v.personelSayisi || 0,
                    islemSayisi: v.islemSayisi || 0,
                    toplamTutar: v.genelToplam,
                    durum: (v.durum === 'ACIK' || v.durum === 0) ? VardiyaDurum.ACIK :
                        (v.durum === 'ONAY_BEKLIYOR' || v.durum === 1) ? VardiyaDurum.ONAY_BEKLIYOR :
                            (v.durum === 'ONAYLANDI' || v.durum === 2) ? VardiyaDurum.ONAYLANDI :
                                (v.durum === 'REDDEDILDI' || v.durum === 3) ? VardiyaDurum.REDDEDILDI : VardiyaDurum.ACIK,
                    redNedeni: '',
                    satislar: []
                }));
            },
            error: (err) => {
                console.error('Vardiyalar yüklenirken hata:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Vardiya listesi yüklenemedi!'
                });
            }
        });
    }

    formatShortNumber(value: number): string {
        return new Intl.NumberFormat('tr-TR', {
            notation: 'compact',
            compactDisplay: 'short',
            maximumFractionDigits: 1
        }).format(value);
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

    dosyaYukle(): void {
        if (!this.parseSonuc?.basarili || !this.secilenDosya) return;

        // Mükerrer dosya kontrolü (Basit kontrol)
        const dosyaVar = this.vardiyalar.some(v => v.dosyaAdi === this.secilenDosya!.name);

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

        // Dosya içeriğini oku
        const reader = new FileReader();
        reader.onload = (e) => {
            const dosyaIcerik = e.target?.result as string; // Data URL olarak gelecek

            // Sadece Backend'e kaydet
            this.vardiyaApiService.createVardiya(yeniVardiya, this.parseSonuc!.satislar, this.parseSonuc!.filoSatislari, dosyaIcerik)
                .subscribe({
                    next: (response) => {
                        console.log('Backend kaydı başarılı:', response);

                        this.messageService.add({
                            severity: 'success',
                            summary: 'Başarılı',
                            detail: `${this.parseSonuc?.kayitSayisi} kayıt veritabanına yüklendi`
                        });

                        this.vardiyalariYukle();
                        this.dosyaDialogKapat();
                    },
                    error: (err) => {
                        console.error('Backend kayıt hatası:', err);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Hata',
                            detail: 'Sunucuya kayıt yapılırken hata oluştu!'
                        });
                    }
                });
        };
        reader.readAsDataURL(this.secilenDosya); // Base64 okuma
    }

    vardiyaSil(vardiya: YuklenenVardiya): void {
        this.vardiyaApiService.deleteVardiya(vardiya.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Silindi',
                    detail: 'Vardiya verisi silindi'
                });
                this.vardiyalariYukle();
            },
            error: (err) => {
                console.error('Silme hatası:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: 'Silme işlemi başarısız oldu'
                });
            }
        });
    }

    dosyaIndir(vardiya: YuklenenVardiya): void {
        this.vardiyaApiService.downloadDosya(vardiya.id);
    }

    mutabakatYap(vardiya: YuklenenVardiya): void {
        console.log('Mutabakat başlatılıyor, vardiya ID:', vardiya.id);
        this.router.navigate(['/vardiya/pompa', vardiya.id]);
    }

    getMutabakatBekleyenSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ACIK || v.durum === VardiyaDurum.REDDEDILDI).length;
    }

    getTamamlananSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ONAYLANDI).length;
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


}
