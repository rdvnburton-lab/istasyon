import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { SorumluDashboardDto } from '../models/dashboard.model';

@Injectable({
    providedIn: 'root'
})
export class DashboardService {
    private apiUrl = `${environment.apiUrl}/dashboard`;

    constructor(private http: HttpClient) { }

    getSorumluSummary(): Observable<SorumluDashboardDto> {
        return this.http.get<SorumluDashboardDto>(`${this.apiUrl}/sorumlu-summary`);
    }
}
