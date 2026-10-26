import { Routes } from '@angular/router';
import {
  sessionAuthGuard,
  sessionLoginGuard,
  sessionRootGuard,
  userAdminGuard
} from './core/auth/demo-auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [sessionLoginGuard],
    loadComponent: () =>
      import('./features/auth/pages/login/login').then(component => component.LoginPage)
  },
  {
    path: 'activate-invitation',
    loadComponent: () =>
      import('./features/auth/pages/activate-invitation/activate-invitation')
        .then(component => component.ActivateInvitationPage)
  },
  {
    path: 'users',
    canActivate: [userAdminGuard],
    loadComponent: () =>
      import(
        './features/user-management/pages/user-list/user-list'
      ).then(component => component.UserListComponent)
  },
  {
    path: 'profile',
    canActivate: [sessionAuthGuard],
    loadComponent: () =>
      import('./features/auth/pages/profile/profile').then(component => component.ProfilePage)
  },
  {
    path: 'unauthorized',
    loadComponent: () =>
      import('./features/auth/pages/unauthorized/unauthorized').then(
        component => component.UnauthorizedPage
      )
  },
  {
    path: 'home',
    canActivate: [sessionAuthGuard],
    loadComponent: () =>
      import('./features/auth/pages/portal-home/portal-home').then(
        component => component.PortalHomePage
      )
  },
  {
    path: '',
    canActivate: [sessionRootGuard],
    pathMatch: 'full',
    loadComponent: () =>
      import('./features/auth/pages/portal-home/portal-home').then(
        component => component.PortalHomePage
      )
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
