import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Istasyon {
    id: number;
    ad: string;
    adres?: string;
    aktif: boolean;
    parentIstasyonId?: number;
    patronId?: number;
    sorumluId?: number;
    kod?: string;
    pompaSayisi?: number;
    marketVar?: boolean;
}

export interface CreateIstasyonDto {
    ad: string;
    adres?: string;
    parentIstasyonId?: number;
    patronId?: number;
    sorumluId?: number;
}

export interface UpdateIstasyonDto {
    ad: string;
    adres?: string;
    aktif: boolean;
    sorumluId?: number;
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
