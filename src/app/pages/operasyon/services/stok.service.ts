import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';

export interface TankGiris {
    id: string;
    tarih: Date;
    faturaNo: string;
    yakitId: number;
    yakitAd?: string;
    yakitRenk?: string;
    litre: number;
    birimFiyat: number;
    toplamTutar: number;
    kaydeden: string;
    gelisYontemi?: string;
    plaka?: string;
    urunGirisTarihi?: Date;
}

export interface TankStokOzet {
    yakitId: number;
    yakitTuru: string;
    renk: string;
    gecenAyDevir: number;
    buAyGiris: number;
    buAySatis: number;
    kalanStok: number;
}

export interface CreateTankGirisItem {
    yakitId: number;
    litre: number;
    birimFiyat: number;
}

export interface CreateFaturaGiris {
    tarih: Date;
    faturaNo: string;
    kaydeden?: string;
    gelisYontemi?: string;
    plaka?: string;
    urunGirisTarihi?: Date;
    kalemler: CreateTankGirisItem[];
}

export interface StokGirisFis {
    faturaNo: string;
    tarih: Date;
    toplamTutar: number;
    kaydeden: string;
    gelisYontemi?: string;
    plaka?: string;
    urunGirisTarihi?: Date;
    toplamLitre: number;
    kalemler: TankGiris[];
}

@Injectable({
    providedIn: 'root'
})
export class StokService {
    private apiUrl = `${environment.apiUrl}/stok`;

    constructor(private http: HttpClient) { }

    addFaturaGiris(fatura: CreateFaturaGiris): Observable<any> {
        return this.http.post(`${this.apiUrl}/fatura-giris`, fatura);
    }

    updateFatura(oldFaturaNo: string, fatura: CreateFaturaGiris): Observable<any> {
        return this.http.put(`${this.apiUrl}/fatura-giris/${oldFaturaNo}`, fatura);
    }

    getGirisler(month?: number, year?: number, startDate?: Date, endDate?: Date): Observable<StokGirisFis[]> {
        let params = [];
        if (startDate && endDate) {
            params.push(`startDate=${startDate.toISOString()}`);
            params.push(`endDate=${endDate.toISOString()}`);
        } else if (year) {
            params.push(`year=${year}`);
            if (month !== undefined && month !== null) {
                params.push(`month=${month + 1}`);
            }
        }

        const queryString = params.length ? `?${params.join('&')}` : '';
        return this.http.get<StokGirisFis[]>(`${this.apiUrl}/girisler${queryString}`);
    }

    getStokDurumu(month: number, year: number): Observable<TankStokOzet[]> {
        return this.http.get<any>(`${this.apiUrl}/ozet?month=${month + 1}&year=${year}`).pipe(
            tap((response: any) => {
                // Log debug info for troubleshooting
                if (response.debug) {
                    console.log('📊 STOK DEBUG:', response.debug);
                }
            }),
            map((response: any) => response.ozet || response) // Handle both new {Ozet, Debug} and legacy array formats
        );
    }

    // Returns full response including debug info for UI display
    getStokDurumuWithDebug(month: number, year: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/ozet?month=${month + 1}&year=${year}`);
    }

    deleteGiris(id: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/giris/${id}`);
    }

    deleteFatura(faturaNo: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/fatura/${faturaNo}`);
    }

    // Kapsamlı Stok Yönetimi API'leri

    // Aylık stok raporu - hesaplanmış değerler
    getAylikRapor(yil: number, ay: number): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/aylik-rapor?yil=${yil}&ay=${ay}`);
    }

    // Fatura bazında stok durumu - FIFO takibi
    getFaturaStokDurumu(yakitId?: number): Observable<any[]> {
        const query = yakitId ? `?yakitId=${yakitId}` : '';
        return this.http.get<any[]>(`${this.apiUrl}/fatura-stok-durumu${query}`);
    }

    // Stoku yeniden hesapla
    yenidenHesapla(yil: number, ay: number): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/yeniden-hesapla?yil=${yil}&ay=${ay}`, {});
    }

    // Ay kapatma işlemi
    ayKapat(yil: number, ay: number): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/ay-kapat?yil=${yil}&ay=${ay}`, {});
    }

    // Karma stok özeti - XML (Motorin/Benzin) + Manuel (LPG)
    getKarmaStokOzeti(yil: number, ay: number, istasyonId?: number): Observable<KarmaStokOzet> {
        let url = `${this.apiUrl}/karma-ozet?yil=${yil}&ay=${ay}`;
        if (istasyonId) {
            url += `&istasyonId=${istasyonId}`;
        }
        return this.http.get<KarmaStokOzet>(url);
    }
}

// Karma Stok Response Interfaces
export interface XmlStokOzet {
    yakitTipi: string;
    toplamSevkiyat: number;
    toplamSatis: number;
    sonStok: number;
    ilkStok: number;
    toplamFark: number;
    kayitSayisi: number;
}

export interface ManuelStokOzet {
    yakitTipi: string;
    toplamGiris: number;
    toplamSatis: number;
    kaynak: string;
}

export interface VardiyaTankHareket {
    vardiyaId: number;
    tarih: Date;
    tanklar: {
        tankNo: number;
        tankAdi: string;
        yakitTipi: string;
        baslangicStok: number;
        bitisStok: number;
        sevkiyatMiktar: number;
        satilanMiktar: number;
        farkMiktar: number;
    }[];
}

export interface KarmaStokOzet {
    xmlKaynakli: XmlStokOzet[];
    manuelKaynakli: ManuelStokOzet[];
    vardiyaHareketleri: VardiyaTankHareket[];
    donem: { yil: number; ay: number };
    ozet: {
        toplamMotorinStok: number;
        toplamBenzinStok: number;
        motorinSevkiyat: number;
        benzinSevkiyat: number;
        motorinSatis: number;
        benzinSatis: number;
    };
}
