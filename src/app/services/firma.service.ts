import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Firma {
    id: number;
    ad: string;
    adres?: string;
    aktif: boolean;
    patronId?: number;
}

export interface CreateFirmaDto {
    ad: string;
    adres?: string;
    patronId?: number;
}

export interface UpdateFirmaDto {
    ad: string;
    adres?: string;
    aktif: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class FirmaService {
    private apiUrl = `${environment.apiUrl}/firma`;

    constructor(private http: HttpClient) { }

    getFirmalar(): Observable<Firma[]> {
        return this.http.get<Firma[]>(this.apiUrl);
    }

    createFirma(firma: CreateFirmaDto): Observable<Firma> {
        return this.http.post<Firma>(this.apiUrl, firma);
    }

    updateFirma(id: number, firma: UpdateFirmaDto): Observable<any> {
        return this.http.put(`${this.apiUrl}/${id}`, firma);
    }

    deleteFirma(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }
}
