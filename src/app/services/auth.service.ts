import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { Personel, PersonelRol } from '../pages/vardiya/models/vardiya.model';
import { DbService } from '../pages/vardiya/services/db.service';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private currentUserSubject = new BehaviorSubject<Personel | null>(null);
    public currentUser$ = this.currentUserSubject.asObservable();

    constructor(private dbService: DbService) {
        // Uygulama açıldığında varsa oturumu yükle (basitçe localStorage'dan)
        this.loadUserFromStorage();
    }

    private loadUserFromStorage(): void {
        const storedUser = localStorage.getItem('currentUser');
        if (storedUser) {
            try {
                const user = JSON.parse(storedUser);
                this.currentUserSubject.next(user);
            } catch (e) {
                console.error('Kayıtlı kullanıcı yüklenemedi', e);
                localStorage.removeItem('currentUser');
            }
        }
    }

    login(username: string): Observable<boolean> {
        // Şifre kontrolü yok, sadece kullanıcı adı ile giriş
        // DB'den kullanıcıyı bul
        return from(this.dbService.getPersonelByKey(username)).pipe(
            map(personel => {
                if (personel) {
                    // Personel modeline dönüştür
                    const user: Personel = {
                        id: personel.id!,
                        keyId: personel.keyId,
                        ad: personel.ad,
                        soyad: personel.soyad,
                        tamAd: personel.tamAd,
                        istasyonId: personel.istasyonId,
                        rol: personel.rol as PersonelRol,
                        aktif: personel.aktif
                    };
                    this.setCurrentUser(user);
                    return true;
                }
                return false;
            }),
            catchError(err => {
                console.error('Giriş hatası:', err);
                return of(false);
            })
        );
    }

    logout(): void {
        localStorage.removeItem('currentUser');
        this.currentUserSubject.next(null);
    }

    private setCurrentUser(user: Personel): void {
        localStorage.setItem('currentUser', JSON.stringify(user));
        this.currentUserSubject.next(user);
    }

    getCurrentUser(): Personel | null {
        return this.currentUserSubject.value;
    }

    isAuthenticated(): boolean {
        return !!this.currentUserSubject.value;
    }

    hasRole(rol: PersonelRol): boolean {
        const user = this.getCurrentUser();
        return user ? user.rol === rol : false;
    }
}
