import { NgModule } from '@angular/core';

import {
  RouterModule,
  Routes
} from '@angular/router';

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

const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    component: LayoutComponent,
    children: [
      {
        path: 'inicio',
        component: InicioComponent
      },
      {
        path: 'dashboard',
        component: DashboardComponent
      },
      {
        path: 'en-vivo',
        component: EnVivoComponent
      },
      {
        path: 'predicciones',
        component: PrediccionesComponent
      },
      {
        path: 'red',
        component: RedComponent
      },
      {
        path: 'alertas',
        component: AlertasComponent
      },
      {
        path: 'historial',
        component: HistorialComponent
      },
      {
        path: 'informes',
        component: InformesComponent
      },
      {
        path: 'valvulas',
        component: ValvulasComponent
      },
      {
        path: 'api',
        component: ApiComponent
      },
      {
        path: '',
        redirectTo: 'inicio',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/login'
  }
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes)
  ],
  exports: [
    RouterModule
  ]
})
export class AppRoutingModule { }