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
import { SelectButtonModule } from 'primeng/selectbutton';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ConfirmationService, MessageService } from 'primeng/api';
import { VardiyaService } from '../../operasyon/services/vardiya.service';
import { VardiyaApiService } from '../../operasyon/services/vardiya-api.service';
import { MarketApiService } from '../../operasyon/services/market-api.service';
import { AuthService } from '../../../services/auth.service';
import { Vardiya, VardiyaOzet, PersonelFarkAnalizi, MarketOzet, GenelOzet, MarketVardiya, FarkDurum, VardiyaTankEnvanteri } from '../../operasyon/models/vardiya.model';

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
        SelectButtonModule,
        TextareaModule,
        ToastModule,
        IconFieldModule,
        InputIconModule,
        InputTextModule
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

    // Silinme Talepleri
    silinmeTalepleri: Vardiya[] = [];

    // Görünüm Kontrolü (Tabs Replacement)
    activeView: 'pompa' | 'market' | 'silinme' = 'pompa';
    viewOptions = [
        { label: 'Pompa Vardiyaları', value: 'pompa', icon: 'pi pi-briefcase' },
        { label: 'Market Vardiyaları', value: 'market', icon: 'pi pi-shopping-cart' },
        { label: 'Silinme Talepleri', value: 'silinme', icon: 'pi pi-trash' }
    ];

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
        private marketApiService: MarketApiService,
        private authService: AuthService,
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.yukle();
    }


    yukle() {
        this.vardiyaApiService.getOnayBekleyenVardiyalar().subscribe((data: any[]) => {
            // Tüm vardiyaları al ve ayır
            this.vardiyalar = data.filter(v => v.durum !== 'SILINME_ONAYI_BEKLIYOR');
            this.silinmeTalepleri = data.filter(v => v.durum === 'SILINME_ONAYI_BEKLIYOR');

            // Market verilerini de çek ve sonra başlıkları güncelle
            this.marketApiService.getMarketVardiyalar().subscribe((marketData: any[]) => {
                this.marketVardiyalar = marketData.filter(v => v.durum === 'ONAY_BEKLIYOR');
                this.updateViewOptions();
            });
        });
    }

    updateViewOptions() {
        this.viewOptions = [
            { label: `Pompa Vardiyaları (${this.vardiyalar.length})`, value: 'pompa', icon: 'pi pi-briefcase' },
            { label: `Market Vardiyaları (${this.marketVardiyalar.length})`, value: 'market', icon: 'pi pi-shopping-cart' },
            { label: `Silinme Talepleri (${this.silinmeTalepleri.length})`, value: 'silinme', icon: 'pi pi-trash' }
        ];
    }

    // Kredi Kartı Detayları
    activeTab: string = '0'; // Varsayılan: Tahsilat Detayları
    pusulaKrediKartiDetaylari: { banka: string; tutar: number; showSeparator?: boolean; isSpecial?: boolean }[] = [];
    toplamKrediKarti: number = 0;
    digerOdemelerToplamTutar: number = 0;

    // Yeni eklenen alanlar
    pompaCiroGosterim: number = 0;
    pompaciSatisTutar: number = 0;
    pompaciNakitToplam: number = 0;
    pompaFark: number = 0;
    pompaFarkDurumRenk: 'success' | 'warn' | 'danger' = 'success';

    // Tank Envanter
    tankEnvanter: VardiyaTankEnvanteri[] = [];
    Math = Math; // For template usage

    openKrediKartiDetay() {
        this.activeTab = '0';
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
                    digerOdemeler: data.genelOzet.digerOdemeler || [],
                    filoToplam: data.genelOzet.filoToplam || 0,
                    genelToplam: data.genelOzet.genelToplam,
                    toplamNakit: data.genelOzet.toplamNakit,
                    toplamKrediKarti: data.genelOzet.toplamKrediKarti,

                    toplamGider: 0,
                    toplamVeresiye: data.genelOzet.toplamVeresiye || 0,
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

                // toplamKrediKarti'yi genelOzet'ten al (daha güvenilir)
                // toplamKrediKarti'yi genelOzet'ten al (daha güvenilir)
                this.toplamKrediKarti = data.genelOzet.toplamKrediKarti;
                this.digerOdemelerToplamTutar = (data.genelOzet.digerOdemeler || []).reduce((acc: number, item: any) => acc + item.toplam, 0);

                // 6. Tank Envanter Verilerini Yükle
                this.vardiyaApiService.getTankEnvanter(vardiya.id).subscribe({
                    next: (tankData) => {
                        this.tankEnvanter = tankData;
                        this.loading = false;
                        this.detayVisible = true;
                    },
                    error: (tankErr) => {
                        console.warn('Tank envanter yüklenemedi:', tankErr);
                        this.tankEnvanter = [];
                        this.loading = false;
                        this.detayVisible = true;
                    }
                });
            },
            error: (err) => {
                this.loading = false;
                console.error('❌ Onay detay yüklenemedi:', err);
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Vardiya detayları yüklenemedi.' });
            }
        });
    }

    // hesaplaKrediKartiDetaylari removed as it is handled backend side or redundant

    onayla(vardiya: Vardiya) {
        const isDeletion = vardiya.durum === 'SILINME_ONAYI_BEKLIYOR';
        const message = isDeletion
            ? `${vardiya.dosyaAdi} dosyasını silmeyi onaylıyor musunuz? Bu işlem geri alınamaz.`
            : `${vardiya.dosyaAdi} dosyasını ve ilgili vardiya mutabakatını onaylıyor musunuz? Bu işlem geri alınamaz.`;

        const header = isDeletion ? 'Silme Onayı' : 'Onay İşlemi';
        const icon = isDeletion ? 'pi pi-trash' : 'pi pi-check-circle';
        const acceptButtonStyleClass = isDeletion ? 'p-button-danger' : 'p-button-success';

        this.confirmationService.confirm({
            message: message,
            header: header,
            icon: icon,
            acceptLabel: isDeletion ? 'Sil' : 'Evet, Onayla',
            rejectLabel: 'Vazgeç',
            acceptButtonStyleClass: acceptButtonStyleClass,
            accept: () => {
                const currentUser = this.authService.getCurrentUser();
                const onaylayanId = currentUser?.id || 1; // Token'dan ID alınıyor
                const onaylayanAdi = currentUser ? currentUser.username : 'Sistem';

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
        const onaylayanId = currentUser?.id || 1; // Token'dan ID alınıyor
        const onaylayanAdi = currentUser ? currentUser.username : 'Sistem';

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
        this.marketApiService.getMarketVardiyaDetay(vardiya.id).subscribe(data => {
            this.seciliMarketVardiya = data;

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
                    kdv0: data.zRaporlari?.[0]?.kdv0 || 0,
                    kdv1: data.zRaporlari?.[0]?.kdv1 || 0,
                    kdv10: data.zRaporlari?.[0]?.kdv10 || 0,
                    kdv20: data.zRaporlari?.[0]?.kdv20 || 0,
                    toplam: data.zRaporlari?.[0]?.kdvToplam || 0
                }
            };
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
                this.marketApiService.onayla(vardiya.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Market vardiyası onaylandı.' });
                        this.marketDetayVisible = false;
                        this.yukle();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Onay işlemi başarısız.' });
                    }
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

        this.marketApiService.reddet(this.seciliMarketVardiya.id, this.marketRedNedeni).subscribe({
            next: () => {
                this.messageService.add({ severity: 'info', summary: 'Reddedildi', detail: 'Market vardiyası reddedildi.' });
                this.marketRedDialogVisible = false;
                this.marketDetayVisible = false;
                this.yukle();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Red işlemi başarısız.' });
            }
        });
    }
}
