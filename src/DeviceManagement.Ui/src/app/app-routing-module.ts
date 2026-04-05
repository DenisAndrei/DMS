import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { InventoryPageComponent } from './features/inventory/inventory-page.component';

const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    component: LoginPageComponent
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    component: RegisterPageComponent
  },
  {
    path: 'inventory',
    pathMatch: 'full',
    redirectTo: ''
  },
  {
    path: '',
    canActivate: [authGuard],
    component: InventoryPageComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
