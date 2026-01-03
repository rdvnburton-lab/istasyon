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
import { PaginatorModule } from 'primeng/paginator';
import { DialogModule } from 'primeng/dialog';
import { ProgressBarModule } from 'primeng/progressbar';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TabsModule } from 'primeng/tabs';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { MessageService, ConfirmationService } from 'primeng/api';

import { VardiyaService } from '../services/vardiya.service';
import { VardiyaApiService } from '../services/vardiya-api.service';
import { VardiyaDurum, OtomasyonSatis, Vardiya, PersonelFarkAnalizi, MarketOzet, GenelOzet, VardiyaOzet, FarkDurum, VardiyaTankEnvanteri } from '../models/vardiya.model';

import { AuthService } from '../../../services/auth.service';
import { SettingsService } from '../../../services/settings.service';

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
        PaginatorModule,
        DialogModule,
        ProgressBarModule,
        ProgressSpinnerModule,
        TabsModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './vardiya-listesi.component.html',
    styleUrls: ['./vardiya-listesi.component.scss']
})
export class VardiyaListesi implements OnInit {
    Math = Math; // For template usage
    vardiyalar: YuklenenVardiya[] = [];

    dosyaDialogVisible = false;
    secilenDosya: File | null = null;
    yukleniyor = false;
    isAutomaticFile: boolean = false;
    currentAutomaticFileId: number | null = null;
    currentAutomaticFileContent: string | null = null;
    otomatikDosyalar: any[] = [];
    autoFirst: number = 0;
    autoRows: number = 6;

    // İlerleme göstergesi için
    islemDurumu = '';
    islemYuzdesi = 0;
    bulunanKayit = 0;
    gecenSure = 0;

    // Progress Tracking
    showGlobalLoading = false;
    processingSteps = [
        'ZIP dosyası açılıyor...',
        'XML içeriği okunuyor...',
        'Satış kayıtları parse ediliyor...',
        'Personel bilgileri işleniyor...',
        'Veritabanına kaydediliyor...',
        'İşlem tamamlanıyor...'
    ];
    currentStep = 0;
    progressInterval: any;

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
    tankEnvanter: VardiyaTankEnvanteri[] = [];
    marketOzet: MarketOzet | null = null;
    vardiyaOzet: VardiyaOzet | null = null;
    activeTab = '0';
    pusulaKrediKartiDetaylari: any[] = [];
    toplamKrediKarti = 0;
    digerOdemelerToplamTutar = 0;
    pompaciSatisTutar = 0;
    pompaCiroGosterim = 0;
    pompaciNakitToplam = 0;
    pompaFark = 0;
    pompaFarkDurumRenk: 'success' | 'warn' | 'danger' = 'success';

    rowsPerPage = 10;

    constructor(
        private vardiyaService: VardiyaService,
        private vardiyaApiService: VardiyaApiService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router,
        private authService: AuthService,
        private settingsService: SettingsService
    ) { }

    ngOnInit(): void {
        this.userRole = this.authService.getCurrentUser()?.role || null;

        // Subscribe to settings
        this.settingsService.settings$.subscribe(settings => {
            if (settings && settings.extraSettingsJson) {
                try {
                    const extra = JSON.parse(settings.extraSettingsJson);
                    if (extra.gorunum && extra.gorunum.satirSayisi) {
                        this.rowsPerPage = extra.gorunum.satirSayisi;
                    }
                } catch (e) {
                    console.error('Error parsing settings in VardiyaListesi', e);
                }
            }
        });

        this.vardiyalariYukle();
        this.otomatikDosyalariYukle();
    }

    onAutoPageChange(event: any) {
        this.autoFirst = event.first;
        this.autoRows = event.rows;
    }

    isNextProcess(file: any): boolean {
        return this.otomatikDosyalar.length > 0 && this.otomatikDosyalar[0].id === file.id;
    }

    parseFileNameInfo(filename: string): any {
        // Format: YYYYMMDDNN.zip or .xml (e.g. 2025052001)
        // 4 chars year, 2 chars month, 2 chars day, 2 chars number
        try {
            const cleanName = filename.replace('.zip', '').replace('.xml', '').replace('.TXT', '').replace('.txt', '');
            if (cleanName.length >= 10) {
                const year = parseInt(cleanName.substring(0, 4));
                const month = parseInt(cleanName.substring(4, 6));
                const day = parseInt(cleanName.substring(6, 8));
                const sequence = parseInt(cleanName.substring(8, 10));

                const date = new Date(year, month - 1, day);
                return { date, sequence, valid: !isNaN(date.getTime()) && !isNaN(sequence) };
            }
        } catch (e) {
            console.error('File name parse error:', filename);
        }
        return { date: new Date(0), sequence: 0, valid: false };
    }

    getVardiyaDate(fileDate: Date): Date {
        // Vardiya tarihi = Dosya tarihi + 1 gün (çünkü vardiya gece başlar)
        const vardiyaDate = new Date(fileDate);
        vardiyaDate.setDate(vardiyaDate.getDate() + 1);
        return vardiyaDate;
    }

