import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Vardiya, OtomasyonSatis, FiloSatis, YakitTuru } from '../models/vardiya.model';

@Injectable({
    providedIn: 'root'
})
export class VardiyaApiService {
    private apiUrl = 'http://localhost:5133/api/vardiya';

    constructor(private http: HttpClient) { }

    createVardiya(vardiya: Vardiya, satislar: OtomasyonSatis[], filoSatislari: FiloSatis[], dosyaIcerik?: string): Observable<any> {
        const payload = {
            istasyonId: vardiya.istasyonId || 1, // Varsayılan
            baslangicTarihi: vardiya.baslangicTarihi,
            bitisTarihi: vardiya.bitisTarihi,
            dosyaAdi: vardiya.dosyaAdi,
            dosyaIcerik: dosyaIcerik ? dosyaIcerik.split(',')[1] || dosyaIcerik : null,
            otomasyonSatislar: satislar.map(s => ({
                personelAdi: s.personelAdi,
                personelKeyId: s.personelKeyId,
                pompaNo: s.pompaNo,
                yakitTuru: this.mapYakitTuru(s.yakitTuru),
                litre: s.litre,
                birimFiyat: s.birimFiyat,
                toplamTutar: s.toplamTutar,
                satisTarihi: s.satisTarihi,
                fisNo: s.fisNo,
                plaka: s.plaka
            })),
            filoSatislar: filoSatislari.map(f => ({
                tarih: f.tarih,
                filoKodu: f.filoKodu,
                plaka: f.plaka,
                yakitTuru: this.mapYakitTuru(f.yakitTuru),
                litre: f.litre,
                tutar: f.tutar,
                pompaNo: f.pompaNo,
                fisNo: f.fisNo
            }))
        };

        return this.http.post(this.apiUrl, payload);
    }

    getVardiyalar(): Observable<any[]> {
        return this.http.get<any[]>(this.apiUrl);
    }

    getVardiyaById(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}`);
    }

    /**
     * OPTIMIZED: Returns pre-aggregated data for Pompa Mutabakatı page
     * Uses database-level GROUP BY for better performance
     */
    getMutabakat(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}/mutabakat`);
    }

    deleteVardiya(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }

    downloadDosya(id: number): void {
        window.open(`${this.apiUrl}/${id}/dosya`, '_blank');
    }


    private mapYakitTuru(yakitTuru: string): number {
        // Backend Enum ile eşleştirme
        switch (yakitTuru) {
            case 'BENZIN': return 0;
            case 'MOTORIN': return 1;
            case 'LPG': return 2;
            case 'EURO_DIESEL': return 3;
            default: return 0;
        }
    }

    vardiyaOnayaGonder(id: number): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/onaya-gonder`, {});
    }

    getOnayBekleyenVardiyalar(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/onay-bekleyenler`);
    }

    vardiyaOnayla(id: number, onaylayanId: number, onaylayanAdi: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/onayla`, { onaylayanId, onaylayanAdi });
    }

    vardiyaReddet(id: number, onaylayanId: number, onaylayanAdi: string, redNedeni: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/reddet`, { onaylayanId, onaylayanAdi, redNedeni });
    }
}
