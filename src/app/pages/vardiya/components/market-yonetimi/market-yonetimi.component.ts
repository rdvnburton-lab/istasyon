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
import {
    Vardiya, // Re-added
    MarketVardiya,
    MarketVardiyaPersonel,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    GiderTuru,
    MarketGelir,
    GelirTuru,
    MarketOzet
} from '../../models/vardiya.model';

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
                this.vardiyaService.marketVardiyaOnayaGonder(vardiya.id).then(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Mutabakat onaya gönderildi' });
                    // Durumu manuel güncelle veya listeyi dinle
                    // List subscription handles update
                }).catch((err: any) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'İşlem sırasında bir hata oluştu' });
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
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.giderTurleri = this.vardiyaService.getGiderTurleri();
        this.gelirTurleri = this.vardiyaService.getGelirTurleri();

        // Market personellerini yükle
        this.vardiyaService.getMarketPersonelleri().subscribe(personeller => {
            this.marketPersonelleri = personeller;
        });

        // Market vardiyalarını dinle
        this.subscriptions.add(
            this.vardiyaService.getMarketVardiyalar().subscribe(list => {
                this.marketVardiyalar = list;
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketGelirler(0).subscribe(list => { // Initial empty or dummy
                this.gelirler = list;
            })
        );

        // Diğer abonelikleri şimdilik pasife alıyoruz veya seçili vardiyaya göre tekrar aktif edeceğiz
        // Mevcut yapıyı korumak için, seciliMarketVardiya değiştiğinde bu verileri güncelleyeceğiz
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

        // Aynı tarih kontrolü
        const secilenTarih = new Date(this.yeniVardiyaForm.tarih);
        secilenTarih.setHours(0, 0, 0, 0);

        const mevcutVardiya = this.marketVardiyalar.find(v => {
            const vTarih = new Date(v.tarih);
            vTarih.setHours(0, 0, 0, 0);
            return vTarih.getTime() === secilenTarih.getTime();
        });

        if (mevcutVardiya) {
            this.messageService.add({
                severity: 'error',
                summary: 'Kayıt Mevcut',
                detail: 'Bu tarih için zaten bir vardiya kaydı bulunmaktadır.'
            });
            return;
        }

        this.vardiyaService.marketVardiyaBaslat({
            tarih: this.yeniVardiyaForm.tarih
        }).subscribe(yeniVardiya => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Yeni market vardiyası oluşturuldu' });
            this.yeniVardiyaForm = { tarih: new Date() };
        });
        this.zRaporu = null;
    }

    isVardiyaSecili(vardiya: any): boolean {
        return this.seciliMarketVardiya?.id === vardiya?.id;
    }

    vardiyaSec(vardiya: MarketVardiya): void {
        this.seciliMarketVardiya = vardiya;
        this.secilenTarih = new Date(vardiya.tarih);

        // Personel Listesini veritabanından çek
        // this.getMarketVardiyaPersonelList(vardiya.id); // Assuming this method will be added later or is a placeholder

        // Z Raporunu veritabanından çek
        // this.getMarketZRaporu(vardiya.id); // Assuming this method will be added later or is a placeholder

        // Giderleri çek
        // this.getMarketGiderleri(vardiya.id); // Assuming this method will be added later or is a placeholder

        // Original logic for loading data, kept for now as helper methods are not defined
        // Personel listesini yükle
        this.vardiyaService.getMarketVardiyaPersonelList(vardiya.id).subscribe(
            list => this.yonetilenPersoneller = list
        );

        // Z Raporunu yükle
        this.vardiyaService.getMarketZRaporu(vardiya.id).subscribe(z => {
            this.zRaporu = z;
            if (z) {
                this.zRaporuForm = {
                    genelToplam: z.genelToplam,
                    kdv0: z.kdv0,
                    kdv1: z.kdv1,
                    kdv10: z.kdv10,
                    kdv20: z.kdv20
                };
            } else {
                this.zRaporuForm = { genelToplam: 0, kdv0: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
            }
        });

        // Gelirleri dinle/yükle
        this.subscriptions.add(
            this.vardiyaService.getMarketGelirler(vardiya.id).subscribe(list => {
                this.gelirler = list;
            })
        );

        this.messageService.add({ severity: 'info', summary: 'Seçildi', detail: 'Vardiya detayları görüntülüyor' });

        // Alt forma scroll ol
        setTimeout(() => {
            const element = document.getElementById('detay-formu');
            if (element) {
                element.scrollIntoView({ behavior: 'smooth' });
            }
        }, 100);
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
    }

    updateOzet(): void {
        this.vardiyaService.getMarketOzet().subscribe((ozet: MarketOzet) => {
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

        this.vardiyaService.marketZRaporuKaydet({
            vardiyaId: this.seciliMarketVardiya.id,
            tarih: this.secilenTarih,
            genelToplam: this.zRaporuForm.genelToplam,
            kdv0: this.zRaporuForm.kdv0,
            kdv1: this.zRaporuForm.kdv1,
            kdv10: this.zRaporuForm.kdv10,
            kdv20: this.zRaporuForm.kdv20
        }).subscribe(z => {
            this.zRaporu = z;
            this.zRaporuDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Z Raporu güncellendi' });
        });
    }

    // Personel İşlem (Satış + Tahsilat) Kaydet
    personelIslemKaydet(): void {
        if (!this.seciliMarketVardiya || !this.personelIslemForm.personelId) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen personel seçin' });
            return;
        }

        const personel = this.marketPersonelleri.find(p => p.value === this.personelIslemForm.personelId);

        this.vardiyaService.marketPersonelIslemKaydet({
            vardiyaId: this.seciliMarketVardiya.id,
            personelId: this.personelIslemForm.personelId,
            personelAdi: personel?.label || 'Bilinmiyor',
            sistemSatisTutari: this.personelIslemForm.sistemSatisTutari,
            nakit: this.personelIslemForm.nakit,
            krediKarti: this.personelIslemForm.krediKarti,
            gider: this.personelIslemForm.gider
        }).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Personel verisi kaydedildi' });
            // Formu sıfırla veya yeni kayıt için hazırla
            this.personelIslemForm = {
                personelId: null,
                sistemSatisTutari: 0,
                nakit: 0,
                krediKarti: 0,
                gider: 0
            };
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

        this.vardiyaService.marketGiderEkle({
            vardiyaId: this.seciliMarketVardiya.id,
            tarih: this.secilenTarih,
            giderTuru: this.giderForm.giderTuru,
            tutar: this.giderForm.tutar,
            aciklama: this.giderForm.aciklama || this.getGiderTuruLabel(this.giderForm.giderTuru)
        }).subscribe(() => {
            this.giderDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
        });
    }

    giderSil(giderId: number): void {
        this.vardiyaService.marketGiderSil(giderId).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
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

        this.vardiyaService.marketGelirEkle({
            vardiyaId: this.seciliMarketVardiya.id,
            tarih: this.secilenTarih,
            gelirTuru: this.gelirForm.gelirTuru,
            tutar: this.gelirForm.tutar,
            aciklama: this.gelirForm.aciklama || this.getGelirTuruLabel(this.gelirForm.gelirTuru)
        }).subscribe(() => {
            this.gelirDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir eklendi' });
        });
    }

    gelirSil(gelirId: number): void {
        this.vardiyaService.marketGelirSil(gelirId).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir silindi' });
        });
    }

    getGelirTuruLabel(turu: GelirTuru): string {
        const found = this.gelirTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    getGelirToplam(): number {
        return this.gelirler.reduce((sum, g) => sum + g.tutar, 0);
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
}
