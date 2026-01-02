import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Vardiya, OtomasyonSatis, FiloSatis, YakitTuru, VardiyaTankEnvanteri } from '../models/vardiya.model';
import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class VardiyaApiService {
    private apiUrl = `${environment.apiUrl}/vardiya`;
    private pendingCountSubject = new BehaviorSubject<number>(0);
    public pendingCount$ = this.pendingCountSubject.asObservable();

    constructor(private http: HttpClient) {
        this.updatePendingCount();
    }

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
                yakitTuru: s.yakitTuru,
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
                yakitTuru: f.yakitTuru,
                litre: f.litre,
                tutar: f.tutar,
                pompaNo: f.pompaNo,
                fisNo: f.fisNo
            }))
        };

        return this.http.post(this.apiUrl, payload);
    }

    uploadVardiyaZip(file: File): Observable<any> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post(`${this.apiUrl}/upload-xml-zip`, formData);
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

    /**
     * OPTIMIZED: Returns pre-aggregated data for Onay Bekleyenler İncele
     * All calculations done server-side for faster response
     */
    getOnayDetay(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}/onay-detay`);
    }

    getVardiyaRaporu(baslangic: Date, bitis: Date): Observable<any> {
        const params = {
            baslangic: baslangic.toISOString(),
            bitis: bitis.toISOString()
        };
        return this.http.get<any>(`${this.apiUrl}/rapor`, { params });
    }

    getFarkRaporu(baslangic: Date, bitis: Date): Observable<any> {
        const params = {
            baslangic: baslangic.toISOString(),
            bitis: bitis.toISOString()
        };
        return this.http.get<any>(`${this.apiUrl}/fark-raporu`, { params });
    }

    getPersonelKarnesi(personelId: number, baslangic: Date, bitis: Date): Observable<any> {
        const params = {
            baslangic: baslangic.toISOString(),
            bitis: bitis.toISOString()
        };
        return this.http.get<any>(`${this.apiUrl}/personel-karnesi/${personelId}`, { params });
    }

    deleteVardiya(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }

    downloadDosya(id: number): void {
        window.open(`${this.apiUrl}/${id}/dosya`, '_blank');
    }

    getKarsilastirma(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}/karsilastirma`);
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
        return this.http.post(`${this.apiUrl}/${id}/onaya-gonder`, {}).pipe(
            tap(() => this.updatePendingCount())
        );
    }

    getOnayBekleyenVardiyalar(): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/onay-bekleyenler`);
    }

    vardiyaOnayla(id: number, onaylayanId: number, onaylayanAdi: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/onayla`, { OnaylayanId: onaylayanId, OnaylayanAdi: onaylayanAdi }).pipe(
            tap(() => this.updatePendingCount())
        );
    }

    vardiyaReddet(id: number, onaylayanId: number, onaylayanAdi: string, redNedeni: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/reddet`, { OnaylayanId: onaylayanId, OnaylayanAdi: onaylayanAdi, RedNedeni: redNedeni }).pipe(
            tap(() => this.updatePendingCount())
        );
    }

    vardiyaSilmeTalebi(id: number, nedeni: string): Observable<any> {
        return this.http.post(`${this.apiUrl}/${id}/silme-talebi`, { Nedeni: nedeni }).pipe(
            tap(() => this.updatePendingCount())
        );
    }

    getVardiyaLoglari(vardiyaId?: number, limit: number = 100): Observable<any[]> {
        let params = new HttpParams().set('limit', limit.toString());
        if (vardiyaId) {
            params = params.set('vardiyaId', vardiyaId.toString());
        }
        return this.http.get<any[]>(`${this.apiUrl}/loglar`, { params });
    }

    addPompaGider(vardiyaId: number, gider: any): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${vardiyaId}/gider`, gider);
    }

    deletePompaGider(vardiyaId: number, giderId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/${vardiyaId}/gider/${giderId}`);
    }

    // Otomatik Dosya İşlemleri
    getPendingAutomaticFiles(): Observable<any[]> {
        return this.http.get<any[]>(`${environment.apiUrl}/FileTransfer/pending`);
    }

    getAutomaticFileContent(id: number): Observable<any> {
        return this.http.get<any>(`${environment.apiUrl}/FileTransfer/${id}/content`);
    }

    markAsProcessed(id: number): Observable<any> {
        return this.http.post(`${environment.apiUrl}/FileTransfer/${id}/processed`, {});
    }

    processAutomaticFile(id: number): Observable<any> {
        return this.http.post(`${environment.apiUrl}/FileTransfer/${id}/process`, {});
    }

    getTankEnvanter(vardiyaId: number): Observable<VardiyaTankEnvanteri[]> {
        return this.http.get<VardiyaTankEnvanteri[]>(`${environment.apiUrl}/vardiya/${vardiyaId}/tank-envanter`);
    }

    updatePendingCount(): void {
        this.getOnayBekleyenVardiyalar().subscribe({
            next: (data) => {
                this.pendingCountSubject.next(data.length);
            },
            error: (err) => console.error('Error fetching pending count:', err)
        });
    }
}
