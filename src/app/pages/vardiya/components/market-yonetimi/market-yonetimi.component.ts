import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';

import { MessageService, ConfirmationService } from 'primeng/api';

import { VardiyaService } from '../../services/vardiya.service';
import { MarketApiService } from '../../services/market-api.service';
import {
    Vardiya, // Re-added
    MarketVardiya,
    MarketVardiyaPersonel,
    PersonelRol,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    GiderTuru,
    MarketGelir,
    GelirTuru,
    MarketOzet
} from '../../models/vardiya.model';
import { PersonelApiService, Personel } from '../../../../services/personel-api.service';
import { AuthService, User } from '../../../../services/auth.service';

@Component({
    selector: 'app-market-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        ButtonModule,
        CardModule,
        TableModule,
        TagModule,
        DividerModule,
        InputNumberModule,
        TextareaModule,
        DialogModule,
        ConfirmDialogModule,
        ToastModule,
        SelectModule,
        MessageModule,
        PanelModule,
        DatePickerModule,
        TooltipModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './market-yonetimi.component.html',
    styleUrls: ['./market-yonetimi.component.scss']
})
export class MarketYonetimi implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    zRaporu: MarketZRaporu | null = null;
    tahsilat: MarketTahsilat | null = null;
    giderler: MarketGider[] = [];
    gelirler: MarketGelir[] = [];
    marketOzet: MarketOzet | null = null;
    secilenTarih: Date = new Date(); // Varsayılan olarak bugün
    mutabakatBaslatildi: boolean = false; // Mutabakat başlatıldı mı?
    marketVardiyalar: MarketVardiya[] = [] as MarketVardiya[];
    seciliMarketVardiya: MarketVardiya | null = null;
    yonetilenPersoneller: MarketVardiyaPersonel[] = [];

    // Yeni Vardiya Formu
    yeniVardiyaForm = {
        tarih: new Date()
    };

    marketPersonelleri: { label: string; value: number }[] = [];

    currentUser: User | null = null;
    redDialogVisible: boolean = false;
    redNedeni: string = '';
    reddedilecekVardiyaId: number | null = null;

    mutabakatBaslat(): void {
        if (!this.secilenTarih) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Uyarı',
                detail: 'Lütfen önce bir tarih seçin'
            });
            return;
        }
        this.mutabakatBaslatildi = true;
        this.messageService.add({
            severity: 'success',
            summary: 'Başarılı',
            detail: `${this.secilenTarih.toLocaleDateString('tr-TR')} tarihi için mutabakat başlatıldı`
        });
    }

    mutabakatGonder(vardiya: MarketVardiya): void {
        this.confirmationService.confirm({
            message: 'Bu vardiya mutabakatını onaya göndermek istediğinize emin misiniz?',
            header: 'Onay Gönderimi',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Gönder',
            rejectLabel: 'Vazgeç',
            accept: () => {
                this.marketApiService.onayaGonder(vardiya.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Mutabakat onaya gönderildi' });
                        this.loadMarketVardiyalar();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'İşlem sırasında bir hata oluştu' });
                    }
                });
            }
        });
    }

    zRaporuForm = { genelToplam: 0, kdv0: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
    personelIslemForm = {
        personelId: null as number | null,
        sistemSatisTutari: 0,
        nakit: 0,
        krediKarti: 0,
        gider: 0
    };
    kdvKontrolSonuc = { gecerli: false, mesaj: 'Z Raporu bilgilerini girin', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };

    giderDialogVisible = false;
    giderTurleri: { label: string; value: GiderTuru }[] = [];
    giderForm = { giderTuru: null as GiderTuru | null, tutar: 0, aciklama: '' };

    gelirDialogVisible = false;
    gelirTurleri: { label: string; value: GelirTuru }[] = [];
    gelirForm = { gelirTuru: null as GelirTuru | null, tutar: 0, aciklama: '' };

    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private marketApiService: MarketApiService,
        private personelApiService: PersonelApiService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router,
        private authService: AuthService
    ) { }

    ngOnInit(): void {
        this.giderTurleri = this.vardiyaService.getGiderTurleri();
        this.gelirTurleri = this.vardiyaService.getGelirTurleri();

        this.authService.currentUser$.subscribe(user => {
            this.currentUser = user;
        });

        // Market personellerini yükle (API üzerinden)
        this.personelApiService.getAll().subscribe((personeller: Personel[]) => {
            this.marketPersonelleri = personeller
                .filter((p: Personel) => p.rol === 'MARKET_GOREVLISI' || p.rol === 'MARKET_SORUMLUSU')
                .map((p: Personel) => ({
                    label: p.adSoyad,
                    value: p.id!
                }));
        });

        this.loadMarketVardiyalar();
    }

    loadMarketVardiyalar(): void {
        this.marketApiService.getMarketVardiyalar().subscribe(list => {
            this.marketVardiyalar = list;
        });
    }

    listeyeDon(): void {
        this.seciliMarketVardiya = null;
        this.yonetilenPersoneller = [];
        this.zRaporu = null;
        this.giderler = [];
        this.gelirler = [];
        this.personelIslemForm = {
            personelId: null,
            sistemSatisTutari: 0,
            nakit: 0,
            krediKarti: 0,
            gider: 0
        };
    }

    yeniVardiyaEkle(): void {
        if (!this.yeniVardiyaForm.tarih) {
            this.messageService.add({ severity: 'warn', summary: 'Eksik Bilgi', detail: 'Lütfen tarih seçin' });
            return;
        }

        // Aynı tarihte vardiya var mı kontrol et
        const secilenTarihStr = this.yeniVardiyaForm.tarih.toDateString();
        const mevcut = this.marketVardiyalar.some(v => new Date(v.tarih).toDateString() === secilenTarihStr);
        if (mevcut) {
            this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Bu tarih için zaten bir vardiya mevcut!' });
            return;
        }

        this.marketApiService.createMarketVardiya({
            tarih: this.yeniVardiyaForm.tarih
        }).subscribe({
            next: (response) => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Yeni market vardiyası oluşturuldu' });
                this.loadMarketVardiyalar();
                this.yeniVardiyaForm = { tarih: new Date() };
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error?.message || 'Vardiya oluşturulamadı' });
            }
        });
    }

    isVardiyaSecili(vardiya: any): boolean {
        return this.seciliMarketVardiya?.id === vardiya?.id;
    }

    vardiyaSec(vardiya: MarketVardiya): void {
        this.marketApiService.getMarketVardiyaDetay(vardiya.id).subscribe(data => {
            this.seciliMarketVardiya = data;
            this.secilenTarih = new Date(data.tarih);

            this.yonetilenPersoneller = data.tahsilatlar.map((t: any) => ({
                id: t.id,
                vardiyaId: t.marketVardiyaId,
                personelId: t.personelId,
                personelAdi: t.personel?.adSoyad || t.personel?.otomasyonAdi || 'Bilinmiyor',
                sistemSatisTutari: t.sistemSatisTutari || 0,
                nakit: t.nakit,
                krediKarti: t.krediKarti,
                gider: 0,
                toplamTeslimat: t.toplam,
                fark: (t.sistemSatisTutari || 0) - t.toplam,
                olusturmaTarihi: t.olusturmaTarihi
            }));

            if (data.zRaporlari && data.zRaporlari.length > 0) {
                const z = data.zRaporlari[0];
                this.zRaporu = z;
                this.zRaporuForm = {
                    genelToplam: z.genelToplam,
                    kdv0: z.kdv0,
                    kdv1: z.kdv1,
                    kdv10: z.kdv10,
                    kdv20: z.kdv20
                };
            } else {
                this.zRaporu = null;
                this.zRaporuForm = { genelToplam: 0, kdv0: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
            }

            this.giderler = data.giderler;
            this.gelirler = data.gelirler;

            this.marketOzet = {
                zRaporuToplam: data.toplamSatisTutari,
                tahsilatToplam: data.tahsilatlar.reduce((sum: number, t: any) => sum + t.toplam, 0),
                giderToplam: data.giderler.reduce((sum: number, g: any) => sum + g.tutar, 0),
                gelirToplam: data.gelirler.reduce((sum: number, g: any) => sum + g.tutar, 0),
                netKasa: data.toplamTeslimatTutari,
                fark: data.toplamFark,
                tahsilatNakit: data.tahsilatlar.reduce((sum: number, t: any) => sum + t.nakit, 0),
                tahsilatKrediKarti: data.tahsilatlar.reduce((sum: number, t: any) => sum + t.krediKarti, 0),
                tahsilatParoPuan: data.tahsilatlar.reduce((sum: number, t: any) => sum + t.paroPuan, 0),
                kdvDokum: {
                    kdv0: this.zRaporu?.kdv0 || 0,
                    kdv1: this.zRaporu?.kdv1 || 0,
                    kdv10: this.zRaporu?.kdv10 || 0,
                    kdv20: this.zRaporu?.kdv20 || 0,
                    toplam: (this.zRaporu?.kdv0 || 0) + (this.zRaporu?.kdv1 || 0) + (this.zRaporu?.kdv10 || 0) + (this.zRaporu?.kdv20 || 0)
                }
            };

            this.messageService.add({ severity: 'info', summary: 'Seçildi', detail: 'Vardiya detayları yüklendi' });

            setTimeout(() => {
                const element = document.getElementById('detay-formu');
                if (element) {
                    element.scrollIntoView({ behavior: 'smooth' });
                }
            }, 100);
        });
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    updateOzet(): void {
        this.vardiyaService.getMarketOzet().subscribe((ozet: MarketOzet | null) => {
            this.marketOzet = ozet;
        });
    }

    // KDV Kontrol
    kdvKontrol(): void {
        const genelToplam = this.zRaporuForm.genelToplam || 0;
        const kdvToplam = this.getKdvToplam();

        if (genelToplam === 0) {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'Genel toplam giriniz', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };
            return;
        }

        // Basit kontrol: KDV toplamı genel toplamdan küçük olmalı ve mantıklı bir aralıkta olmalı
        const kdvOrani = (kdvToplam / genelToplam) * 100;

        if (kdvToplam === 0) {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'KDV kırılımlarını giriniz', sinif: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700', icon: 'pi-exclamation-triangle' };
        } else if (kdvOrani > 0 && kdvOrani <= 25) {
            this.kdvKontrolSonuc = { gecerli: true, mesaj: 'KDV oranları uygun', sinif: 'bg-green-100 dark:bg-green-900/30 text-green-700', icon: 'pi-check-circle' };
        } else {
            this.kdvKontrolSonuc = { gecerli: false, mesaj: 'KDV oranları tutarsız görünüyor', sinif: 'bg-red-100 dark:bg-red-900/30 text-red-700', icon: 'pi-times-circle' };
        }
    }

    getKdvToplam(): number {
        return (this.zRaporuForm.kdv0 || 0) + (this.zRaporuForm.kdv1 || 0) + (this.zRaporuForm.kdv10 || 0) + (this.zRaporuForm.kdv20 || 0);
    }

    getKdvHaric(): number {
        return (this.zRaporuForm.genelToplam || 0) - this.getKdvToplam();
    }

    getTahsilatToplam(): number {
        return (this.personelIslemForm.nakit || 0) + (this.personelIslemForm.krediKarti || 0);
    }

    getGiderToplam(): number {
        return this.giderler.reduce((sum, g) => sum + g.tutar, 0);
    }

    // Kayıt işlemleri
    zRaporuDialogVisible = false;

    zRaporuDialogAc(vardiya?: MarketVardiya): void {
        if (vardiya) {
            this.vardiyaSec(vardiya);
        }
        this.zRaporuDialogVisible = true;
    }

    zRaporuKaydet(): void {
        if (!this.seciliMarketVardiya) return;

        const data = {
            genelToplam: this.zRaporuForm.genelToplam,
            kdv0: this.zRaporuForm.kdv0,
            kdv1: this.zRaporuForm.kdv1,
            kdv10: this.zRaporuForm.kdv10,
            kdv20: this.zRaporuForm.kdv20,
            kdvToplam: this.getKdvToplam(),
            kdvHaricToplam: this.getKdvHaric()
        };

        this.marketApiService.saveZRaporu(this.seciliMarketVardiya.id, data).subscribe(z => {
            this.zRaporu = z;
            this.zRaporuDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Z Raporu güncellendi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    // Personel İşlem (Satış + Tahsilat) Kaydet
    personelIslemKaydet(): void {
        if (!this.seciliMarketVardiya || !this.personelIslemForm.personelId) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen personel seçin' });
            return;
        }

        const data = {
            personelId: this.personelIslemForm.personelId,
            sistemSatisTutari: this.personelIslemForm.sistemSatisTutari,
            nakit: this.personelIslemForm.nakit,
            krediKarti: this.personelIslemForm.krediKarti,
            paroPuan: 0, // Şimdilik 0
            toplam: this.getPersonelIslemToplam(),
            aciklama: ''
        };

        this.marketApiService.saveTahsilat(this.seciliMarketVardiya.id, data).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel verisi kaydedildi' });
            this.personelIslemForm = {
                personelId: null,
                sistemSatisTutari: 0,
                nakit: 0,
                krediKarti: 0,
                gider: 0
            };
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    // Düzenlemek için personel seç
    personelDuzenle(kayit: MarketVardiyaPersonel): void {
        this.personelIslemForm = {
            personelId: kayit.personelId,
            sistemSatisTutari: kayit.sistemSatisTutari,
            nakit: kayit.nakit,
            krediKarti: kayit.krediKarti,
            gider: kayit.gider || 0
        };
    }

    getPersonelIslemToplam(): number {
        return (this.personelIslemForm.nakit || 0) + (this.personelIslemForm.krediKarti || 0) + (this.personelIslemForm.gider || 0);
    }

    getPersonelFark(): number {
        return this.getPersonelIslemToplam() - (this.personelIslemForm.sistemSatisTutari || 0);
    }

    // Gider işlemleri
    giderDialogAc(): void {
        this.giderForm = { giderTuru: null, tutar: 0, aciklama: '' };
        this.giderDialogVisible = true;
    }

    giderEkle(): void {
        if (!this.giderForm.giderTuru || !this.giderForm.tutar || !this.seciliMarketVardiya) return;

        this.marketApiService.addGider(this.seciliMarketVardiya.id, {
            giderTuru: this.giderForm.giderTuru,
            tutar: this.giderForm.tutar,
            aciklama: this.giderForm.aciklama || this.getGiderTuruLabel(this.giderForm.giderTuru)
        }).subscribe(() => {
            this.giderDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    giderSil(giderId: number): void {
        this.marketApiService.deleteGider(giderId).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    getGiderTuruLabel(turu: GiderTuru): string {
        const found = this.giderTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    // Gelir İşlemleri
    gelirDialogAc(): void {
        this.gelirForm = { gelirTuru: null, tutar: 0, aciklama: '' };
        this.gelirDialogVisible = true;
    }

    gelirEkle(): void {
        if (!this.gelirForm.gelirTuru || !this.gelirForm.tutar || !this.seciliMarketVardiya) return;

        this.marketApiService.addGelir(this.seciliMarketVardiya.id, {
            gelirTuru: this.gelirForm.gelirTuru,
            tutar: this.gelirForm.tutar,
            aciklama: this.gelirForm.aciklama || this.getGelirTuruLabel(this.gelirForm.gelirTuru)
        }).subscribe(() => {
            this.gelirDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir eklendi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    gelirSil(gelirId: number): void {
        this.marketApiService.deleteGelir(gelirId).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir silindi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    getGelirTuruLabel(turu: GelirTuru): string {
        const found = this.gelirTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    getGelirToplam(): number {
        return this.gelirler.reduce((sum, g) => sum + g.tutar, 0);
    }

    getDurumLabel(durum: string): string {
        switch (durum) {
            case 'ACIK': return 'Açık';
            case 'ONAY_BEKLIYOR': return 'Onay Bekliyor';
            case 'ONAYLANDI': return 'Onaylandı';
            case 'REDDEDILDI': return 'Reddedildi';
            case 'SILINME_ONAYI_BEKLIYOR': return 'Silinme Onayı Bekliyor';
            case 'SILINDI': return 'Silindi';
            default: return durum;
        }
    }

    getDurumSeverity(durum: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' | undefined {
        switch (durum) {
            case 'ACIK': return 'info';
            case 'ONAY_BEKLIYOR': return 'warn';
            case 'ONAYLANDI': return 'success';
            case 'REDDEDILDI': return 'danger';
            case 'SILINME_ONAYI_BEKLIYOR': return 'warn';
            case 'SILINDI': return 'danger';
            default: return 'secondary';
        }
    }

    getFarkCardClass(): string {
        if (!this.marketOzet) return 'bg-gradient-to-br from-gray-500 to-gray-600';
        if (Math.abs(this.marketOzet.fark) < 1) return 'bg-gradient-to-br from-green-500 to-green-600';
        if (this.marketOzet.fark < 0) return 'bg-gradient-to-br from-red-500 to-red-600';
        return 'bg-gradient-to-br from-blue-500 to-blue-600';
    }

    // Yeni stil fonksiyonları
    getFarkBackground(): string {
        if (!this.marketOzet) return 'linear-gradient(135deg, #6b7280 0%, #4b5563 100%)';
        if (Math.abs(this.marketOzet.fark) < 1) return 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)';
        if (this.marketOzet.fark < 0) return 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)';
        return 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)';
    }

    getFarkShadow(): string {
        if (!this.marketOzet) return '0 10px 40px rgba(107, 114, 128, 0.3)';
        if (Math.abs(this.marketOzet.fark) < 1) return '0 10px 40px rgba(34, 197, 94, 0.3)';
        if (this.marketOzet.fark < 0) return '0 10px 40px rgba(239, 68, 68, 0.3)';
        return '0 10px 40px rgba(59, 130, 246, 0.3)';
    }

    getFarkIcon(): string {
        if (!this.marketOzet) return 'pi-chart-line';
        if (Math.abs(this.marketOzet.fark) < 1) return 'pi-check-circle';
        if (this.marketOzet.fark < 0) return 'pi-exclamation-triangle';
        return 'pi-arrow-up';
    }

    getFarkAciklama(): string {
        if (!this.marketOzet) return 'Veri bekleniyor';
        if (Math.abs(this.marketOzet.fark) < 1) return 'Mutabakat tamam';
        if (this.marketOzet.fark < 0) return 'Kasa açığı';
        return 'Kasa fazlası';
    }

    getMutabakatBackground(): string {
        if (this.zRaporu && this.tahsilat) {
            return 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)';
        }
        return 'linear-gradient(135deg, #f59e0b 0%, #ea580c 100%)';
    }

    // Onay İşlemleri
    onayla(vardiya: MarketVardiya): void {
        this.confirmationService.confirm({
            message: 'Bu vardiyayı onaylamak istediğinize emin misiniz?',
            header: 'Onay',
            icon: 'pi pi-check-circle',
            acceptLabel: 'Evet, Onayla',
            rejectLabel: 'Vazgeç',
            accept: () => {
                this.marketApiService.onayla(vardiya.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Vardiya onaylandı' });
                        this.loadMarketVardiyalar();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Onay işlemi başarısız' });
                    }
                });
            }
        });
    }

    reddetDialogAc(vardiya: MarketVardiya): void {
        this.reddedilecekVardiyaId = vardiya.id;
        this.redNedeni = '';
        this.redDialogVisible = true;
    }

    reddetOnayla(): void {
        if (!this.reddedilecekVardiyaId || !this.redNedeni.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen ret nedeni giriniz' });
            return;
        }

        this.marketApiService.reddet(this.reddedilecekVardiyaId, this.redNedeni).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Vardiya reddedildi' });
                this.redDialogVisible = false;
                this.loadMarketVardiyalar();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Ret işlemi başarısız' });
            }
        });
    }
}
