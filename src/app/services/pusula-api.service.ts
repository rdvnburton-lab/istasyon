import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface KrediKartiDetay {
    banka: string;
    tutar: number;
}

export interface Pusula {
    id?: number;
    vardiyaId: number;
    personelAdi: string;
    personelId?: number;
    nakit: number;
    krediKarti: number;
    paroPuan: number;
    mobilOdeme: number;
    krediKartiDetay?: KrediKartiDetay[];
    aciklama?: string;
    olusturmaTarihi?: Date;
    guncellemeTarihi?: Date;
    toplam?: number;
}

export interface PusulaOzet {
    toplamPusula: number;
    toplamNakit: number;
    toplamKrediKarti: number;
    toplamParoPuan: number;
    toplamMobilOdeme: number;
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

    private mapFromBackend(data: any): Pusula {
        return {
            ...data,
            krediKartiDetay: data.krediKartiDetay ? JSON.parse(data.krediKartiDetay) : [],
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
            paroPuan: pusula.paroPuan,
            mobilOdeme: pusula.mobilOdeme,
            krediKartiDetay: pusula.krediKartiDetay ? JSON.stringify(pusula.krediKartiDetay) : null,
            aciklama: pusula.aciklama
        };
    }
}
