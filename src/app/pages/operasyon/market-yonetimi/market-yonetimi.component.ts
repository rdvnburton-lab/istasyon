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
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { CheckboxModule } from 'primeng/checkbox';

import { MessageService, ConfirmationService } from 'primeng/api';

import { VardiyaService } from '../services/vardiya.service';
import { MarketApiService } from '../services/market-api.service';
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
} from '../models/vardiya.model';
import { PersonelApiService, Personel } from '../../../services/personel-api.service';
import { AuthService, User } from '../../../services/auth.service';
import { DefinitionsService, DefinitionType } from '../../../services/definitions.service';
import { forkJoin } from 'rxjs';

// ... (in class methods)

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
        InputNumberModule,
        TextareaModule,
        ToggleSwitchModule,
        CheckboxModule,
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
export class MarketYonetimiComponent implements OnInit, OnDestroy {
    seciliMarketVardiya: MarketVardiya | null = null;
    marketOzet: MarketOzet | null = null;
    zRaporu: MarketZRaporu | null = null;
    tahsilatlar: MarketTahsilat[] = [];
    giderler: MarketGider[] = [];
    gelirler: MarketGelir[] = [];

    // Lists
    marketVardiyalar: MarketVardiya[] = [];
    marketPersonelleri: { label: string; value: number }[] = [];
    gelirTurleri: { label: string; value: any }[] = [];
    giderTurleri: { label: string; value: any }[] = [];
    bankalar: { label: string; value: any }[] = []; // Banka listesi

    // Dialogs
    zRaporuDialogVisible: boolean = false;
    pusulaDialogVisible: boolean = false;
    giderDialogVisible: boolean = false;
    gelirDialogVisible: boolean = false;
    redDialogVisible: boolean = false;
    onaylaDialogVisible: boolean = false;

    // Forms
    zRaporuForm: any = { genelToplam: 0, kdv0: 0, kdv1: 0, kdv10: 0, kdv20: 0 };
    personelIslemForm = {
        personelId: null as number | null,
        sistemSatisTutari: 0,
        nakit: 0,
        krediKarti: 0,
        paroPuan: 0,
        personelFazlasi: 0,
        bankaId: null as number | null,
        krediKartiDetay: [] as { bankaId: number, bankaAdi: string, tutar: number }[]
    };

    // Helper for adding bank
    yeniBankaIslem = {
        bankaId: null as number | null,
        tutar: 0
    };
    giderForm: any = { giderTuru: null, tutar: 0, aciklama: '' };
    gelirForm: any = { gelirTuru: null, tutar: 0, aciklama: '' };

    // Diğer
    currentUser: User | null = null;
    redNedeni: string = '';
    reddedilecekVardiyaId: number | null = null;

    // Yeni Vardiya Formu
    yeniVardiyaForm = { tarih: new Date() };
    secilenTarih: Date = new Date();
    mutabakatBaslatildi: boolean = false;

    // Helpers
    kdvKontrolSonuc = { gecerli: false, mesaj: 'Z Raporu bilgilerini girin', sinif: 'bg-surface-100 dark:bg-surface-800', icon: 'pi-info-circle' };
    private subscriptions = new Subscription();
    yonetilenPersoneller: any[] = []; // ViewModel for List

    constructor(
        private vardiyaService: VardiyaService,
        private marketApiService: MarketApiService,
        private personelApiService: PersonelApiService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private router: Router,
        private authService: AuthService,
        private definitionsService: DefinitionsService
    ) { }


    get personelListesi() {
        return this.marketPersonelleri;
    }

    get listOzet() {
        const ozet = {
            toplamSatis: 0,
            toplamTeslimat: 0,
            toplamFark: 0,
            onayBekleyen: 0
        };

        this.marketVardiyalar.forEach(v => {
            ozet.toplamSatis += v.toplamSatisTutari || 0;
            ozet.toplamTeslimat += v.toplamTeslimatTutari || 0;
            ozet.toplamFark += v.toplamFark || 0;
            if (v.durum === 'ONAY_BEKLIYOR') {
                ozet.onayBekleyen++;
            }
        });

        return ozet;
    }

    get personelListesiOzet() {
        const ozet = {
            sistemSatis: 0,
            nakit: 0,
            krediKarti: 0,
            paroPuan: 0,
            teslimat: 0,
            fark: 0,
            personelFazlasi: 0
        };

        this.yonetilenPersoneller.forEach(p => {
            ozet.sistemSatis += p.sistemSatisTutari || 0;
            ozet.nakit += p.nakit || 0;
            ozet.krediKarti += p.krediKarti || 0;
            ozet.paroPuan += p.paroPuan || 0;
            ozet.teslimat += p.toplamTeslimat || 0;
            ozet.personelFazlasi += p.personelFazlasi || 0;
            ozet.fark += p.fark || 0;
        });

        return ozet;
    }

