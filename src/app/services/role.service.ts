import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Role {
    id: number;
    ad: string;
    aciklama?: string;
    isSystemRole: boolean;
}

export interface CreateRoleDto {
    ad: string;
    aciklama?: string;
}

export interface UpdateRoleDto {
    ad: string;
    aciklama?: string;
}

@Injectable({
    providedIn: 'root'
})
export class RoleService {
    private apiUrl = `${environment.apiUrl}/role`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<Role[]> {
        return this.http.get<Role[]>(this.apiUrl);
    }

    getById(id: number): Observable<Role> {
        return this.http.get<Role>(`${this.apiUrl}/${id}`);
    }

    create(role: CreateRoleDto): Observable<Role> {
        return this.http.post<Role>(this.apiUrl, role);
    }

    update(id: number, role: UpdateRoleDto): Observable<void> {
        return this.http.put<void>(`${this.apiUrl}/${id}`, role);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
