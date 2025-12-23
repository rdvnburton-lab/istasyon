import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TabsModule } from 'primeng/tabs';
import { TextareaModule } from 'primeng/textarea';
import { ConfirmationService, MessageService } from 'primeng/api';
import { VardiyaService } from '../../services/vardiya.service';
import { VardiyaApiService } from '../../services/vardiya-api.service';
import { AuthService } from '../../../../services/auth.service';
import { Vardiya, VardiyaOzet, PersonelFarkAnalizi, MarketOzet, GenelOzet, MarketVardiya, FarkDurum } from '../../models/vardiya.model';

@Component({
    selector: 'app-onay-bekleyenler',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        TagModule,
        TooltipModule,
        DialogModule,
        ConfirmDialogModule,
        TabsModule,
        TextareaModule
    ],
    templateUrl: './onay-bekleyenler.component.html',
    styleUrls: ['./onay-bekleyenler.component.scss'],
    providers: [ConfirmationService, MessageService]
})
export class OnayBekleyenlerComponent implements OnInit {
    vardiyalar: Vardiya[] = [];
    seciliVardiya: Vardiya | null = null;
    detayVisible: boolean = false;

    marketVardiyalar: MarketVardiya[] = [];
    seciliMarketVardiya: MarketVardiya | null = null;
    marketDetayVisible: boolean = false;

    // Detay Verileri
    genelOzet: GenelOzet | null = null;
    farkAnalizi: PersonelFarkAnalizi[] = [];
    marketOzet: MarketOzet | null = null;
    vardiyaOzet: VardiyaOzet | null = null;

    // Reddetme
    redDialogVisible: boolean = false;
    redNedeni: string = '';

    marketRedDialogVisible: boolean = false;
    marketRedNedeni: string = '';

    constructor(
        private vardiyaService: VardiyaService,
        private vardiyaApiService: VardiyaApiService,
        private authService: AuthService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.yukle();
    }

    yukle() {
        this.vardiyaApiService.getOnayBekleyenVardiyalar().subscribe((data: any[]) => {
            this.vardiyalar = data;
        });
        this.vardiyaService.getOnayBekleyenMarketVardiyalar().subscribe((data: MarketVardiya[]) => {
            this.marketVardiyalar = data;
        });
    }

    // Kredi Kartı Detayları
    activeTab: string = '0';
    pusulaKrediKartiDetaylari: { banka: string; tutar: number; showSeparator?: boolean; isSpecial?: boolean }[] = [];
    toplamKrediKarti: number = 0;

    // Yeni eklenen alanlar
    pompaciSatisTutar: number = 0;
    pompaciNakitToplam: number = 0;
    pompaFark: number = 0;
    pompaFarkDurumRenk: 'success' | 'warn' | 'danger' = 'success';

    openKrediKartiDetay() {
        this.activeTab = '1';
    }

    loading: boolean = false;