    otomatikDosyalariYukle(): void {
        this.vardiyaApiService.getPendingAutomaticFiles().subscribe({
            next: (files) => {
                // Dosya ismine göre sırala (Eskiden Yeniye: YYYYMMDDNN)
                this.otomatikDosyalar = files.sort((a, b) => {
                    const infoA = this.parseFileNameInfo(a.dosyaAdi);
                    const infoB = this.parseFileNameInfo(b.dosyaAdi);

                    if (infoA.valid && infoB.valid) {
                        const dateDiff = infoA.date.getTime() - infoB.date.getTime();
                        if (dateDiff === 0) {
                            return infoA.sequence - infoB.sequence;
                        }
                        return dateDiff;
                    }

                    // Format uymuyorsa yükleme tarihine göre fallback
                    return new Date(a.yuklemeTarihi).getTime() - new Date(b.yuklemeTarihi).getTime();
                });
            },
            error: (err) => console.error('Otomatik dosyalar yüklenemedi:', err)
        });
    }

    otomatikDosyaIsle(file: any): void {
        // Dosya adından tarih ve vardiya sırasını parse et
        const fileInfo = this.parseFileNameInfo(file.dosyaAdi);
        let confirmMessage = `"${file.dosyaAdi}" dosyasını işlemek istediğinizden emin misiniz?`;

        if (fileInfo && fileInfo.valid) {
            // Vardiya tarihi = Dosya tarihi + 1 gün (çünkü vardiya gece başlar)
            const vardiyaTarihi = new Date(fileInfo.date);
            vardiyaTarihi.setDate(vardiyaTarihi.getDate() + 1);

            const tarihStr = vardiyaTarihi.toLocaleDateString('tr-TR', {
                day: '2-digit',
                month: 'long',
                year: 'numeric'
            });

            const vardiyaNo = fileInfo.sequence;

            confirmMessage = `${tarihStr} tarihli ${vardiyaNo}. vardiyayı yüklemek istediğinize emin misiniz?\n\nBu işlem vardiya kaydı oluşturacaktır.`;
        } else {
            confirmMessage += '\n\nBu işlem vardiya kaydı oluşturacaktır.';
        }

        // Onay Dialog Göster
        this.confirmationService.confirm({
            message: confirmMessage,
            header: 'Vardiya Yükleme Onayı',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Yükle',
            rejectLabel: 'İptal',
            accept: () => {
                this.processDosya(file);
            }
        });
    }

