import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';
import { PermissionService } from './permission.service';
import { MessageService } from 'primeng/api';

export const roleGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const permissionService = inject(PermissionService);
    const router = inject(Router);
    const messageService = inject(MessageService);

    const user = authService.getCurrentUser();

    if (!user) {
        return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
    }

    const requiredResource = route.data['resource'] as string;

    console.log('RoleGuard Check:', {
        path: route.url[0]?.path,
        resource: requiredResource,
        userRole: user.role,
        hasAccess: permissionService.hasAccess(user.role, requiredResource)
    });

    // Eğer rota için kaynak tanımlanmamışsa, erişime izin ver
    if (!requiredResource) {
        return true;
    }

    const userRole = user.role;

    if (permissionService.hasAccess(userRole, requiredResource)) {
        return true;
    }

    // Yetkisiz erişim
    messageService.add({
        severity: 'error',
        summary: 'Yetkisiz Erişim',
        detail: 'Bu sayfaya erişim yetkiniz bulunmamaktadır.'
    });

    return router.createUrlTree(['/dashboard']);
};
