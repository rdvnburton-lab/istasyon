import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface KrediKartiDetay {
    banka: string;
    tutar: number;
}

export interface DigerOdeme {
    turKodu: string;
    turAdi: string;
    tutar: number;
    silinemez?: boolean;
}

export interface Veresiye {
    cariKartId: number;
    cariAd?: string; // Gösterim için
    plaka?: string;
    litre: number;
    tutar: number;
    aciklama?: string;
}

export interface Pusula {
    id?: number;
    vardiyaId: number;
    personelAdi: string;
    personelId?: number;
    nakit: number;
    krediKarti: number;

    krediKartiDetay?: KrediKartiDetay[];
    krediKartiDetayList?: KrediKartiDetay[]; // Legacy or DTO naming
    digerOdemeler?: DigerOdeme[];
    digerOdemeList?: DigerOdeme[]; // DTO naming

    veresiyeler?: Veresiye[];

    aciklama?: string;
    pusulaTuru?: string;
    olusturmaTarihi?: Date;
    guncellemeTarihi?: Date;
    toplam?: number;
}

export interface PusulaOzet {
    toplamPusula: number;
    toplamNakit: number;
    toplamKrediKarti: number;

    genelToplam: number;
}

import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class PusulaApiService {
    private baseUrl = `${environment.apiUrl}/vardiya`;

    constructor(private http: HttpClient) { }

    getAll(vardiyaId: number): Observable<Pusula[]> {
        return this.http.get<any[]>(`${this.baseUrl}/${vardiyaId}/pusula`).pipe(
            map(pusulalar => pusulalar.map(p => this.mapFromBackend(p)))
        );
    }

    getById(vardiyaId: number, id: number): Observable<Pusula> {
        return this.http.get<any>(`${this.baseUrl}/${vardiyaId}/pusula/${id}`).pipe(
            map(p => this.mapFromBackend(p))
        );
    }

    getByPersonel(vardiyaId: number, personelAdi: string): Observable<Pusula> {
        return this.http.get<any>(`${this.baseUrl}/${vardiyaId}/pusula/personel/${personelAdi}`).pipe(
            map(p => this.mapFromBackend(p))
        );
    }

    create(vardiyaId: number, pusula: Pusula): Observable<Pusula> {
        const payload = this.mapToBackend(pusula);
        return this.http.post<any>(`${this.baseUrl}/${vardiyaId}/pusula`, payload).pipe(
            map(p => this.mapFromBackend(p))
        );
    }

    update(vardiyaId: number, id: number, pusula: Pusula): Observable<Pusula> {
        const payload = this.mapToBackend(pusula);
        return this.http.put<any>(`${this.baseUrl}/${vardiyaId}/pusula/${id}`, payload).pipe(
            map(p => this.mapFromBackend(p))
        );
    }

    delete(vardiyaId: number, id: number): Observable<void> {
        return this.http.delete<void>(`${this.baseUrl}/${vardiyaId}/pusula/${id}`);
    }

    getOzet(vardiyaId: number): Observable<PusulaOzet> {
        return this.http.get<PusulaOzet>(`${this.baseUrl}/${vardiyaId}/pusula/ozet`);
    }

    analyzeImage(base64Image: string): Observable<any> {
        return this.http.post<any>(`${environment.apiUrl}/pusula-ocr/analyze`, { imageBase64: base64Image });
    }

    private mapFromBackend(data: any): Pusula {
        return {
            ...data,
            krediKartiDetay: (data.krediKartiDetayList && data.krediKartiDetayList.length > 0)
                ? data.krediKartiDetayList.map((d: any) => ({ banka: d.bankaAdi, tutar: d.tutar }))
                : (data.krediKartiDetaylari && data.krediKartiDetaylari.length > 0)
                    ? data.krediKartiDetaylari.map((d: any) => ({ banka: d.bankaAdi, tutar: d.tutar }))
                    : (data.krediKartiDetay ? JSON.parse(data.krediKartiDetay) : []),
            digerOdemeler: (data.digerOdemeler || []).map((d: any) => ({
                turKodu: d.turKodu,
                turAdi: d.turAdi,
                tutar: d.tutar,
                silinemez: d.silinemez || d.Silinemez || false
            })),
            veresiyeler: (data.veresiyeler || []).map((v: any) => ({
                cariKartId: v.cariKartId,
                cariAd: v.cariAd || v.cariKart?.ad,
                plaka: v.plaka,
                litre: v.litre,
                tutar: v.tutar,
                aciklama: v.aciklama
            })),
            pusulaTuru: data.pusulaTuru || 'TAHSILAT',
            olusturmaTarihi: data.olusturmaTarihi ? new Date(data.olusturmaTarihi) : undefined,
            guncellemeTarihi: data.guncellemeTarihi ? new Date(data.guncellemeTarihi) : undefined
        };
    }

    private mapToBackend(pusula: Pusula): any {
        return {
            vardiyaId: pusula.vardiyaId,
            personelAdi: pusula.personelAdi,
            personelId: pusula.personelId,
            nakit: pusula.nakit,
            krediKarti: pusula.krediKarti,

            krediKartiDetay: pusula.krediKartiDetay ? JSON.stringify(pusula.krediKartiDetay) : null,
            krediKartiDetayList: pusula.krediKartiDetay ? pusula.krediKartiDetay.map(d => ({ bankaAdi: d.banka, tutar: d.tutar })) : [],
            digerOdemeList: pusula.digerOdemeler ? pusula.digerOdemeler : [],
            veresiyeList: pusula.veresiyeler ? pusula.veresiyeler.map(v => ({
                cariKartId: v.cariKartId,
                plaka: v.plaka,
                litre: v.litre,
                tutar: v.tutar,
                aciklama: v.aciklama
            })) : [],
            aciklama: pusula.aciklama,
            pusulaTuru: pusula.pusulaTuru || 'TAHSILAT'
        };
    }
}
