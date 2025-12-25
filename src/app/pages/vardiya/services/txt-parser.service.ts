import { Injectable } from '@angular/core';
import { OtomasyonSatis, YakitTuru, FiloSatis } from '../models/vardiya.model';

export interface ParseSonuc {
    basarili: boolean;
    kayitSayisi: number;
    personeller: string[];
    toplamTutar: number;
    baslangicTarih: Date | null;
    bitisTarih: Date | null;
    satislar: OtomasyonSatis[];
    hatalar: string[];
    filoSatislari: FiloSatis[];
}

@Injectable({
    providedIn: 'root'
})
export class TxtParserService {

    parseOtomasyonDosyasi(icerik: string): ParseSonuc {
        const satislar: OtomasyonSatis[] = [];
        const filoSatislari: FiloSatis[] = [];
        const hatalar: string[] = [];
        const personelSet = new Set<string>();
        let toplamTutar = 0;
        let baslangicTarih: Date | null = null;
        let bitisTarih: Date | null = null;

        console.log('=== PARSER BAŞLADI ===');
        console.log('İçerik uzunluğu:', icerik.length);

        // Satırları ayır
        const satirlar = icerik.split(/\r\n|\n/);

        // Regex tanımları (Global flag YOK, satır satır eşleşme için)
        const yakitRegexPart = '(LPG|MOTORIN|KURSUNSUZ|KURŞUNSUZ|BENZIN|BENZİN|EURO DIESEL|V\/MAX DIESEL|V\/MAX KUR|V\/MAX|DIESEL|KATKILI|KATKILI MOTORIN)';
        const plakaRegexPart = '([A-Z0-9\\.\\-\\_]{3,15})';

        const personelRegex = new RegExp(`(\\d{2}\\.\\d{2}\\.\\d{4})\\s+(\\d{2}:\\d{2}:\\d{2})\\s+[İI]STASYON\\s+C0000\\s+([A-Z][A-Z0-9\\.]+)\\s+${yakitRegexPart}\\s+(\\d{6})\\s+(\\d{4})\\s+(\\d{8})\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)`);
        const otobilimRegex = new RegExp(`(\\d{2}\\.\\d{2}\\.\\d{4})\\s+(\\d{2}:\\d{2}:\\d{2})\\s+OTOBILIM\\s+[A-Z0-9_]+\\s+${plakaRegexPart}\\s+${yakitRegexPart}\\s+(\\d{6})\\s+(\\d{4})\\s+(\\d{8})\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)`);
        const ozelSatisRegex = new RegExp(`(\\d{2}\\.\\d{2}\\.\\d{4})\\s+(\\d{2}:\\d{2}:\\d{2})\\s+(YAKITKART|YAKIT\\s+KART|HEDİYEKART|HEDIYEKART|HEDIYE\\s+KART|HEDİYE\\s+KART|TURUNCU\\s+ANAHTARLIK|TURUNCUANAHTARLIK)\\s+([A-Z0-9_]+)\\s+${plakaRegexPart}\\s+${yakitRegexPart}\\s+(\\d{6})\\s+(\\d{4})\\s+(\\d{8})\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)`);
        const filoRegex = new RegExp(`(\\d{2}\\.\\d{2}\\.\\d{4})\\s+(\\d{2}:\\d{2}:\\d{2})\\s+([A-Z0-9\\.\\-\\_]{2,15})\\s+${plakaRegexPart}\\s+${yakitRegexPart}\\s+(\\d{6})\\s+(\\d{4})\\s+(\\d{8})\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)`);

        for (const satir of satirlar) {
            const temizSatir = satir.trim();
            if (temizSatir.length === 0 || temizSatir.startsWith('---') || temizSatir.startsWith('TARİH')) continue;

            let match;

            // 1. Personel Satışı
            if ((match = temizSatir.match(personelRegex))) {
                try {
                    const [, tarihStr, saatStr, personelAdi, yakitStr, litreStr, fiyatStr, tutarStr, tb, pompaNoStr, fisNoStr] = match;
                    const tarih = this.parseTarihSaat(tarihStr, saatStr);
                    const litre = parseInt(litreStr) / 100;
                    const tutar = parseInt(tutarStr) / 100;
                    const birimFiyat = parseInt(fiyatStr) / 100;
                    const pompaNo = parseInt(pompaNoStr);
                    const fisNo = parseInt(fisNoStr);

                    if (!baslangicTarih || tarih < baslangicTarih) baslangicTarih = tarih;
                    if (!bitisTarih || tarih > bitisTarih) bitisTarih = tarih;

                    if (litre <= 0 || tutar <= 0) {
                        console.warn(`0 değerli kayıt atlandı: ${temizSatir}`);
                        continue;
                    }

                    const satis: OtomasyonSatis = {
                        id: fisNo,
                        vardiyaId: 0,
                        personelId: this.getPersonelId(personelAdi),
                        personelAdi: personelAdi.toUpperCase(),
                        personelKeyId: `P${this.getPersonelId(personelAdi).toString().padStart(3, '0')}`,
                        pompaNo,
                        yakitTuru: this.parseYakitTuru(yakitStr),
                        litre,
                        birimFiyat,
                        toplamTutar: tutar,
                        satisTarihi: tarih,
                        fisNo
                    };

                    satislar.push(satis);
                    personelSet.add(personelAdi.toUpperCase());
                    toplamTutar += tutar;
                } catch (error) {
                    hatalar.push(`Personel parse hatası: ${temizSatir}`);
                }
            }
            // 2. Otobilim Satışı
            else if ((match = temizSatir.match(otobilimRegex))) {
                try {
                    const [, tarihStr, saatStr, plaka, yakitStr, litreStr, fiyatStr, tutarStr, tb, pompaNoStr, fisNoStr] = match;
                    const tarih = this.parseTarihSaat(tarihStr, saatStr);
                    const litre = parseInt(litreStr) / 100;
                    const tutar = parseInt(tutarStr) / 100;
                    const fisNo = parseInt(fisNoStr);
                    const pompaNo = parseInt(pompaNoStr);

                    if (litre <= 0 || tutar <= 0) {
                        console.warn(`0 değerli Otobilim kaydı atlandı: ${temizSatir}`);
                        continue;
                    }

                    const filo: FiloSatis = {
                        tarih,
                        filoKodu: 'OTOBILIM',
                        plaka,
                        yakitTuru: this.parseYakitTuru(yakitStr),
                        litre,
                        tutar,
                        pompaNo,
                        fisNo
                    };

                    filoSatislari.push(filo);
                    toplamTutar += tutar;
                } catch (error) {
                    hatalar.push(`Otobilim parse hatası: ${temizSatir}`);
                }
            }
            // 3. Özel Satış (Yakıt Kart, Hediye Kart vb.)
            else if ((match = temizSatir.match(ozelSatisRegex))) {
                try {
                    const [, tarihStr, saatStr, satisTuru, filoKodu, plaka, yakitStr, litreStr, fiyatStr, tutarStr, tb, pompaNoStr, fisNoStr] = match;
                    const tarih = this.parseTarihSaat(tarihStr, saatStr);
                    const litre = parseInt(litreStr) / 100;
                    const tutar = parseInt(tutarStr) / 100;
                    const fisNo = parseInt(fisNoStr);
                    const pompaNo = parseInt(pompaNoStr);

                    if (litre <= 0 || tutar <= 0) {
                        console.warn(`0 değerli Özel Satış kaydı atlandı: ${temizSatir}`);
                        continue;
                    }

                    const filo: FiloSatis = {
                        tarih,
                        filoKodu,
                        plaka,
                        yakitTuru: this.parseYakitTuru(yakitStr),
                        litre,
                        tutar,
                        pompaNo,
                        fisNo
                    };

                    filoSatislari.push(filo);
                    toplamTutar += tutar;
                } catch (error) {
                    hatalar.push(`Özel Satış parse hatası: ${temizSatir}`);
                }
            }
            // 4. Filo Satışı
            else if ((match = temizSatir.match(filoRegex))) {
                try {
                    const [, tarihStr, saatStr, filoKodu, plaka, yakitStr, litreStr, fiyatStr, tutarStr, tb, pompaNoStr, fisNoStr] = match;
                    const tarih = this.parseTarihSaat(tarihStr, saatStr);
                    const litre = parseInt(litreStr) / 100;
                    const tutar = parseInt(tutarStr) / 100;
                    const fisNo = parseInt(fisNoStr);
                    const pompaNo = parseInt(pompaNoStr);

                    if (litre <= 0 || tutar <= 0) {
                        console.warn(`0 değerli Filo kaydı atlandı: ${temizSatir}`);
                        continue;
                    }

                    const filo: FiloSatis = {
                        tarih,
                        filoKodu,
                        plaka,
                        yakitTuru: this.parseYakitTuru(yakitStr),
                        litre,
                        tutar,
                        pompaNo,
                        fisNo
                    };

                    filoSatislari.push(filo);
                    toplamTutar += tutar;
                } catch (error) {
                    hatalar.push(`Filo parse hatası: ${temizSatir}`);
                }
            }
            // Eşleşmeyen Satır
            else {
                if (/\d{2}\.\d{2}\.\d{4}/.test(temizSatir)) {
                    console.warn('Eşleşmeyen Satır:', temizSatir);
                    hatalar.push(`Tanınmayan satır: ${temizSatir}`);
                }
            }
        }

        console.log('=== PARSER BİTTİ ===');
        console.log(`Personel satışları: ${satislar.length}`);
        console.log(`Filo satışları: ${filoSatislari.length}`);
        console.log(`Toplam tutar: ${toplamTutar.toFixed(2)} TL`);
        console.log(`Personeller:`, Array.from(personelSet));
        console.log(`Hatalar:`, hatalar);

        return {
            basarili: satislar.length > 0 || filoSatislari.length > 0,
            kayitSayisi: satislar.length + filoSatislari.length,
            personeller: Array.from(personelSet),
            toplamTutar,
            baslangicTarih,
            bitisTarih,
            satislar,
            filoSatislari,
            hatalar
        };
    }

