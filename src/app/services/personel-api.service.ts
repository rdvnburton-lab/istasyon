import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface Personel {
    id?: number;
    otomasyonAdi: string;
    adSoyad: string;
    keyId?: string;
    rol: string;
    aktif: boolean;
    telefon?: string;
}

import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class PersonelApiService {
    private apiUrl = `${environment.apiUrl}/personel`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<Personel[]> {
        return this.http.get<any[]>(this.apiUrl).pipe(
            map(personeller => personeller.map(p => this.mapPersonelFromBackend(p)))
        );
    }

    getById(id: number): Observable<Personel> {
        return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
            map(p => this.mapPersonelFromBackend(p))
        );
    }

    create(personel: Personel): Observable<Personel> {
        const payload = this.mapPersonelToBackend(personel);
        return this.http.post<Personel>(this.apiUrl, payload);
    }

    update(id: number, personel: Personel): Observable<Personel> {
        const payload = this.mapPersonelToBackend(personel);
        return this.http.put<Personel>(`${this.apiUrl}/${id}`, payload);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    toggleAktif(id: number): Observable<Personel> {
        return this.http.patch<Personel>(`${this.apiUrl}/${id}/toggle-aktif`, {});
    }

    private mapPersonelFromBackend(data: any): Personel {
        return {
            ...data,
            rol: this.mapRolToString(data.rol)
        };
    }

    private mapPersonelToBackend(personel: Personel): any {
        return {
            ...personel,
            rol: personel.rol // Backend handles string enums
        };
    }

    private mapRolToString(rol: any): string {
        if (typeof rol === 'string') return rol;
        switch (rol) {
            case 0: return 'POMPACI';
            case 1: return 'MARKET_GOREVLISI';
            case 2: return 'VARDIYA_SORUMLUSU';
            case 3: return 'PATRON';
            case 4: return 'MARKET_SORUMLUSU';
            default: return 'POMPACI';
        }
    }

    private mapRolToNumber(rol: string): number {
        switch (rol) {
            case 'POMPACI': return 0;
            case 'MARKET_GOREVLISI': return 1;
            case 'VARDIYA_SORUMLUSU': return 2;
            case 'PATRON': return 3;
            case 'MARKET_SORUMLUSU': return 4;
            default: return 0;
        }
    }
}
