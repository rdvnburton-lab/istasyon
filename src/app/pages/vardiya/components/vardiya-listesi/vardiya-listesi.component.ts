import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { TabsModule } from 'primeng/tabs';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../../services/vardiya.service';
import { VardiyaApiService } from '../../services/vardiya-api.service';
import { TxtParserService, ParseSonuc } from '../../services/txt-parser.service';
import { VardiyaDurum, OtomasyonSatis, Vardiya, PersonelFarkAnalizi, MarketOzet, GenelOzet, VardiyaOzet, FarkDurum } from '../../models/vardiya.model';

import { AuthService } from '../../../../services/auth.service';

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
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        ToastModule,
        TooltipModule,
        FileUploadModule,
        DialogModule,
        ProgressBarModule,
        TabsModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule
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
    isAutomaticFile: boolean = false;
    currentAutomaticFileId: number | null = null;
    currentAutomaticFileContent: string | null = null;
    otomatikDosyalar: any[] = [];

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

    // Silme Talebi Dialog
    silmeTalebiDialogVisible = false;
    secilenVardiya: YuklenenVardiya | null = null;
    silmeNedeni = '';
    userRole: string | null = null;

    // Detay Modalı İçin
    detayVisible = false;
    seciliVardiyaDetay: any = null;
    genelOzet: GenelOzet | null = null;
    farkAnalizi: PersonelFarkAnalizi[] = [];
    marketOzet: MarketOzet | null = null;
    vardiyaOzet: VardiyaOzet | null = null;
    activeTab = '0';
    pusulaKrediKartiDetaylari: any[] = [];
    toplamKrediKarti = 0;
    pompaciSatisTutar = 0;
    pompaciNakitToplam = 0;
    pompaFark = 0;
    pompaFarkDurumRenk: 'success' | 'warn' | 'danger' = 'success';
    Math = Math;

    constructor(
        private vardiyaService: VardiyaService,
        private vardiyaApiService: VardiyaApiService,
        private txtParser: TxtParserService,
        private messageService: MessageService,
        private router: Router,

        private authService: AuthService
    ) { }

    ngOnInit(): void {
        this.userRole = this.authService.getCurrentUser()?.role || null;
        this.vardiyalariYukle();
        this.otomatikDosyalariYukle();
    }


    otomatikDosyalariYukle(): void {
        this.vardiyaApiService.getPendingAutomaticFiles().subscribe({
            next: (files) => {
                this.otomatikDosyalar = files;
            },
            error: (err) => console.error('Otomatik dosyalar yüklenemedi:', err)
        });
    }

    otomatikDosyaIsle(file: any): void {
        this.yukleniyor = true;
        this.islemDurumu = 'Dosya sunucudan alınıyor...';

        this.vardiyaApiService.getAutomaticFileContent(file.id).subscribe({
            next: (data) => {
                // Base64 içeriği çöz
                const binaryString = window.atob(data.icerik);
                const bytes = new Uint8Array(binaryString.length);
                for (let i = 0; i < binaryString.length; i++) {
                    bytes[i] = binaryString.charCodeAt(i);
                }

                // Windows-1254 (Turkish) decoding
                const decoder = new TextDecoder('windows-1254');
                const icerik = decoder.decode(bytes);

                this.islemDurumu = 'Dosya parse ediliyor...';
                const sonuc = this.txtParser.parseOtomasyonDosyasi(icerik);

                if (sonuc.basarili) {
                    this.parseSonuc = sonuc;
                    this.secilenDosya = { name: data.dosyaAdi } as File; // Mock file object

                    // Otomatik dosya olduğunu işaretle
                    this.isAutomaticFile = true;
                    this.currentAutomaticFileId = data.id;
                    this.currentAutomaticFileContent = data.icerik;

                    this.dosyaDialogVisible = true;
                    this.yukleniyor = false;
                } else {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Dosya parse edilemedi!' });
                    this.yukleniyor = false;
                }
            },
            error: (err) => {
                console.error('Dosya içeriği alınamadı:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Dosya içeriği sunucudan alınamadı!' });
                this.yukleniyor = false;
            }
        });
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
                                (v.durum === 'REDDEDILDI' || v.durum === 3) ? VardiyaDurum.REDDEDILDI :
                                    (v.durum === 'SILINME_ONAYI_BEKLIYOR' || v.durum === 4) ? VardiyaDurum.SILINME_ONAYI_BEKLIYOR :
                                        (v.durum === 'SILINDI' || v.durum === 5) ? VardiyaDurum.SILINDI : VardiyaDurum.ACIK,
                    redNedeni: '',
                    satislar: []
                }))
                    // Silinmiş vardiyaları listeden çıkar
                    .filter((v: YuklenenVardiya) => v.durum !== VardiyaDurum.SILINDI);
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
        this.isAutomaticFile = false;
        this.currentAutomaticFileId = null;
        this.currentAutomaticFileContent = null;
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
        if (!this.parseSonuc?.basarili || !this.secilenDosya || this.yukleniyor) return;

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

        this.yukleniyor = true; // Yükleme başlıyor

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

        const finalizeUpload = (dosyaIcerik?: string) => {
            this.vardiyaApiService.createVardiya(yeniVardiya, this.parseSonuc!.satislar, this.parseSonuc!.filoSatislari, dosyaIcerik)
                .subscribe({
                    next: (response) => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Başarılı',
                            detail: `${this.parseSonuc?.kayitSayisi} kayıt veritabanına yüklendi`
                        });

                        // Eğer otomatik dosya ise, işlendi olarak işaretle
                        if (this.isAutomaticFile && this.currentAutomaticFileId) {
                            this.vardiyaApiService.markAsProcessed(this.currentAutomaticFileId).subscribe(() => {
                                this.otomatikDosyalariYukle();
                            });
                        }

                        this.vardiyalariYukle();
                        this.dosyaDialogKapat();
                        this.isAutomaticFile = false;
                        this.currentAutomaticFileId = null;
                        this.yukleniyor = false; // Yükleme bitti
                    },
                    error: (err) => {
                        console.error('Backend kayıt hatası:', err);
                        let detail = 'Sunucuya kayıt yapılırken hata oluştu!';

                        if (err.error && typeof err.error === 'string') {
                            detail = err.error;
                        } else if (err.error && err.error.message) {
                            detail = err.error.message;
                        } else if (err.error && err.error.errors) {
                            // FluentValidation errors
                            const errors = err.error.errors;
                            const firstError = Object.values(errors)[0] as string[];
                            if (firstError && firstError.length > 0) {
                                detail = firstError[0];
                            }
                        }

                        this.messageService.add({
                            severity: 'error',
                            summary: 'Hata',
                            detail: detail,
                            life: 7000
                        });
                        this.yukleniyor = false; // Yükleme bitti (hata ile)
                    }
                });
        };

        if (this.isAutomaticFile && this.currentAutomaticFileContent) {
            // Otomatik dosyada içeriği sunucuya geri gönderiyoruz (validator zorunlu kılıyor)
            finalizeUpload(this.currentAutomaticFileContent);
        } else {
            // Manuel yüklemede dosyayı oku
            const reader = new FileReader();
            reader.onload = (e) => {
                const dosyaIcerik = e.target?.result as string;
                finalizeUpload(dosyaIcerik);
            };
            reader.readAsDataURL(this.secilenDosya as any);
        }
    }

    vardiyaSil(vardiya: YuklenenVardiya): void {
        this.secilenVardiya = vardiya;
        this.silmeNedeni = '';
        this.silmeTalebiDialogVisible = true;
    }

    silmeTalebiDialogKapat(): void {
        this.silmeTalebiDialogVisible = false;
        this.secilenVardiya = null;
        this.silmeNedeni = '';
    }

    silmeTalebiGonder(): void {
        if (!this.secilenVardiya || !this.silmeNedeni || this.silmeNedeni.trim().length < 10) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Lütfen en az 10 karakter uzunluğunda bir silme nedeni giriniz.'
            });
            return;
        }

        this.vardiyaApiService.vardiyaSilmeTalebi(this.secilenVardiya.id, this.silmeNedeni).subscribe({
            next: (response: any) => {
                const message = response?.message || 'Silme talebi gönderildi';
                this.messageService.add({
                    severity: 'success',
                    summary: 'Başarılı',
                    detail: message
                });
                this.silmeTalebiDialogKapat();
                this.vardiyalariYukle();
            },
            error: (err) => {
                console.error('Silme talebi hatası:', err);
                const errorMsg = err.error?.message || 'Silme talebi gönderilemedi';
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: errorMsg
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

    incele(vardiya: YuklenenVardiya): void {
        if (this.yukleniyor) return;

        this.yukleniyor = true;
        this.seciliVardiyaDetay = vardiya;
        this.messageService.add({ severity: 'info', summary: 'Lütfen Bekleyiniz', detail: 'Vardiya detayları yükleniyor...', life: 2000 });

        this.vardiyaApiService.getOnayDetay(vardiya.id).subscribe({
            next: (data) => {
                // 1. Genel Özet
                this.genelOzet = {
                    pompaToplam: data.genelOzet.pompaToplam,
                    marketToplam: data.genelOzet.marketToplam,
                    genelToplam: data.genelOzet.genelToplam,
                    toplamNakit: data.genelOzet.toplamNakit,
                    toplamKrediKarti: data.genelOzet.toplamKrediKarti,
                    toplamParoPuan: data.genelOzet.toplamParoPuan,
                    toplamMobilOdeme: data.genelOzet.toplamMobilOdeme,
                    toplamGider: 0,
                    toplamFark: data.genelOzet.toplamFark,
                    durumRenk: data.genelOzet.durumRenk
                };

                // 2. Fark Analizi
                this.farkAnalizi = (data.farkAnalizi || []).map((item: any) => ({
                    personelId: item.personelId,
                    personelAdi: item.personelAdi,
                    otomasyonToplam: item.otomasyonToplam,
                    pusulaToplam: item.pusulaToplam,
                    fark: item.fark,
                    farkDurum: item.farkDurum === 'UYUMLU' ? FarkDurum.UYUMLU :
                        (item.farkDurum === 'ACIK' ? FarkDurum.ACIK : FarkDurum.FAZLA),
                    pusulaDokum: item.pusulaDokum || { nakit: 0, krediKarti: 0, paroPuan: 0, mobilOdeme: 0 }
                }));

                // 3. İstatistikler
                this.pompaciNakitToplam = this.farkAnalizi.reduce((sum, item) => sum + (item.pusulaDokum?.nakit || 0), 0);
                this.pompaciSatisTutar = this.farkAnalizi
                    .filter(item => item.personelAdi?.toUpperCase() !== 'OTOMASYON')
                    .reduce((sum, item) => sum + (item.otomasyonToplam || 0), 0);
                this.pompaFark = data.genelOzet.toplamFark;

                if (Math.abs(this.pompaFark) < 10) {
                    this.pompaFarkDurumRenk = 'success';
                } else if (this.pompaFark < 0) {
                    this.pompaFarkDurumRenk = 'danger';
                } else {
                    this.pompaFarkDurumRenk = 'warn';
                }

                // 4. Kredi Kartı Detayları
                this.pusulaKrediKartiDetaylari = (data.krediKartiDetaylari || []).map((item: any) => ({
                    banka: item.banka,
                    tutar: item.tutar,
                    isSpecial: item.banka === 'Paro Puan' || item.banka === 'Mobil Ödeme',
                    showSeparator: false
                }));

                this.toplamKrediKarti = data.genelOzet.toplamKrediKarti + data.genelOzet.toplamParoPuan + data.genelOzet.toplamMobilOdeme;

                this.yukleniyor = false;
                this.detayVisible = true;
            },
            error: (err) => {
                this.yukleniyor = false;
                console.error('Detay yüklenemedi:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Vardiya detayları yüklenemedi.' });
            }
        });
    }

    openKrediKartiDetay() {
        this.activeTab = '1';
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
            [VardiyaDurum.REDDEDILDI]: 'Reddedildi',
            [VardiyaDurum.SILINME_ONAYI_BEKLIYOR]: 'Silinme Onayı Bekliyor',
            [VardiyaDurum.SILINDI]: 'Silindi'
        };
        return labels[durum];
    }

    getDurumSeverity(durum: VardiyaDurum): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
        const severities: Record<VardiyaDurum, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
            [VardiyaDurum.ACIK]: 'warn',
            [VardiyaDurum.ONAY_BEKLIYOR]: 'info',
            [VardiyaDurum.ONAYLANDI]: 'success',
            [VardiyaDurum.REDDEDILDI]: 'danger',
            [VardiyaDurum.SILINME_ONAYI_BEKLIYOR]: 'danger',
            [VardiyaDurum.SILINDI]: 'secondary'
        };
        return severities[durum];
    }

    getBasariOrani(): number {
        if (this.vardiyalar.length === 0) return 0;
        return Math.round((this.getTamamlananSayisi() / this.vardiyalar.length) * 100);
    }


}