    private parseTarihSaat(tarihStr: string, saatStr: string): Date {
        const [gun, ay, yil] = tarihStr.split('.').map(s => parseInt(s));
        const [saat, dakika, saniye] = saatStr.split(':').map(s => parseInt(s));
        return new Date(yil, ay - 1, gun, saat, dakika, saniye);
    }

    private parseYakitTuru(yakitStr: string): YakitTuru {
        const upper = yakitStr.toUpperCase().trim();
        if (upper.includes('LPG')) return YakitTuru.LPG;
        if (upper.includes('MOTORIN') || upper.includes('DIESEL')) return YakitTuru.MOTORIN;
        if (upper.includes('KURSUNSUZ') || upper.includes('BENZIN') || upper.includes('KUR')) return YakitTuru.BENZIN;
        return YakitTuru.BENZIN;
    }

    private getPersonelId(personelAdi: string): number {
        const personelMap: Record<string, number> = {
            'A.VURAL': 1,
            'E.AKCA': 2,
            'M.DUMDUZ': 3,
            'F.BAYLAS': 4,
            'U.ZEYLEK': 5,
            'Y.CINAR': 6,
            'L.GUNGOR': 7,
            'H.GULER': 8,
            'S.YILMAZ': 9,
            'O.KAYA': 10,
            'B.DEMIR': 11
        };

        const upperName = personelAdi.toUpperCase();
        if (personelMap[upperName]) {
            return personelMap[upperName];
        }

        let hash = 0;
        for (let i = 0; i < personelAdi.length; i++) {
            hash = ((hash << 5) - hash) + personelAdi.charCodeAt(i);
            hash = hash & hash;
        }
        return Math.abs(hash % 1000) + 100;
    }

    static desteklenenUzantilar(): string[] {
        return ['.txt', '.d1a', '.d1b', '.d1c', '.d1d', '.d1e', '.d1f'];
    }

    static gecerliDosyaMi(dosyaAdi: string): boolean {
        const uzanti = dosyaAdi.toLowerCase().substring(dosyaAdi.lastIndexOf('.'));
        return this.desteklenenUzantilar().includes(uzanti);
    }
}
