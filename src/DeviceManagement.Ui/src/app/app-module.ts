import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { AuthInterceptor } from './core/http/auth.interceptor';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { DeviceDetailsPanelComponent } from './features/inventory/device-details-panel.component';
import { DeviceFormPanelComponent } from './features/inventory/device-form-panel.component';
import { DeviceListPanelComponent } from './features/inventory/device-list-panel.component';
import { InventoryPageComponent } from './features/inventory/inventory-page.component';
import { InventorySearchComponent } from './features/inventory/inventory-search.component';

@NgModule({
  declarations: [
    App,
    LoginPageComponent,
    RegisterPageComponent,
    InventorySearchComponent,
    DeviceListPanelComponent,
    DeviceFormPanelComponent,
    DeviceDetailsPanelComponent,
    InventoryPageComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    ReactiveFormsModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ],
  bootstrap: [App]
})
export class AppModule { }
