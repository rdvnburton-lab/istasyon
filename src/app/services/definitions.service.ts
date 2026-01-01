import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export enum DefinitionType {
    YAKIT = 1,
    BANKA = 2,
    GIDER = 3,
    GELIR = 4,
    ODEME = 5,
    GELIS_YONTEMI = 6,
    POMPA_GIDER = 7,
    PUSULA_TURU = 8
}

export interface SystemDefinition {
    id?: number;
    type: DefinitionType;
    name: string;
    description?: string;
    isActive: boolean;
    sortOrder: number;
    code?: string;
}

@Injectable({
    providedIn: 'root'
})
export class DefinitionsService {
    private apiUrl = `${environment.apiUrl}/definitions`;

    constructor(private http: HttpClient) { }

    getByType(type: DefinitionType): Observable<SystemDefinition[]> {
        return this.http.get<SystemDefinition[]>(`${this.apiUrl}/${type}`);
    }

    create(definition: SystemDefinition): Observable<SystemDefinition> {
        return this.http.post<SystemDefinition>(this.apiUrl, definition);
    }

    update(id: number, definition: SystemDefinition): Observable<SystemDefinition> {
        return this.http.put<SystemDefinition>(`${this.apiUrl}/${id}`, definition);
    }

    delete(id: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }

    // Helper to get Label/Value list for dropdowns
    getDropdownList(type: DefinitionType): Observable<{ label: string; value: any }[]> {
        return new Observable(observer => {
            this.getByType(type).subscribe({
                next: (data) => {
                    const list = data.map(item => ({
                        label: item.name,
                        value: item.code || item.name // Use code if available, otherwise name
                    }));
                    observer.next(list);
                    observer.complete();
                },
                error: (err) => observer.error(err)
            });
        });
    }
}