    private processDosya(file: any): void {
        // Sıra Uyarısı (Engelleme Yok - Sadece Bilgilendirme)
        if (this.otomatikDosyalar.length > 0 && this.otomatikDosyalar[0].id !== file.id) {
            this.messageService.add({
                severity: 'info',
                summary: 'Bilgi',
                detail: 'Not: En eski tarihli dosyadan başlamanız önerilir, ancak istediğiniz dosyayı işleyebilirsiniz.',
                life: 3000
            });
        }

        // Global Loading Overlay Aktif
        this.showGlobalLoading = true;
        this.yukleniyor = true;
        this.currentStep = 0;
        this.islemDurumu = this.processingSteps[0];

        // Progress Simulation (Her adım ~1.5 saniye)
        this.simulateProgress();

        // Doğrudan backend üzerinden işle (Frontend Parsing YOK)
        this.vardiyaApiService.processAutomaticFile(file.id).subscribe({
            next: (response) => {
                // Progress tamamla
                this.stopProgressSimulation();
                this.currentStep = this.processingSteps.length - 1;
                this.islemDurumu = this.processingSteps[this.currentStep];

                setTimeout(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Dosya başarıyla işlendi.' });

                    // Optimistic Update: Hemen listeden sil
                    this.otomatikDosyalar = this.otomatikDosyalar.filter(d => d.id !== file.id);

                    this.vardiyalariYukle();
                    this.otomatikDosyalariYukle();

                    this.showGlobalLoading = false;
                    this.yukleniyor = false;
                }, 500);
            },
            error: (err) => {
                this.stopProgressSimulation();
                console.error('Dosya işleme hatası:', err);

                const errorDetail = err.error?.message || err.error || 'Dosya işlenirken bir hata oluştu!';
                this.messageService.add({
                    severity: 'error',
                    summary: 'Hata',
                    detail: errorDetail,
                    life: 5000
                });

                this.showGlobalLoading = false;
                this.yukleniyor = false;
            }
        });
    }

    simulateProgress(): void {
        const stepDuration = 1500; // Her adım 1.5 saniye
        this.progressInterval = setInterval(() => {
            if (this.currentStep < this.processingSteps.length - 2) {
                this.currentStep++;
                this.islemDurumu = this.processingSteps[this.currentStep];
                this.islemYuzdesi = Math.round((this.currentStep / this.processingSteps.length) * 100);
            }
        }, stepDuration);
    }

    stopProgressSimulation(): void {
        if (this.progressInterval) {
            clearInterval(this.progressInterval);
            this.progressInterval = null;
        }
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
        this.dosyaDialogVisible = true;
    }

    dosyaDialogKapat(): void {
        this.dosyaDialogVisible = false;
        this.secilenDosya = null;
        this.isAutomaticFile = false;
        this.currentAutomaticFileId = null;
        this.currentAutomaticFileContent = null;
    }

    dosyaSec(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.secilenDosya = input.files[0];

            if (!this.secilenDosya.name.toLowerCase().endsWith('.zip')) {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Sadece ZIP dosyaları desteklenmektedir!' });
                this.secilenDosya = null;
                return;
            }
        }
    }

    dosyaYukle(): void {
        if (!this.secilenDosya) return;

        // Otomatik Dosya İşleme (Zaten Process Edildi)
        if (this.isAutomaticFile) {
            this.dosyaDialogKapat();
            return;
        }

        const dosya = this.secilenDosya; // Referansı sakla

        // Manuel Yükleme - Progress Simulation Başlat
        this.dosyaDialogKapat(); // Dialogu kapat (this.secilenDosya null olur)
        this.showGlobalLoading = true;
        this.yukleniyor = true;
        this.currentStep = 0;
        this.islemDurumu = this.processingSteps[0];

        this.simulateProgress();

        this.vardiyaApiService.uploadVardiyaZip(dosya).subscribe({
            next: (response) => {
                // Progress tamamla
                this.stopProgressSimulation();
                this.currentStep = this.processingSteps.length - 1;
                this.islemDurumu = this.processingSteps[this.currentStep];

                setTimeout(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Dosya başarıyla yüklendi ve işlendi.' });
                    this.vardiyalariYukle();

                    this.showGlobalLoading = false;
                    this.yukleniyor = false;
                }, 500);
            },
            error: (err) => {
                this.stopProgressSimulation();
                console.error('Dosya yükleme hatası:', err);

                const errorDetail = err.error?.message || err.error || 'Dosya yüklenirken bir hata oluştu!';
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: errorDetail, life: 5000 });

                this.showGlobalLoading = false;
                this.yukleniyor = false;
            }
        });
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
        this.router.navigate(['/operasyon/pompa', vardiya.id]);
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
                    // toplamParoPuan ve toplamMobilOdeme removed
                    digerOdemeler: data.genelOzet.digerOdemeler || [],
                    filoToplam: data.genelOzet.filoToplam || 0,
                    toplamGider: 0,
                    toplamVeresiye: data.genelOzet.toplamVeresiye || 0,
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
                    pusulaDokum: item.pusulaDokum || { nakit: 0, krediKarti: 0, digerOdemeler: [] }
                }));

                // 3. İstatistikler
                this.pompaciNakitToplam = this.farkAnalizi.reduce((sum, item) => sum + (item.pusulaDokum?.nakit || 0), 0);
                this.pompaciSatisTutar = this.farkAnalizi
                    .filter(item => item.personelAdi?.toUpperCase() !== 'OTOMASYON')
                    .reduce((sum, item) => sum + (item.otomasyonToplam || 0), 0);
                // Pompa Cirosu = Pompacı Satışları - Filo Satışları
                this.pompaCiroGosterim = this.pompaciSatisTutar - (data.genelOzet.filoToplam || 0);

                this.pompaFark = data.genelOzet.toplamFark;

                if (Math.abs(this.pompaFark) < 10) {
                    this.pompaFarkDurumRenk = 'success';
                } else if (this.pompaFark < 0) {
                    this.pompaFarkDurumRenk = 'danger';
                } else {
                    this.pompaFarkDurumRenk = 'warn';
                }

                // 4. Kredi Kartı Detayları
                // Diğer ödemeleri listeden çıkar (zaten yukarıda özet olarak var)
                const digerOdemeIsimleri = new Set((this.genelOzet?.digerOdemeler || []).map(d => d.turAdi));

                this.pusulaKrediKartiDetaylari = (data.krediKartiDetaylari || [])
                    .filter((item: any) => !digerOdemeIsimleri.has(item.banka))
                    .map((item: any) => ({
                        banka: item.banka,
                        tutar: item.tutar,
                        isSpecial: false,
                        showSeparator: false
                    }));

                this.toplamKrediKarti = data.genelOzet.toplamKrediKarti;
                this.digerOdemelerToplamTutar = (this.genelOzet?.digerOdemeler || []).reduce((acc, item) => acc + item.toplam, 0);

                // 5. Tank Envanter Yükle
                this.vardiyaApiService.getTankEnvanter(vardiya.id).subscribe({
                    next: (tankData) => {
                        this.tankEnvanter = tankData;
                        console.log('Tank Envanter Yüklendi:', this.tankEnvanter);
                    },
                    error: (err) => {
                        console.error('Tank envanter yüklenemedi:', err);
                        this.tankEnvanter = [];
                    }
                });

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
