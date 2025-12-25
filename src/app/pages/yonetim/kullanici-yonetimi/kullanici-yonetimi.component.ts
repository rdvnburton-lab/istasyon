import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PasswordModule } from 'primeng/password';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { UserService, UserDto, CreateUserDto, UpdateUserDto } from '../../../services/user.service';
import { YonetimService, Istasyon } from '../services/yonetim.service';
import { AuthService } from '../../../services/auth.service';
import { RoleService, Role } from '../../../services/role.service';

@Component({
    selector: 'app-kullanici-yonetimi',
    standalone: true,
    imports: [CommonModule, FormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, SelectModule, PasswordModule, ToastModule],
    providers: [MessageService],
    template: `
        <div class="card">
            <p-toast></p-toast>
            <div class="flex justify-content-between align-items-center mb-4">
                <h2 class="m-0">Kullanıcı Yönetimi</h2>
                <button pButton label="Yeni Kullanıcı" icon="pi pi-plus" class="p-button-success" (click)="openNew()"></button>
            </div>

            <p-table [value]="users" [tableStyle]="{'min-width': '50rem'}">
                <ng-template pTemplate="header">
                    <tr>
                        <th>ID</th>
                        <th>Kullanıcı Adı</th>
                        <th>Ad Soyad</th>
                        <th>Telefon</th>
                        <th>Rol</th>
                        <th>İstasyon / Firma</th>
                        <th>İşlemler</th>
                    </tr>
                </ng-template>
                <ng-template pTemplate="body" let-user>
                    <tr>
                        <td>{{user.id}}</td>
                        <td>{{user.username}}</td>
                        <td>{{user.adSoyad || '-'}}</td>
                        <td>{{user.telefon || '-'}}</td>
                        <td>{{user.role}}</td>
                        <td>{{user.istasyonAdi || '-'}}</td>
                        <td>
                            <button pButton icon="pi pi-pencil" class="p-button-rounded p-button-text p-button-warning mr-2" (click)="editUser(user)"></button>
                            <button pButton icon="pi pi-trash" class="p-button-rounded p-button-text p-button-danger" (click)="deleteUser(user)"></button>
                        </td>
                    </tr>
                </ng-template>
            </p-table>

            <p-dialog [(visible)]="dialogVisible" [style]="{width: '450px'}" header="Kullanıcı Detay" [modal]="true" styleClass="p-fluid">
                <ng-template pTemplate="content">
                    <div class="field">
                        <label for="username">Kullanıcı Adı</label>
                        <input type="text" pInputText id="username" [(ngModel)]="user.username" required autofocus />
                        <small class="p-error" *ngIf="submitted && !user.username">Kullanıcı adı gereklidir.</small>
                    </div>
                    
                    <div class="field">
                        <label for="adSoyad">Ad Soyad</label>
                        <input type="text" pInputText id="adSoyad" [(ngModel)]="user.adSoyad" />
                    </div>

                    <div class="field">
                        <label for="telefon">Telefon</label>
                        <input type="text" pInputText id="telefon" [(ngModel)]="user.telefon" />
                    </div>
                    
                    <div class="field" *ngIf="!user.id || changePassword">
                        <label for="password">Şifre</label>
                        <p-password id="password" [(ngModel)]="user.password" [toggleMask]="true" [feedback]="false"></p-password>
                        <small class="p-error" *ngIf="submitted && (!user.id && !user.password)">Şifre gereklidir.</small>
                    </div>

                    <div class="field" *ngIf="user.id && !changePassword">
                         <button pButton type="button" label="Şifreyi Değiştir" class="p-button-secondary p-button-text" (click)="changePassword = true"></button>
                    </div>

                    <div class="field" *ngIf="!(isPatron && user.id === currentUserId)">
                        <label for="role">Rol</label>
                        <p-select id="role" [options]="roles" [(ngModel)]="user.roleId" placeholder="Rol Seçiniz" optionLabel="ad" optionValue="id" (onChange)="onRoleChange($event)"></p-select>
                    </div>

                    <!-- Patron için Firma Seçimi -->
                    <div class="field" *ngIf="user.role === 'patron'">
                        <label for="firma">Firma (Patronun Sahibi Olduğu)</label>
                        <p-select id="firma" [options]="firmalar" [(ngModel)]="user.istasyonId" optionLabel="ad" optionValue="id" placeholder="Firma Seçiniz" [showClear]="true"></p-select>
                        <small class="p-error" *ngIf="submitted && !user.istasyonId">Firma seçimi gereklidir.</small>
                    </div>

                    <!-- Diğer Roller için İstasyon Seçimi -->
                    <div class="field" *ngIf="user.role !== 'patron' && user.role !== 'admin' && !(isPatron && user.id === currentUserId)">
                        <label for="istasyon">İstasyon</label>
                        <p-select id="istasyon" [options]="istasyonlar" [(ngModel)]="user.istasyonId" optionLabel="ad" optionValue="id" placeholder="İstasyon Seçiniz" [showClear]="true"></p-select>
                        <small class="p-error" *ngIf="submitted && !user.istasyonId">İstasyon seçimi gereklidir.</small>
                    </div>
                </ng-template>

                <ng-template pTemplate="footer">
                    <button pButton label="İptal" icon="pi pi-times" class="p-button-text" (click)="hideDialog()"></button>
                    <button pButton label="Kaydet" icon="pi pi-check" class="p-button-text" (click)="saveUser()"></button>
                </ng-template>
            </p-dialog>
        </div>
    `,
    styles: [`
        .field { margin-bottom: 1rem; }
    `]
})
export class KullaniciYonetimiComponent implements OnInit {
    users: UserDto[] = [];
    user: any = {};
    istasyonlar: Istasyon[] = []; // Alt istasyonlar
    firmalar: Istasyon[] = [];    // Ana firmalar (parentIstasyonId == null)
    roles: any[] = [];
    dialogVisible: boolean = false;
    submitted: boolean = false;
    changePassword: boolean = false;
    isAdmin: boolean = false;
    isPatron: boolean = false;
    currentUserId: number | undefined;

