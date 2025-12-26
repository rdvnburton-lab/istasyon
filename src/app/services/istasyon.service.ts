import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Istasyon {
    id: number;
    ad: string;
    adres?: string;
    aktif: boolean;
    firmaId: number;

    // 3 ayrı sorumlu ID
    istasyonSorumluId?: number;
    vardiyaSorumluId?: number;
    marketSorumluId?: number;

    apiKey?: string;
    kod?: string;
    pompaSayisi?: number;
    marketVar?: boolean;

    // Backend'den gelen hazır sorumlu adları
    istasyonSorumlusu?: string;
    vardiyaSorumlusu?: string;
    marketSorumlusu?: string;
}

export interface CreateIstasyonDto {
    ad: string;
    adres?: string;
    firmaId: number;
    istasyonSorumluId?: number;
    vardiyaSorumluId?: number;
    marketSorumluId?: number;
    apiKey?: string;
}

export interface UpdateIstasyonDto {
    ad: string;
    adres?: string;
    aktif: boolean;
    istasyonSorumluId?: number;
    vardiyaSorumluId?: number;
    marketSorumluId?: number;
    apiKey?: string;
}

@Injectable({
    providedIn: 'root'
})
export class IstasyonService {
    private apiUrl = `${environment.apiUrl}/istasyon`;

    constructor(private http: HttpClient) { }

    getIstasyonlar(): Observable<Istasyon[]> {
        return this.http.get<Istasyon[]>(this.apiUrl);
    }

    createIstasyon(istasyon: CreateIstasyonDto): Observable<Istasyon> {
        return this.http.post<Istasyon>(this.apiUrl, istasyon);
    }

    updateIstasyon(id: number, istasyon: UpdateIstasyonDto): Observable<any> {
        return this.http.put(`${this.apiUrl}/${id}`, istasyon);
    }

    deleteIstasyon(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }
}
