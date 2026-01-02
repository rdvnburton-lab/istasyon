import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PasswordModule } from 'primeng/password';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { UserService, UserDto, CreateUserDto, UpdateUserDto } from '../../../services/user.service';
import { IstasyonService, Istasyon } from '../../../services/istasyon.service';
import { FirmaService, Firma } from '../../../services/firma.service';
import { AuthService } from '../../../services/auth.service';
import { RoleService, Role } from '../../../services/role.service';

@Component({
    selector: 'app-kullanici-yonetimi',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        SelectModule,
        PasswordModule,
        TagModule,
        TooltipModule,
        ToastModule,
        IconFieldModule,
        InputIconModule
    ],
    providers: [MessageService],
    templateUrl: './kullanici-yonetimi.component.html',
    styleUrls: ['./kullanici-yonetimi.component.scss']
})
export class KullaniciYonetimiComponent implements OnInit {
    users: UserDto[] = [];
    user: any = {};
    istasyonlar: Istasyon[] = [];
    firmalar: Firma[] = [];
    roles: any[] = [];
    dialogVisible: boolean = false;
    submitted: boolean = false;
    changePassword: boolean = false;
    loading: boolean = false;
    isAdmin: boolean = false;
    isPatron: boolean = false;
    currentUserId: number | undefined;

    constructor(
        private userService: UserService,
        private istasyonService: IstasyonService,
        private firmaService: FirmaService,
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
                // Patron can only assign specific roles
                const allowedRoles = ['vardiya sorumlusu', 'market sorumlusu', 'istasyon sorumlusu', 'pompaci', 'market gorevlisi'];
                this.roles = data.filter(r => allowedRoles.includes(r.ad));
            } else {
                this.roles = data;
            }
        });
    }

    loadUsers() {
        this.loading = true;
        this.userService.getUsers().subscribe({
            next: (data) => {
                this.users = data;
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Kullanıcılar yüklenemedi' });
            }
        });
    }

    loadIstasyonlar() {
        if (this.isPatron) {
            // Patron sadece kendi istasyonlarını görebilmeli
            const currentUser = this.authService.getCurrentUser();
            if (currentUser && currentUser.istasyonlar) {
                this.istasyonlar = currentUser.istasyonlar.map(i => ({ id: i.id, ad: i.ad, aktif: true } as Istasyon));
            }
        } else {
            this.istasyonService.getIstasyonlar().subscribe(data => {
                this.istasyonlar = data;
            });

            this.firmaService.getFirmalar().subscribe(data => {
                this.firmalar = data;
            });
        }
    }

    openNew() {
        this.user = { roleId: null, role: '' };

        // Patron ise ve tek bir istasyonu varsa otomatik seç
        if (this.isPatron && this.istasyonlar.length === 1) {
            this.user.istasyonId = this.istasyonlar[0].id;
        }

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
            this.loading = true;
            this.userService.deleteUser(user.id).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kullanıcı silindi' });
                    this.loadUsers();
                },
                error: () => {
                    this.loading = false;
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

        // Patron değilse istasyonu sıfırla (Pasif rolü hariç)
        if (!this.isPatron && this.user.role?.toLowerCase() !== 'pasif') {
            this.user.istasyonId = null;
            this.user.firmaId = null;
        }
    }

    onFileSelect(event: any) {
        const file = event.target.files[0];
        if (file) {
            this.resizeImage(file, 300, 300).then(base64 => {
                this.user.fotografData = base64;
            });
        }
    }

    removePhoto() {
        this.user.fotografData = null;
    }

    resizeImage(file: File, maxWidth: number, maxHeight: number): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.readAsDataURL(file);
            reader.onload = (event: any) => {
                const img = new Image();
                img.src = event.target.result;
                img.onload = () => {
                    const canvas = document.createElement('canvas');
                    let width = img.width;
                    let height = img.height;

                    if (width > height) {
                        if (width > maxWidth) {
                            height *= maxWidth / width;
                            width = maxWidth;
                        }
                    } else {
                        if (height > maxHeight) {
                            width *= maxHeight / height;
                            height = maxHeight;
                        }
                    }

                    canvas.width = width;
                    canvas.height = height;
                    const ctx = canvas.getContext('2d');
                    ctx?.drawImage(img, 0, 0, width, height);
                    resolve(canvas.toDataURL('image/jpeg', 0.7));
                };
                img.onerror = (error) => reject(error);
            };
            reader.onerror = (error) => reject(error);
        });
    }

    saveUser() {
        this.submitted = true;

        // Admin rolü için istasyon seçimi zorunlu değil
        const isIstasyonRequired = this.user.role !== 'admin' && this.user.role !== 'patron';
        const isFirmaRequired = this.user.role === 'patron';

        if (this.user.username?.trim() && (this.user.id || this.user.password) &&
            (!isIstasyonRequired || this.user.istasyonId) &&
            (!isFirmaRequired || this.user.firmaId)) {

            this.loading = true;

            if (this.user.id) {
                const updateDto: UpdateUserDto = {
                    username: this.user.username,
                    roleId: this.user.roleId,
                    istasyonId: this.user.istasyonId,
                    firmaId: this.user.firmaId,
                    adSoyad: this.user.adSoyad,
                    telefon: this.user.telefon,
                    fotografData: this.user.fotografData
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
                        this.loading = false;
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Güncelleme başarısız' });
                    }
                });
            } else {
                const createDto: CreateUserDto = {
                    username: this.user.username,
                    password: this.user.password,
                    roleId: this.user.roleId,
                    istasyonId: this.user.istasyonId,
                    firmaId: this.user.firmaId,
                    adSoyad: this.user.adSoyad,
                    telefon: this.user.telefon,
                    fotografData: this.user.fotografData
                };
                this.userService.createUser(createDto).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Kullanıcı oluşturuldu' });
                        this.loadUsers();
                        this.hideDialog();
                    },
                    error: (err) => {
                        this.loading = false;
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error?.message || err.error || 'Oluşturma başarısız' });
                    }
                });
            }
        }
    }

    getRoleSeverity(role: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const roleMap: { [key: string]: 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' } = {
            'admin': 'danger',
            'patron': 'warn',
            'istasyon sorumlusu': 'info',
            'vardiya sorumlusu': 'success',
            'market sorumlusu': 'success',
            'pompaci': 'secondary',
            'market gorevlisi': 'secondary',
            'pasif': 'contrast'
        };
        return roleMap[role?.toLowerCase()] || 'secondary';
    }
}