    get marketHesapOzet() {
        if (!this.seciliMarketVardiya) return null;

        const pOzet = this.personelListesiOzet;
        const giderTop = this.giderler.reduce((sum, l) => sum + (l.tutar || 0), 0);
        const gelirTop = this.gelirler.reduce((sum, l) => sum + (l.tutar || 0), 0);

        // Farkı etkileyen gelirlerin toplamı
        const gelirFarkTop = this.gelirler
            .filter(g => g.farkiEtkilesin !== false)
            .reduce((sum, l) => sum + (l.tutar || 0), 0);

        // Farkı etkileyen giderlerin toplamı
        const giderFarkTop = this.giderler
            .filter(g => g.farkiEtkilesin !== false)
            .reduce((sum, l) => sum + (l.tutar || 0), 0);

        // Z-Raporu ve Personel reaktifliği için computed değerler
        const satisTop = this.zRaporu?.genelToplam ?? this.seciliMarketVardiya.toplamSatisTutari ?? 0;
        const computedNetKasa = pOzet.teslimat + gelirTop - giderTop;

        // Fark = (Tahsilat + FarkEtkileyenGelir - FarkEtkileyenGider - Fazlalık) - Satış
        const computedFark = (pOzet.teslimat + gelirFarkTop - giderFarkTop - pOzet.personelFazlasi) - satisTop;

        return {
            sistemSatis: satisTop,
            teslimatPersonel: pOzet.teslimat,
            teslimatNakit: pOzet.nakit,
            teslimatKrediKarti: pOzet.krediKarti,
            giderler: giderTop,
            gelirler: gelirTop,
            netKasa: computedNetKasa,
            fark: computedFark,
            personelFazlasi: pOzet.personelFazlasi
        };
    }

    formSifirla(): void {
        this.personelIslemForm = {
            personelId: null,
            sistemSatisTutari: 0,
            nakit: 0,
            krediKarti: 0,
            paroPuan: 0,
            personelFazlasi: 0,
            bankaId: null,
            krediKartiDetay: []
        };
    }

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

    ngOnInit(): void {
        this.loadDefinitions();

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

    loadDefinitions(): void {
        this.definitionsService.getDropdownList(DefinitionType.GIDER).subscribe((list: any[]) => {
            this.giderTurleri = list;
        });
        this.definitionsService.getDropdownList(DefinitionType.GELIR).subscribe((list: any[]) => {
            this.gelirTurleri = list;
        });
        // Use getByType to ensure we get the ID (number) instead of Code (string)
        this.definitionsService.getByType(DefinitionType.BANKA).subscribe((list: any[]) => {
            this.bankalar = list.map(item => ({ label: item.name, value: item.id! }));
        });
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
        this.pusulaDialogVisible = false;
        this.personelIslemForm = {
            personelId: null,
            sistemSatisTutari: 0,
            nakit: 0,
            krediKarti: 0,
            paroPuan: 0,
            personelFazlasi: 0,
            bankaId: null,
            krediKartiDetay: []
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
                paroPuan: t.paroPuan || 0,
                personelFazlasi: t.personelFazlasi || 0,
                toplamTeslimat: t.toplam,
                fark: (t.toplam - (t.personelFazlasi || 0)) - (t.sistemSatisTutari || 0),
                olusturmaTarihi: t.olusturmaTarihi,
                krediKartiDetayJson: t.krediKartiDetayJson
            }));

