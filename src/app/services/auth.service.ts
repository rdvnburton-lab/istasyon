import { Injectable, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, Subject, Subscription, merge, fromEvent, timer, throttleTime, from, of, catchError, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { Capacitor } from '@capacitor/core';
import { NativeBiometric } from '@capgo/capacitor-native-biometric';

export interface SimpleIstasyon {
    id: number;
    ad: string;
}

export interface User {
    id?: number;
    username: string;
    role: string;
    token?: string;
    firmaAdi?: string;
    istasyonlar?: SimpleIstasyon[];
    selectedIstasyonId?: number;
}

export interface AuthResponse {
    token: string;
    username: string;
    role: string;
    id?: number;
    firmaAdi?: string;
    istasyonlar?: SimpleIstasyon[];
}

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private currentUserSubject = new BehaviorSubject<User | null>(null);
    public currentUser$ = this.currentUserSubject.asObservable();

    private selectedIstasyonSubject = new BehaviorSubject<SimpleIstasyon | null>(null);
    public selectedIstasyon$ = this.selectedIstasyonSubject.asObservable();

    private apiUrl = `${environment.apiUrl}/auth`;

    private idleWarningSubject = new Subject<void>();
    public idleWarning$ = this.idleWarningSubject.asObservable();

    private lastActivity: number = Date.now();
    private checkInterval: any;
    private readonly IDLE_LIMIT = 15 * 60 * 1000; // 15 dakika
    private readonly WARNING_LIMIT = 14 * 60 * 1000; // 14 dakika (Son 1 dakika uyarı)
    private warningShown = false;
    private activitySubscription?: Subscription;

    constructor(private http: HttpClient, private router: Router, private ngZone: NgZone) {
        this.loadUserFromStorage();
        if (this.isAuthenticated()) {
            this.startIdleMonitoring();
        }
    }

    private loadUserFromStorage(): void {
        const storedUser = sessionStorage.getItem('currentUser');
        if (storedUser) {
            try {
                const user = JSON.parse(storedUser);
                this.currentUserSubject.next(user);

                // Restore selected station or default to first
                if (user.istasyonlar && user.istasyonlar.length > 0) {
                    const savedStationId = sessionStorage.getItem('selectedIstasyonId');
                    const station = user.istasyonlar.find((i: SimpleIstasyon) => i.id === Number(savedStationId)) || user.istasyonlar[0];
                    this.selectedIstasyonSubject.next(station);
                }
            } catch (e) {
                console.error('Kayıtlı kullanıcı yüklenemedi', e);
                sessionStorage.removeItem('currentUser');
            }
        }
    }

    login(username: string, password: string): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { username, password }).pipe(
            tap(response => {
                const user: User = {
                    id: this.getUserIdFromToken(response.token),
                    username: response.username,
                    role: response.role,
                    token: response.token,
                    firmaAdi: response.firmaAdi,
                    istasyonlar: response.istasyonlar
                };
                this.setCurrentUser(user);

                // Set initial station
                if (user.istasyonlar && user.istasyonlar.length > 0) {
                    this.setSelectedIstasyon(user.istasyonlar[0]);
                }

                this.startIdleMonitoring();
            })
        );
    }

    logout(): void {
        sessionStorage.removeItem('currentUser');
        sessionStorage.removeItem('selectedIstasyonId');
        this.stopIdleMonitoring();
        this.router.navigate(['/auth/login']);
        this.currentUserSubject.next(null);
        this.selectedIstasyonSubject.next(null);
    }

    private setCurrentUser(user: User): void {
        sessionStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUserSubject.next(user);
    }

    public async saveCredentialsForBiometric(username: string, password: string) {
        await this.saveBiometricCredentials(username, password);
    }

    public setSelectedIstasyon(istasyon: SimpleIstasyon) {
        sessionStorage.setItem('selectedIstasyonId', istasyon.id.toString());
        this.selectedIstasyonSubject.next(istasyon);

        // Update current user state with selected station ID if needed
        const currentUser = this.currentUserSubject.value;
        if (currentUser) {
            currentUser.selectedIstasyonId = istasyon.id;
            this.setCurrentUser(currentUser);
        }
    }

    getCurrentUser(): User | null {
        return this.currentUserSubject.value;
    }

    isAuthenticated(): boolean {
        return !!this.currentUserSubject.value;
    }

    getToken(): string | null {
        const user = this.getCurrentUser();
        return user ? user.token || null : null;
    }
    public startIdleMonitoring() {
        this.lastActivity = Date.now();
        this.warningShown = false;

        this.ngZone.runOutsideAngular(() => {
            const events = ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'];
            const eventStreams = events.map(ev => fromEvent(document, ev));

            this.activitySubscription = merge(...eventStreams).pipe(throttleTime(1000)).subscribe(() => {
                this.lastActivity = Date.now();
                if (this.warningShown) {
                    this.ngZone.run(() => {
                        this.warningShown = false;
                        // Kullanıcı geri döndü, uyarıyı kapatmak için bir event fırlatılabilir veya dialog otomatik kapanabilir
                        // Şimdilik sadece timer resetleniyor, dialog logic'i component'te
                    });
                }
            });

            this.checkInterval = setInterval(() => {
                const now = Date.now();
                const idleDuration = now - this.lastActivity;

                if (idleDuration > this.IDLE_LIMIT) {
                    this.ngZone.run(() => {
                        this.logout();
                    });
                } else if (idleDuration > this.WARNING_LIMIT && !this.warningShown) {
                    this.ngZone.run(() => {
                        this.warningShown = true;
                        this.idleWarningSubject.next();
                    });
                }
            }, 10000); // 10 saniyede bir kontrol et
        });
    }

    public stopIdleMonitoring() {
        if (this.checkInterval) {
            clearInterval(this.checkInterval);
        }
        if (this.activitySubscription) {
            this.activitySubscription.unsubscribe();
        }
    }

    public resetIdle() {
        this.lastActivity = Date.now();
        this.warningShown = false;
    }

    private getUserIdFromToken(token: string): number | undefined {
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload.id ? parseInt(payload.id, 10) : undefined;
        } catch (e) {
            return undefined;
        }
    }

    // --- Biometric Authentication ---

    async checkBiometricAvailability(): Promise<boolean> {
        if (!Capacitor.isNativePlatform()) return false;
        try {
            const result = await NativeBiometric.isAvailable();
            return result.isAvailable;
        } catch (e) {
            console.error('Biometric check failed', e);
            return false;
        }
    }

    async saveBiometricCredentials(username: string, password: string): Promise<void> {
        if (!Capacitor.isNativePlatform()) return;
        try {
            // Check if available first
            const available = await this.checkBiometricAvailability();
            if (available) {
                await NativeBiometric.setCredentials({
                    username,
                    password,
                    server: 'tr.com.istasyon.patron',
                });
            }
        } catch (e) {
            console.error('Failed to save biometric credentials', e);
        }
    }

    async loginWithBiometric(): Promise<boolean> {
        if (!Capacitor.isNativePlatform()) return false;
        try {
            const result = await NativeBiometric.isAvailable();
            if (!result.isAvailable) return false;

            await NativeBiometric.verifyIdentity({
                reason: "Patron Paneline Giriş",
                title: "Hızlı Giriş",
                subtitle: "Devam etmek için doğrulayın",
                description: "Lütfen parmak izinizi veya yüzünüzü okutun",
            });

            // If we are here, verification was successful (otherwise it throws)
            const credentials = await NativeBiometric.getCredentials({
                server: 'tr.com.istasyon.patron',
            });

            if (credentials && credentials.username && credentials.password) {
                // Trigger normal login flow
                return new Promise((resolve) => {
                    this.login(credentials.username, credentials.password).subscribe({
                        next: () => resolve(true),
                        error: () => resolve(false)
                    });
                });
            }
        } catch (e) {
            console.error('Biometric login failed', e);
        }
        return false;
    }

    // Clear biometric credentials on logout if needed (Optional policy)
    async clearBiometricCredentials(): Promise<void> {
        if (!Capacitor.isNativePlatform()) return;
        try {
            await NativeBiometric.deleteCredentials({
                server: 'tr.com.istasyon.patron',
            });
        } catch (e) {
            // Ignore
        }
    }
}
