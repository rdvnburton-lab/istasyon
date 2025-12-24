import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface Istasyon {
    id: number;
    ad: string;
    adres?: string;
    aktif: boolean;
    parentIstasyonId?: number;
    patronId?: number;
}

export interface CreateIstasyonDto {
    ad: string;
    adres?: string;
    parentIstasyonId?: number;
}

export interface UpdateIstasyonDto {
    ad: string;
    adres?: string;
    aktif: boolean;
}

export interface CreateUserDto {
    username: string;
    password?: string;
    role: string;
    istasyonId: number;
}

@Injectable({
    providedIn: 'root'
})
export class YonetimService {
    private apiUrl = environment.apiUrl;

    constructor(private http: HttpClient) { }

    // Istasyon Methods
    getIstasyonlar(): Observable<Istasyon[]> {
        return this.http.get<Istasyon[]>(`${this.apiUrl}/istasyon`);
    }

    createIstasyon(dto: CreateIstasyonDto): Observable<Istasyon> {
        return this.http.post<Istasyon>(`${this.apiUrl}/istasyon`, dto);
    }

    updateIstasyon(id: number, dto: UpdateIstasyonDto): Observable<any> {
        return this.http.put(`${this.apiUrl}/istasyon/${id}`, dto);
    }

    deleteIstasyon(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/istasyon/${id}`);
    }

    // User Methods
    createUser(dto: CreateUserDto): Observable<any> {
        return this.http.post(`${this.apiUrl}/auth/create-user`, dto);
    }
}
