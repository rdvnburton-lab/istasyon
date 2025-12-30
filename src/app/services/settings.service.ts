import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserSettingsDto {
    theme: string;
    notificationsEnabled: boolean;
    emailNotifications: boolean;
    language: string;
    extraSettingsJson?: string;
}

@Injectable({
    providedIn: 'root'
})
export class SettingsService {
    private apiUrl = `${environment.apiUrl}/settings`;

    private settingsSubject = new BehaviorSubject<UserSettingsDto | null>(null);
    public settings$ = this.settingsSubject.asObservable();

    constructor(private http: HttpClient) { }

    loadSettings(): void {
        this.getSettings().subscribe();
    }

    getSettings(): Observable<UserSettingsDto> {
        return this.http.get<UserSettingsDto>(this.apiUrl).pipe(
            tap(settings => this.settingsSubject.next(settings))
        );
    }

    updateSettings(settings: UserSettingsDto): Observable<any> {
        return this.http.put(this.apiUrl, settings).pipe(
            tap(() => this.settingsSubject.next(settings))
        );
    }

    getCurrentSettings(): UserSettingsDto | null {
        return this.settingsSubject.value;
    }
}