            if (data.zRaporlari && data.zRaporlari.length > 0) {
                const z = data.zRaporlari[0];
                this.zRaporu = z;

                // Base -> Gross dönüşümünde oluşan kuruş farklarını (ör: 10.550 -> 10.549,99)
                // Genel Toplam'a bakarak düzeltiyoruz.

                let g1 = Math.round(z.kdv1 * 1.01 * 100) / 100;
                let g10 = Math.round(z.kdv10 * 1.10 * 100) / 100;
                let g20 = Math.round(z.kdv20 * 1.20 * 100) / 100;

                // Ham toplam
                const currentSum = z.kdv0 + g1 + g10 + g20;
                const diff = Math.round((z.genelToplam - currentSum) * 100) / 100;

                // Farkı dağıt (En büyük tutara ekle veya sırayla)
                // Genelde %10 veya %20'de kayıp olur.
                if (diff !== 0) {
                    // Basitçe en büyük 'Base'e sahip olana ekleyelim, fark genelde 0.01 veya 0.02'dir.
                    // Veya direk g10'a ekleyelim eğer g10 varsa. 
                    if (z.kdv20 > 0) g20 += diff;
                    else if (z.kdv10 > 0) g10 += diff;
                    else if (z.kdv1 > 0) g1 += diff;
                }

                this.zRaporuForm = {
                    genelToplam: z.genelToplam,
                    kdv0: z.kdv0,
                    kdv1: Number(g1.toFixed(2)),
                    kdv10: Number(g10.toFixed(2)),
                    kdv20: Number(g20.toFixed(2))
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
        this.hesaplaZTotals();
    }

    hesaplaZTotals(): void {
        const kdv0 = this.zRaporuForm.kdv0 || 0;
        const kdv1 = this.zRaporuForm.kdv1 || 0;
        const kdv10 = this.zRaporuForm.kdv10 || 0;
        const kdv20 = this.zRaporuForm.kdv20 || 0;

        // Yuvarlama hatasını önlemek için
        const total = kdv0 + kdv1 + kdv10 + kdv20;
        this.zRaporuForm.genelToplam = Math.round(total * 100) / 100;
    }

    getKdvToplam(): number {
        const kdv1 = this.zRaporuForm.kdv1 || 0;
        const kdv10 = this.zRaporuForm.kdv10 || 0;
        const kdv20 = this.zRaporuForm.kdv20 || 0;

        // Her kalemin vergisini ayrı ayrı hesaplayıp YUVARLIYORUZ (2 hane)
        // Tax = Gross - (Gross / (1+Rate))

        const tax1 = Math.round((kdv1 - (kdv1 / 1.01)) * 100) / 100;
        const tax10 = Math.round((kdv10 - (kdv10 / 1.10)) * 100) / 100;
        const tax20 = Math.round((kdv20 - (kdv20 / 1.20)) * 100) / 100;

        return Math.round((tax1 + tax10 + tax20) * 100) / 100;
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

    // Banka İşlemleri
    bankaIslemEkle(): void {
        if (!this.yeniBankaIslem.bankaId || this.yeniBankaIslem.tutar <= 0) {
            return;
        }

        const banka = this.bankalar.find(b => b.value === this.yeniBankaIslem.bankaId);
        if (banka) {
            this.personelIslemForm.krediKartiDetay.push({
                bankaId: this.yeniBankaIslem.bankaId!,
                bankaAdi: banka.label,
                tutar: this.yeniBankaIslem.tutar
            });

            this.updateKrediKartiToplam();
            this.yeniBankaIslem = { bankaId: null, tutar: 0 };
        }
    }

    bankaIslemSil(index: number): void {
        this.personelIslemForm.krediKartiDetay.splice(index, 1);
        this.updateKrediKartiToplam();
    }

    updateKrediKartiToplam(): void {
        this.personelIslemForm.krediKarti = this.personelIslemForm.krediKartiDetay.reduce((sum, item) => sum + item.tutar, 0);
    }

    // Kayıt işlemleri


    zRaporuDialogAc(vardiya?: MarketVardiya): void {
        if (vardiya) {
            this.vardiyaSec(vardiya);
        }
        this.zRaporuDialogVisible = true;
    }

    zRaporuKaydet(): void {
        if (!this.seciliMarketVardiya) return;

        // Ensure totals are fresh
        this.hesaplaZTotals();

        const data = {
            genelToplam: this.zRaporuForm.genelToplam,
            kdv0: this.zRaporuForm.kdv0 || 0,
            kdv1: this.zRaporuForm.kdv1 || 0,
            kdv10: this.zRaporuForm.kdv10 || 0,
            kdv20: this.zRaporuForm.kdv20 || 0,
            kdvToplam: this.getKdvToplam(),
            kdvHaricToplam: this.getKdvHaric(),
            isKdvDahil: true
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
            paroPuan: this.personelIslemForm.paroPuan,
            personelFazlasi: this.personelIslemForm.personelFazlasi,
            toplam: this.personelIslemForm.nakit + this.personelIslemForm.krediKarti + this.personelIslemForm.paroPuan,
            krediKartiDetayJson: JSON.stringify(this.personelIslemForm.krediKartiDetay),
            bankaId: null,
            aciklama: this.personelIslemForm.krediKartiDetay.map(x => x.bankaAdi).join(', ')
        };

        this.marketApiService.saveTahsilat(this.seciliMarketVardiya.id, data).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'İşlem kaydedildi' });
                this.formSifirla();
                this.pusulaDialogVisible = false;
                this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
            },
            error: (err: any) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kaydetme sırasında bir hata oluştu' });
            }
        });
    }

    personelDialogAc(): void {
        this.personelIslemForm = {
            personelId: null,
            sistemSatisTutari: 0,
            nakit: 0,
            krediKarti: 0,
            paroPuan: 0,
            personelFazlasi: 0,
            bankaId: null,
            krediKartiDetay: []
        };
        this.yeniBankaIslem = { bankaId: null, tutar: 0 };
        this.pusulaDialogVisible = true;
    }

    personelSil(kayit: any): void {
        this.confirmationService.confirm({
            message: `${kayit.personelAdi} personeline ait bu tahsilat kaydını silmek istediğinize emin misiniz?`,
            header: 'Kayıt Sil',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Sil',
            rejectLabel: 'Vazgeç',
            accept: () => {
                this.marketApiService.deleteTahsilat(kayit.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kayıt silindi' });
                        this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kayıt silinirken bir hata oluştu' });
                    }
                });
            }
        });
    }

    // Düzenlemek için personel seç
    personelDuzenle(kayit: any): void { // Changed type to any to avoid strict check on dynamic prop
        this.personelIslemForm = {
            personelId: kayit.personelId,
            sistemSatisTutari: kayit.sistemSatisTutari,
            nakit: kayit.nakit || 0,
            krediKarti: kayit.krediKarti || 0,
            paroPuan: kayit.paroPuan || 0,
            personelFazlasi: kayit.personelFazlasi || 0,
            bankaId: null,
            krediKartiDetay: kayit.krediKartiDetayJson ? JSON.parse(kayit.krediKartiDetayJson) : []
        };
        this.yeniBankaIslem = { bankaId: null, tutar: 0 };
        this.pusulaDialogVisible = true;
    }

    getPersonelIslemToplam(): number {
        return (this.personelIslemForm.nakit || 0) + (this.personelIslemForm.krediKarti || 0) + (this.personelIslemForm.paroPuan || 0);
    }

    getPersonelFark(): number {
        const teslimat = this.getPersonelIslemToplam();
        const satis = this.personelIslemForm.sistemSatisTutari || 0;
        const fazlalik = this.personelIslemForm.personelFazlasi || 0;
        return (teslimat - fazlalik) - satis;
    }

    personelFazlasiniKasayaAktar(): void {
        const teslimat = this.getPersonelIslemToplam();
        const satis = this.personelIslemForm.sistemSatisTutari || 0;
        const fark = teslimat - satis;

        if (fark > 0) {
            this.personelIslemForm.personelFazlasi = fark;
            this.messageService.add({
                severity: 'success',
                summary: 'Başarılı',
                detail: `${fark.toFixed(2)} ₺ tutarındaki fazlalık kasaya aktarıldı. Personel hesabı sıfırlandı.`
            });
        }
    }

    // Gider işlemleri
    giderDialogAc(): void {
        this.giderForm = { giderTuru: null, tutar: 0, aciklama: '', farkiEtkilesin: true };
        this.giderDialogVisible = true;
    }

    giderEkle(): void {
        if (!this.giderForm.giderTuru || !this.giderForm.tutar || !this.seciliMarketVardiya) return;

        this.marketApiService.addGider(this.seciliMarketVardiya.id, {
            giderTuru: this.giderForm.giderTuru,
            tutar: this.giderForm.tutar,
            aciklama: this.giderForm.aciklama || this.getGiderTuruLabel(this.giderForm.giderTuru),
            farkiEtkilesin: this.giderForm.farkiEtkilesin
        }).subscribe(() => {
            this.giderDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider eklendi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    giderSil(giderId: number): void {
        this.confirmationService.confirm({
            message: 'Bu gider kaydını silmek istediğinize emin misiniz?',
            header: 'Gider Sil',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Sil',
            rejectLabel: 'Vazgeç',
            accept: () => {
                this.marketApiService.deleteGider(giderId).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gider silindi' });
                    this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
                });
            }
        });
    }

    getGiderTuruLabel(turu: GiderTuru): string {
        const found = this.giderTurleri.find(t => t.value === turu);
        return found?.label || turu;
    }

    // Gelir İşlemleri
    gelirDialogAc(): void {
        this.gelirForm = { gelirTuru: null, tutar: 0, aciklama: '', farkiEtkilesin: true };
        this.gelirDialogVisible = true;
    }

    gelirEkle(): void {
        if (!this.gelirForm.gelirTuru || !this.gelirForm.tutar || !this.seciliMarketVardiya) return;

        this.marketApiService.addGelir(this.seciliMarketVardiya.id, {
            gelirTuru: this.gelirForm.gelirTuru,
            tutar: this.gelirForm.tutar,
            aciklama: this.gelirForm.aciklama || this.getGelirTuruLabel(this.gelirForm.gelirTuru),
            farkiEtkilesin: this.gelirForm.farkiEtkilesin
        }).subscribe(() => {
            this.gelirDialogVisible = false;
            this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir eklendi' });
            this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
        });
    }

    gelirSil(gelirId: number): void {
        this.confirmationService.confirm({
            message: 'Bu gelir kaydını silmek istediğinize emin misiniz?',
            header: 'Gelir Sil',
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: 'Evet, Sil',
            rejectLabel: 'Vazgeç',
            accept: () => {
                this.marketApiService.deleteGelir(gelirId).subscribe(() => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Gelir silindi' });
                    this.vardiyaSec(this.seciliMarketVardiya!); // Refresh
                });
            }
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
