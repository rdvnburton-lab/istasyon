import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class MarketApiService {
    private apiUrl = `${environment.apiUrl}/MarketVardiya`;

    constructor(private http: HttpClient) { }

    getMarketVardiyalar(): Observable<any[]> {
        return this.http.get<any[]>(this.apiUrl);
    }

    getMarketVardiyaDetay(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}`);
    }

    createMarketVardiya(data: any): Observable<any> {
        return this.http.post<any>(this.apiUrl, data);
    }

    saveZRaporu(id: number, data: any): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/z-raporu`, data);
    }

    saveTahsilat(id: number, data: any): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/tahsilat`, data);
    }

    deleteTahsilat(tahsilatId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/tahsilat/${tahsilatId}`);
    }

    addGider(id: number, data: any): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/gider`, data);
    }

    deleteGider(giderId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/gider/${giderId}`);
    }

    addGelir(id: number, data: any): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/gelir`, data);
    }

    deleteGelir(gelirId: number): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/gelir/${gelirId}`);
    }

    getMarketRaporu(baslangic: Date, bitis: Date): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/rapor`, {
            params: {
                baslangic: baslangic.toISOString(),
                bitis: bitis.toISOString()
            }
        });
    }

    onayla(id: number): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/onayla`, {});
    }

    onayaGonder(id: number): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/onaya-gonder`, {});
    }

    reddet(id: number, neden: string): Observable<any> {
        return this.http.post<any>(`${this.apiUrl}/${id}/reddet`, JSON.stringify(neden), {
            headers: { 'Content-Type': 'application/json' }
        });
    }
}
