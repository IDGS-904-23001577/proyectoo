import { HttpClientModule } from '@angular/common/http';

import {
  NgModule,
  provideBrowserGlobalErrorListeners
} from '@angular/core';

import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing-module';

import { App } from './app';
import { LoginComponent } from './login/login';
import { InicioComponent } from './inicio/inicio';
import { DashboardComponent } from './dashboard/dashboard';
import { EnVivoComponent } from './en-vivo/en-vivo';
import { PrediccionesComponent } from './predicciones/predicciones';
import { AlertasComponent } from './alertas/alertas';
import { HistorialComponent } from './historial/historial';
import { RedComponent } from './red/red';
import { InformesComponent } from './informes/informes';
import { LayoutComponent } from './layout/layout';
import { ValvulasComponent } from './valvulas/valvulas';
import { ApiComponent } from './apii/api';


@NgModule({
  declarations: [
    App,
    LoginComponent,
    InicioComponent,
    DashboardComponent,
    EnVivoComponent,
    PrediccionesComponent,
    AlertasComponent,
    HistorialComponent,
    RedComponent,
    InformesComponent,
    LayoutComponent,
    ValvulasComponent,
    ApiComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners()
  ],
  bootstrap: [
    App
  ]
})
export class AppModule { }