    constructor(
        private userService: UserService,
        private yonetimService: YonetimService,
        private messageService: MessageService,
        private authService: AuthService,
        private roleService: RoleService
    ) { }

    ngOnInit() {
        const currentUser = this.authService.getCurrentUser();
        this.isAdmin = currentUser?.role === 'admin';
        this.isPatron = currentUser?.role === 'patron';
        this.currentUserId = currentUser?.id;

        this.loadRoles();
        this.loadUsers();
        this.loadIstasyonlar();
    }

    loadRoles() {
        this.roleService.getAll().subscribe(data => {
            if (this.isPatron) {
                // Patron can only assign non-admin/non-patron roles
                this.roles = data.filter(r => r.ad !== 'admin' && r.ad !== 'patron');
            } else {
                this.roles = data;
            }
        });
    }

    loadUsers() {
        this.userService.getUsers().subscribe(data => {
            this.users = data;
        });
    }

    loadIstasyonlar() {
        this.yonetimService.getIstasyonlar().subscribe(data => {
            // Firmalar: parentIstasyonId'si olmayanlar (Ana istasyonlar/Firmalar)
            this.firmalar = data.filter(i => i.parentIstasyonId == null);

            // İstasyonlar: parentIstasyonId'si olanlar (Alt istasyonlar)
            this.istasyonlar = data.filter(i => i.parentIstasyonId != null);
        });
    }

    openNew() {
        this.user = { roleId: null, role: '' };
        this.submitted = false;
        this.changePassword = false;
        this.dialogVisible = true;
    }

    editUser(user: UserDto) {
        this.user = { ...user };
        this.user.password = ''; // Don't show hash
        this.changePassword = false;
        this.dialogVisible = true;
    }

    deleteUser(user: UserDto) {
        if (confirm(user.username + ' kullanıcısını silmek istediğinize emin misiniz?')) {
            this.userService.deleteUser(user.id).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kullanıcı silindi' });
                    this.loadUsers();
                },
                error: () => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Silme işlemi başarısız' });
                }
            });
        }
    }

    hideDialog() {
        this.dialogVisible = false;
        this.submitted = false;
    }

    onRoleChange(event: any) {
        const selectedRole = this.roles.find(r => r.id === event.value);
        this.user.role = selectedRole?.ad;
        this.user.istasyonId = null;
    }

    saveUser() {
        this.submitted = true;

        // Admin rolü için istasyon seçimi zorunlu değil
        const isIstasyonRequired = this.user.role !== 'admin';

        if (this.user.username?.trim() && (this.user.id || this.user.password) && (!isIstasyonRequired || this.user.istasyonId)) {
            if (this.user.id) {
                const updateDto: UpdateUserDto = {
                    username: this.user.username,
                    roleId: this.user.roleId,
                    istasyonId: this.user.istasyonId,
                    adSoyad: this.user.adSoyad,
                    telefon: this.user.telefon
                };
                if (this.changePassword && this.user.password) {
                    updateDto.password = this.user.password;
                }

                this.userService.updateUser(this.user.id, updateDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kullanıcı güncellendi' });
                        this.loadUsers();
                        this.hideDialog();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Güncelleme başarısız' });
                    }
                });
            } else {
                const createDto: CreateUserDto = {
                    username: this.user.username,
                    password: this.user.password,
                    roleId: this.user.roleId,
                    istasyonId: this.user.istasyonId,
                    adSoyad: this.user.adSoyad,
                    telefon: this.user.telefon
                };
                this.userService.createUser(createDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kullanıcı oluşturuldu' });
                        this.loadUsers();
                        this.hideDialog();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error?.message || err.error || 'Oluşturma başarısız' });
                    }
                });
            }
        }
    }
}
