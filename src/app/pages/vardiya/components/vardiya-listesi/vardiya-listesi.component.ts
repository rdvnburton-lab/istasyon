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

    constructor(
        private vardiyaService: VardiyaService,
        private txtParser: TxtParserService,
        private messageService: MessageService,
        private router: Router,
        private dbService: DbService
    ) { }

    ngOnInit(): void {
        this.vardiyalariYukle();
    }

    async vardiyalariYukle(): Promise<void> {
        const dbVardiyalar = await this.dbService.tumVardiyalariGetir();
        this.vardiyalar = dbVardiyalar.map(v => ({
            id: v.id!,
            dosyaAdi: v.dosyaAdi,
            yuklemeTarihi: v.yuklemeTarihi,
            baslangicTarih: v.baslangicTarih,
            bitisTarih: v.bitisTarih,
            personelSayisi: v.personelSayisi,
            islemSayisi: v.islemSayisi,
            toplamTutar: v.toplamTutar,
            durum: v.durum === 'ACIK' ? VardiyaDurum.ACIK :
                v.durum === 'ONAY_BEKLIYOR' ? VardiyaDurum.ONAY_BEKLIYOR :
                    v.durum === 'ONAYLANDI' ? VardiyaDurum.ONAYLANDI : VardiyaDurum.REDDEDILDI,
            redNedeni: v.redNedeni,
            satislar: [] // Listede satış detayına gerek yok
        }));
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

    async dosyaYukle(): Promise<void> {
        if (!this.parseSonuc?.basarili || !this.secilenDosya) return;

        // Mükerrer dosya kontrolü
        const mevcutDosyalar = await this.dbService.tumVardiyalariGetir();
        const dosyaVar = mevcutDosyalar.some(v => v.dosyaAdi === this.secilenDosya!.name);

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

        try {
            await this.vardiyaService.vardiyaEkle(yeniVardiya, this.parseSonuc.satislar, this.parseSonuc.filoSatislari);

            this.messageService.add({
                severity: 'success',
                summary: 'Başarılı',
                detail: `${this.parseSonuc.kayitSayisi} kayıt veritabanına yüklendi`
            });

            await this.vardiyalariYukle();
            this.dosyaDialogKapat();

        } catch (error) {
            console.error('Kayıt hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Veritabanına kayıt sırasında hata oluştu!'
            });
        }
    }

    async vardiyaSil(vardiya: YuklenenVardiya): Promise<void> {
        try {
            await this.dbService.vardiyaSil(vardiya.id);
            this.messageService.add({
                severity: 'success',
                summary: 'Silindi',
                detail: 'Vardiya verisi silindi'
            });
            await this.vardiyalariYukle();
        } catch (error) {
            console.error('Silme hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Silme işlemi başarısız oldu'
            });
        }
    }

    async mutabakatYap(vardiya: YuklenenVardiya): Promise<void> {
        console.log('Mutabakat başlatılıyor...', vardiya);

        try {
            // Aktif vardiyayı ve satışları veritabanından yükle
            await this.vardiyaService.setAktifVardiyaById(vardiya.id);

            console.log('Pompa sayfasına yönlendiriliyor...');
            this.router.navigate(['/vardiya/pompa']);
        } catch (error) {
            console.error('Mutabakat başlatma hatası:', error);
            this.messageService.add({
                severity: 'error',
                summary: 'Hata',
                detail: 'Vardiya detayları yüklenemedi!'
            });
        }
    }

    getMutabakatBekleyenSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ACIK).length;
    }

    getTamamlananSayisi(): number {
        return this.vardiyalar.filter(v => v.durum === VardiyaDurum.ONAYLANDI).length;
    }

    getToplamCiro(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.toplamTutar, 0);
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

    getToplamIslem(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.islemSayisi, 0);
    }

    getToplamPersonel(): number {
        return this.vardiyalar.reduce((sum, v) => sum + v.personelSayisi, 0);
    }
}