    incele(vardiya: Vardiya) {
        if (this.loading) return;

        this.loading = true;
        this.seciliVardiya = vardiya;
        this.messageService.add({ severity: 'info', summary: 'Lütfen Bekleyiniz', detail: 'Vardiya detayları yükleniyor...', life: 2000 });

        console.time('⚡ Onay Detay Load');

        // OPTIMIZED: Use new endpoint with server-side aggregation
        this.vardiyaApiService.getOnayDetay(vardiya.id).subscribe({
            next: (data) => {
                console.timeEnd('⚡ Onay Detay Load');
                console.log(`📊 Performance: ${data._performanceMs}ms (server-side)`);

                // 1. Genel Özet - Zaten hesaplanmış geliyor
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

                // 2. Fark Analizi - Zaten hesaplanmış geliyor
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

                // 4. Vardiya Özet
                this.vardiyaOzet = {
                    vardiya: vardiya,
                    pompaOzet: {
                        personelSayisi: data.personelSayisi,
                        toplamOtomasyonSatis: data.genelOzet.pompaToplam,
                        toplamPusulaTahsilat: data.genelOzet.pusulaToplam,
                        toplamFark: this.pompaFark,
                        farkDurum: Math.abs(this.pompaFark) < 10 ? FarkDurum.UYUMLU : (this.pompaFark < 0 ? FarkDurum.ACIK : FarkDurum.FAZLA),
                        personelFarklari: this.farkAnalizi,
                        giderToplam: 0,
                        netTahsilat: data.genelOzet.pusulaToplam
                    },
                    marketOzet: null,
                    genelToplam: data.genelOzet.genelToplam,
                    genelFark: this.pompaFark
                };

                // 5. Kredi Kartı Detayları - Zaten gruplu geliyor
                this.pusulaKrediKartiDetaylari = (data.krediKartiDetaylari || []).map((item: any) => ({
                    banka: item.banka,
                    tutar: item.tutar,
                    isSpecial: item.banka === 'Paro Puan' || item.banka === 'Mobil Ödeme',
                    showSeparator: false
                }));

                // toplamKrediKarti'yi genelOzet'ten al (daha güvenilir)
                this.toplamKrediKarti = data.genelOzet.toplamKrediKarti + data.genelOzet.toplamParoPuan + data.genelOzet.toplamMobilOdeme;

                this.loading = false;
                this.detayVisible = true;
            },
            error: (err) => {
                this.loading = false;
                console.error('❌ Onay detay yüklenemedi:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Vardiya detayları yüklenemedi.' });
            }
        });
    }

    private hesaplaKrediKartiDetaylari(pusulalar: any[]) {
        const bankaMap = new Map<string, number>();
        this.toplamKrediKarti = 0;

        pusulalar.forEach(pusula => {
            if (pusula.krediKartiDetay && Array.isArray(pusula.krediKartiDetay)) {
                pusula.krediKartiDetay.forEach((detay: any) => {
                    const banka = detay.banka || 'Diğer';
                    const tutar = detay.tutar || 0;

                    const mevcut = bankaMap.get(banka) || 0;
                    bankaMap.set(banka, mevcut + tutar);
                    this.toplamKrediKarti += tutar;
                });
            } else if (pusula.krediKarti > 0) {
                const banka = 'Genel / Detaysız';
                const mevcut = bankaMap.get(banka) || 0;
                bankaMap.set(banka, mevcut + pusula.krediKarti);
                this.toplamKrediKarti += pusula.krediKarti;
            }

            if (pusula.paroPuan > 0) {
                const banka = 'Paro Puan';
                const mevcut = bankaMap.get(banka) || 0;
                bankaMap.set(banka, mevcut + pusula.paroPuan);
                this.toplamKrediKarti += pusula.paroPuan;
            }

            if (pusula.mobilOdeme > 0) {
                const banka = 'Mobil Ödeme';
                const mevcut = bankaMap.get(banka) || 0;
                bankaMap.set(banka, mevcut + pusula.mobilOdeme);
                this.toplamKrediKarti += pusula.mobilOdeme;
            }
        });

        const tumKayitlar = Array.from(bankaMap.entries()).map(([banka, tutar]) => {
            const isSpecial = banka === 'Paro Puan' || banka === 'Mobil Ödeme';
            return {
                banka,
                tutar,
                isSpecial,
                showSeparator: false
            };
        });

        const normalKayitlar = tumKayitlar.filter(x => !x.isSpecial).sort((a, b) => b.tutar - a.tutar);
        const ozelKayitlar = tumKayitlar.filter(x => x.isSpecial).sort((a, b) => b.tutar - a.tutar);

        if (ozelKayitlar.length > 0 && normalKayitlar.length > 0) {
            ozelKayitlar[0].showSeparator = true;
        }

        this.pusulaKrediKartiDetaylari = [...normalKayitlar, ...ozelKayitlar];
    }

    onayla(vardiya: Vardiya) {
        this.confirmationService.confirm({
            message: `${vardiya.dosyaAdi} dosyasını ve ilgili vardiya mutabakatını onaylıyor musunuz? Bu işlem geri alınamaz.`,
            header: 'Onay İşlemi',
            icon: 'pi pi-check-circle',
            acceptLabel: 'Evet, Onayla',
            rejectLabel: 'Vazgeç',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                const currentUser = this.authService.getCurrentUser();
                const onaylayanId = currentUser?.id || 0;
                const onaylayanAdi = currentUser ? `${currentUser.ad} ${currentUser.soyad}` : 'Sistem';

                this.vardiyaApiService.vardiyaOnayla(vardiya.id, onaylayanId, onaylayanAdi).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Vardiya başarıyla onaylandı.' });
                        this.detayVisible = false;
                        this.yukle();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Onay işlemi sırasında bir hata oluştu.' });
                    }
                });
            }
        });
    }

    reddetDialogAc(vardiya: Vardiya) {
        this.seciliVardiya = vardiya;
        this.redNedeni = '';
        this.redDialogVisible = true;
    }

    reddetIslemi() {
        if (!this.seciliVardiya) return;
        if (!this.redNedeni.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen bir red nedeni giriniz.' });
            return;
        }

        const currentUser = this.authService.getCurrentUser();
        const onaylayanId = currentUser?.id || 0;
        const onaylayanAdi = currentUser ? `${currentUser.ad} ${currentUser.soyad}` : 'Sistem';

        this.vardiyaApiService.vardiyaReddet(this.seciliVardiya.id, onaylayanId, onaylayanAdi, this.redNedeni).subscribe({
            next: () => {
                this.messageService.add({ severity: 'info', summary: 'Reddedildi', detail: 'Vardiya reddedildi.' });
                this.redDialogVisible = false;
                this.detayVisible = false;
                this.yukle();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Red işlemi başarısız.' });
            }
        });
    }

    marketIncele(vardiya: MarketVardiya) {
        this.seciliMarketVardiya = vardiya;
        this.vardiyaService.getMarketGelirler(vardiya.id).subscribe();
        this.vardiyaService.getMarketOzet().subscribe(ozet => {
            this.marketOzet = ozet;
            this.marketDetayVisible = true;
        });
    }

    marketOnayla(vardiya: MarketVardiya) {
        this.confirmationService.confirm({
            message: `${new Date(vardiya.tarih).toLocaleDateString()} tarihli market vardiyasını onaylıyor musunuz?`,
            header: 'Market Vardiya Onayı',
            icon: 'pi pi-check-circle',
            acceptLabel: 'Evet, Onayla',
            rejectLabel: 'Vazgeç',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.vardiyaService.marketVardiyaOnayla(vardiya.id, 1, 'Yönetici').then(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Market vardiyası onaylandı.' });
                    this.marketDetayVisible = false;
                    this.yukle();
                });
            }
        });
    }

    marketReddetDialogAc(vardiya: MarketVardiya) {
        this.seciliMarketVardiya = vardiya;
        this.marketRedNedeni = '';
        this.marketRedDialogVisible = true;
    }

    marketReddetIslemi() {
        if (!this.seciliMarketVardiya) return;
        if (!this.marketRedNedeni.trim()) {
            this.messageService.add({ severity: 'warn', summary: 'Uyarı', detail: 'Lütfen bir red nedeni giriniz.' });
            return;
        }

        this.vardiyaService.marketVardiyaReddet(this.seciliMarketVardiya.id, this.marketRedNedeni).then(() => {
            this.messageService.add({ severity: 'info', summary: 'Reddedildi', detail: 'Market vardiyası reddedildi.' });
            this.marketRedDialogVisible = false;
            this.marketDetayVisible = false;
            this.yukle();
        });
    }
}
