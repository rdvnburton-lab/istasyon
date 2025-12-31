import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
        return this.http.get<TankStokOzet[]>(`${this.apiUrl}/ozet?month=${month + 1}&year=${year}`);
    }

    deleteGiris(id: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/giris/${id}`);
    }

    deleteFatura(faturaNo: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/fatura/${faturaNo}`);
    }
}
