import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class PersonelApiService {
    private apiUrl = `${environment.apiUrl}/personel`;

    constructor(private http: HttpClient) { }

    getPersoneller(): Observable<any[]> {
        return this.http.get<any[]>(this.apiUrl);
    }

    getPersonelById(id: number): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/${id}`);
    }

    createPersonel(personel: any): Observable<any> {
        return this.http.post<any>(this.apiUrl, personel);
    }

    updatePersonel(id: number, personel: any): Observable<any> {
        return this.http.put<any>(`${this.apiUrl}/${id}`, personel);
    }

    deletePersonel(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }

    toggleAktif(id: number): Observable<any> {
        return this.http.patch<any>(`${this.apiUrl}/${id}/toggle-aktif`, {});
    }
}
