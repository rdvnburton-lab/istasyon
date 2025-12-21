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
import { Vardiya, VardiyaOzet, PersonelFarkAnalizi, MarketOzet, GenelOzet, MarketVardiya } from '../../models/vardiya.model';

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
        private confirmationService: ConfirmationService,
        private messageService: MessageService
    ) { }

    ngOnInit() {
        this.yukle();
    }

    yukle() {
        this.vardiyaService.getOnayBekleyenVardiyalar().subscribe((data: Vardiya[]) => {
            this.vardiyalar = data;
        });
        this.vardiyaService.getOnayBekleyenMarketVardiyalar().subscribe((data: MarketVardiya[]) => {
            this.marketVardiyalar = data;
        });
    }



    // Kredi Kartı Detayları
    krediKartiDialogVisible: boolean = false;
    pusulaKrediKartiDetaylari: { banka: string; tutar: number; showSeparator?: boolean; isSpecial?: boolean }[] = [];
    toplamKrediKarti: number = 0;

    // Yeni eklenen alanlar
    pompaciSatisTutar: number = 0;
    pompaciNakitToplam: number = 0;
    pompaFark: number = 0;
    pompaFarkDurumRenk: 'success' | 'warn' | 'danger' = 'success';

    openKrediKartiDetay() {
        this.krediKartiDialogVisible = true;
    }

    incele(vardiya: Vardiya) {
        this.seciliVardiya = vardiya;
        this.vardiyaService.setAktifVardiyaById(vardiya.id).then(() => {
            // Tüm verileri paralel çek
            this.vardiyaService.getGenelOzet(vardiya.id).subscribe(val => {
                this.genelOzet = val;
                // Pompacı Satışları = Genel Ciro - Otomasyon Satışları (Pompa Toplam)
                // Not: Service'de pompaToplam zaten otomasyon satışları toplamıdır.
                // Kullanıcı isteği: "toplam cirodan otomasyon satışlarını düşerek pompacı satışlarını bulalım"
                // Genel Ciro (Genel Toplam) = Pompa (Otomasyon) + Market
                // Dolayısıyla bu işlem Market cirosunu verir. Ancak kullanıcının isteği isimlendirme olarak "Pompacı Satışları".
                // Belki de "Toplam Tahsilat" - "Otomasyon" demek istemiştir ama "Toplam Ciro" dedi.
                // Biz tam olarak dediğini yapalım: GenelToplam - PompaToplam

            });

            this.vardiyaService.getFarkAnalizi(vardiya.id).subscribe(val => {
                this.farkAnalizi = val;
                // Pompacı Nakit Satış Sayısı (Tutarı)
                // Fark analizindeki pusula nakitlerini topla
                this.pompaciNakitToplam = this.farkAnalizi.reduce((sum, item) => sum + (item.pusulaDokum?.nakit || 0), 0);

                // Pompacı Satışları (Sistemden gelen satış verisi - OTOMASYON hariç)
                // Kullanıcı isteği: "otomasyon adı altında topladığımız bir veri seti... bunu toplam satışlardan düşüp"
                // Yani: Toplam Otomasyon - "OTOMASYON" isimli personelin satışı
                this.pompaciSatisTutar = this.farkAnalizi
                    .filter(item => item.personelAdi.toUpperCase() !== 'OTOMASYON')
                    .reduce((sum, item) => sum + (item.otomasyonToplam || 0), 0);

                // Pompa Farkı Hesabı (Pusula Toplamı - Otomasyon Toplamı)
                // Bu, pompacıların kasasındaki farkı gösterir. Market hariç.
                // Not: Pompacı Satışları (pompaciSatisTutar) sadece pompacıların otomasyon satışıdır.
                // Pompacıların Toplam Tahsilatı (pusulaToplam) ile karşılaştıracağız.
                const pusulaToplamTahsilat = this.farkAnalizi.reduce((sum, item) => sum + (item.pusulaToplam || 0), 0);
                // Burada tüm fark analizi kalemlerini alıyoruz çünkü OTOMASYON kullanıcısının tahsilatı varsa o da hesaba katılır mı?
                // Genelde OTOMASYON kullanıcısı sanal bir kullanıcıdır, tahsilat girmez.
                // O yüzden fark analizi üzerinden toplam farkı bulabiliriz.

                // Veya Fark Analizi'ndeki 'fark' sütunlarını toplayarak da bulabiliriz.
                this.pompaFark = this.farkAnalizi.reduce((sum, item) => sum + item.fark, 0);

                // Durum Rengi
                if (Math.abs(this.pompaFark) < 10) {
                    this.pompaFarkDurumRenk = 'success';
                } else if (this.pompaFark < 0) {
                    this.pompaFarkDurumRenk = 'danger'; // Açık
                } else {
                    this.pompaFarkDurumRenk = 'warn'; // Fazla
                }
            });
            this.vardiyaService.getMarketOzet().subscribe(val => this.marketOzet = val);
            this.vardiyaService.getVardiyaOzet(vardiya.id).subscribe(val => this.vardiyaOzet = val);

            // Kredi Kartı Detaylarını Hesapla
            this.vardiyaService.getPusulaGirisleri().subscribe(pusulalar => {
                this.hesaplaKrediKartiDetaylari(pusulalar);
            });

            this.detayVisible = true;
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
                // Detay yoksa genel olarak ekle
                const banka = 'Genel / Detaysız';
                const mevcut = bankaMap.get(banka) || 0;
                bankaMap.set(banka, mevcut + pusula.krediKarti);
                this.toplamKrediKarti += pusula.krediKarti;
            }

            // Paro Puan ve Mobil Ödeme (Kredi Kartı gibi döküme ekleniyor)
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

        // 1. Normal kayıtları tutara göre sırala
        const normalKayitlar = tumKayitlar.filter(x => !x.isSpecial).sort((a, b) => b.tutar - a.tutar);

        // 2. Özel kayıtları (Paro, Mobil) tutara göre sırala
        const ozelKayitlar = tumKayitlar.filter(x => x.isSpecial).sort((a, b) => b.tutar - a.tutar);

        // 3. Özel kayıtların ilki varsa ona ayraç işareti koy (eğer normal kayıtlar da varsa)
        if (ozelKayitlar.length > 0 && normalKayitlar.length > 0) {
            ozelKayitlar[0].showSeparator = true;
        }

        // 4. Birleştir: Normal kayıtlar üstte, özel kayıtlar altta
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
                this.vardiyaService.vardiyaOnayla(vardiya.id).subscribe({
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

        this.vardiyaService.vardiyaReddet(this.seciliVardiya.id, this.redNedeni).subscribe({
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
        // Market özeti için market verilerini yükle
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
                // Şimdilik dummy kullanıcı
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
