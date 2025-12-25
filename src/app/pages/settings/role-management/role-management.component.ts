import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { RoleService, Role } from '../../../services/role.service';

@Component({
    selector: 'app-role-management',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        TableModule,
        ButtonModule,
        DialogModule,
        InputTextModule,
        TextareaModule,
        ToastModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './role-management.component.html',
    styleUrl: './role-management.component.scss'
})
export class RoleManagementComponent implements OnInit {
    roles: Role[] = [];
    roleDialog: boolean = false;
    roleForm: FormGroup;
    submitted: boolean = false;
    dialogHeader: string = '';
    isEditMode: boolean = false;
    currentRoleId: number | null = null;

    constructor(
        private roleService: RoleService,
        private messageService: MessageService,
        private confirmationService: ConfirmationService,
        private fb: FormBuilder
    ) {
        this.roleForm = this.fb.group({
            ad: ['', Validators.required],
            aciklama: ['']
        });
    }

    ngOnInit() {
        this.loadRoles();
    }

    loadRoles() {
        this.roleService.getAll().subscribe({
            next: (data) => {
                this.roles = data;
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Hata', detail: 'Roller yüklenirken bir hata oluştu.' });
            }
        });
    }

    openNew() {
        this.roleForm.reset();
        this.submitted = false;
        this.roleDialog = true;
        this.dialogHeader = 'Yeni Rol Ekle';
        this.isEditMode = false;
        this.currentRoleId = null;
    }

    editRole(role: Role) {
        this.roleForm.patchValue({
            ad: role.ad,
            aciklama: role.aciklama
        });
        this.roleDialog = true;
        this.dialogHeader = 'Rol Düzenle';
        this.isEditMode = true;
        this.currentRoleId = role.id;
    }

    deleteRole(role: Role) {
        this.confirmationService.confirm({
            message: `'${role.ad}' rolünü silmek istediğinize emin misiniz?`,
            header: 'Silme Onayı',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.roleService.delete(role.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Rol silindi.' });
                        this.loadRoles();
                    },
                    error: (err) => {
                        this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Rol silinemedi.' });
                    }
                });
            }
        });
    }

    saveRole() {
        this.submitted = true;

        if (this.roleForm.invalid) {
            return;
        }

        const roleData = this.roleForm.value;

        if (this.isEditMode && this.currentRoleId) {
            this.roleService.update(this.currentRoleId, roleData).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Rol güncellendi.' });
                    this.roleDialog = false;
                    this.loadRoles();
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Rol güncellenemedi.' });
                }
            });
        } else {
            this.roleService.create(roleData).subscribe({
                next: () => {
                    this.messageService.add({ severity: 'success', summary: 'Başarılı', detail: 'Rol oluşturuldu.' });
                    this.roleDialog = false;
                    this.loadRoles();
                },
                error: (err) => {
                    this.messageService.add({ severity: 'error', summary: 'Hata', detail: err.error || 'Rol oluşturulamadı.' });
                }
            });
        }
    }

    hideDialog() {
        this.roleDialog = false;
        this.submitted = false;
    }
}
