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
import { ToastModule } from 'primeng/toast';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { PanelModule } from 'primeng/panel';

import { MessageService } from 'primeng/api';

import { VardiyaService } from '../../services/vardiya.service';
import {
    Vardiya,
    MarketZRaporu,
    MarketTahsilat,
    MarketGider,
    GiderTuru,
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
        ToastModule,
        SelectModule,
        MessageModule,
        PanelModule
    ],
    providers: [MessageService],
    templateUrl: './market-yonetimi.component.html',
    styleUrls: ['./market-yonetimi.component.scss']
})
export class MarketYonetimi implements OnInit, OnDestroy {
    aktifVardiya: Vardiya | null = null;
    zRaporu: MarketZRaporu | null = null;
    tahsilat: MarketTahsilat | null = null;
    giderler: MarketGider[] = [];
    marketOzet: MarketOzet | null = null;

    zRaporuForm = { genelToplam: 0, kdv0: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
    tahsilatForm = { nakit: 0, krediKarti: 0 };

    kdvKontrolSonuc = { gecerli: false, mesaj: 'Z Raporu bilgilerini girin', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };

    giderDialogVisible = false;
    giderTurleri: { label: string; value: GiderTuru }[] = [];
    giderForm = { giderTuru: null as GiderTuru | null, tutar: 0, aciklama: '' };

    private subscriptions = new Subscription();

    constructor(
        private vardiyaService: VardiyaService,
        private messageService: MessageService,
        private router: Router
    ) { }

    ngOnInit(): void {
        this.giderTurleri = this.vardiyaService.getGiderTurleri();

        this.subscriptions.add(
            this.vardiyaService.getAktifVardiya().subscribe(vardiya => {
                if (vardiya) {
                    this.aktifVardiya = vardiya;
                } else {
                    // Pompa dosyası yüklenmeden market mutabakatı yapılabilmesi için
                    // geçici bir market vardiyası oluştur
                    this.aktifVardiya = {
                        id: Date.now(),
                        istasyonId: 1,
                        istasyonAdi: 'Market Mutabakatı',
                        sorumluId: 1,
                        sorumluAdi: 'Market Sorumlusu',
                        baslangicTarihi: new Date(),
                        durum: 'ACIK' as any,
                        pompaToplam: 0,
                        marketToplam: 0,
                        genelToplam: 0,
                        toplamFark: 0,
                        olusturmaTarihi: new Date(),
                        kilitli: false
                    };
                }
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketZRaporu().subscribe((zRaporu: MarketZRaporu | null) => {
                this.zRaporu = zRaporu;
                if (zRaporu) {
                    this.zRaporuForm = {
                        genelToplam: zRaporu.genelToplam,
                        kdv0: zRaporu.kdv0,
                        kdv1: zRaporu.kdv1,
                        kdv10: zRaporu.kdv10,
                        kdv20: zRaporu.kdv20
                    };
                    this.kdvKontrol();
                }
                this.updateOzet();
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketTahsilat().subscribe((tahsilat: MarketTahsilat | null) => {
                this.tahsilat = tahsilat;
                if (tahsilat) {
                    this.tahsilatForm = {
                        nakit: tahsilat.nakit,
                        krediKarti: tahsilat.krediKarti
                    };
                }
                this.updateOzet();
            })
        );

        this.subscriptions.add(
            this.vardiyaService.getMarketGiderler().subscribe((giderler: MarketGider[]) => {
                this.giderler = giderler;
                this.updateOzet();
            })
        );
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
        return (this.tahsilatForm.nakit || 0) + (this.tahsilatForm.krediKarti || 0);
    }

    getGiderToplam(): number {
        return this.giderler.reduce((sum, g) => sum + g.tutar, 0);
    }

    // Kayıt işlemleri
    zRaporuKaydet(): void {
        if (!this.aktifVardiya) return;

        this.vardiyaService.marketZRaporuKaydet({
            vardiyaId: this.aktifVardiya.id,
            genelToplam: this.zRaporuForm.genelToplam,
            kdv0: this.zRaporuForm.kdv0,
            kdv1: this.zRaporuForm.kdv1,
            kdv10: this.zRaporuForm.kdv10,
            kdv20: this.zRaporuForm.kdv20
        }).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Z Raporu kaydedildi' });
        });
    }

    tahsilatKaydet(): void {
        if (!this.aktifVardiya) return;

        this.vardiyaService.marketTahsilatKaydet({
            vardiyaId: this.aktifVardiya.id,
            nakit: this.tahsilatForm.nakit,
            krediKarti: this.tahsilatForm.krediKarti
        }).subscribe(() => {
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Tahsilat kaydedildi' });
        });
    }

    // Gider işlemleri
    giderDialogAc(): void {
        this.giderForm = { giderTuru: null, tutar: 0, aciklama: '' };
        this.giderDialogVisible = true;
    }

    giderEkle(): void {
        if (!this.giderForm.giderTuru || !this.giderForm.tutar || !this.aktifVardiya) return;

        this.vardiyaService.marketGiderEkle({
            vardiyaId: this.aktifVardiya.id,
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
