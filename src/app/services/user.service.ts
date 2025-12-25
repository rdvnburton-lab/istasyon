import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserDto {
    id: number;
    username: string;
    role: string;
    roleId: number;
    istasyonId?: number;
    istasyonAdi?: string;
    adSoyad?: string;
    telefon?: string;
}

export interface CreateUserDto {
    username: string;
    password: string;
    roleId: number;
    istasyonId?: number;
    adSoyad?: string;
    telefon?: string;
}

export interface UpdateUserDto {
    username: string;
    password?: string;
    roleId: number;
    istasyonId?: number;
    adSoyad?: string;
    telefon?: string;
}

@Injectable({
    providedIn: 'root'
})
export class UserService {
    private apiUrl = `${environment.apiUrl}/user`;
    private authUrl = `${environment.apiUrl}/auth`;

    constructor(private http: HttpClient) { }

    getUsers(): Observable<UserDto[]> {
        return this.http.get<UserDto[]>(this.apiUrl);
    }

    createUser(user: CreateUserDto): Observable<any> {
        return this.http.post(`${this.authUrl}/create-user`, user);
    }

    updateUser(id: number, user: UpdateUserDto): Observable<any> {
        return this.http.put(`${this.apiUrl}/${id}`, user);
    }

    deleteUser(id: number): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${id}`);
    }

    getUsersByRole(role: string): Observable<UserDto[]> {
        return this.http.get<UserDto[]>(`${this.apiUrl}/by-role/${role}`);
    }
}
