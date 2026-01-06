import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Yakit {
    id: number;
    ad: string;
    otomasyonUrunAdi: string;
    renk: string;
    sira: number;
    turpakUrunKodu?: string;
}

@Injectable({
    providedIn: 'root'
})
export class YakitService {
    private apiUrl = `${environment.apiUrl}/yakit`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<Yakit[]> {
        return this.http.get<Yakit[]>(this.apiUrl);
    }

    add(yakit: Partial<Yakit>): Observable<Yakit> {
        return this.http.post<Yakit>(this.apiUrl, yakit);
    }

    update(id: number, yakit: Partial<Yakit>): Observable<Yakit> {
        return this.http.put<Yakit>(`${this.apiUrl}/${id}`, yakit);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
