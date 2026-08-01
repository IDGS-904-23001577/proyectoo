import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  signal
} from '@angular/core';

import { Router } from '@angular/router';

import {
  EMPTY,
  Observable,
  Subscription,
  catchError,
  exhaustMap,
  finalize,
  tap,
  timer
} from 'rxjs';

import { Dashboard } from '../models/dashboard';
import { DashboardService } from '../services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'],
  standalone: false
})
export class DashboardComponent implements OnInit, OnDestroy {
  dashboard = signal<Dashboard | null>(null);

  cargando = signal(true);

  mensajeError = signal('');

  readonly intervaloActualizacionMs = 15000;

  private actualizacionSubscription?: Subscription;

  readonly maximoFugasSemanales = computed(() => {
    const datos = this.dashboard()?.fugasSemanales ?? [];

    return Math.max(1, ...datos.map(dia => dia.cantidad));
  });

  constructor(
    private router: Router,
    private dashboardService: DashboardService
  ) { }

  ngOnInit(): void {
    const sesion = localStorage.getItem('sesionSIAF');

    if (!sesion) {
      this.router.navigate(['/login']);
      return;
    }

    this.iniciarActualizacionAutomatica();
  }

  ngOnDestroy(): void {
    this.actualizacionSubscription?.unsubscribe();
  }

  private iniciarActualizacionAutomatica(): void {
    this.actualizacionSubscription = timer(
      0,
      this.intervaloActualizacionMs
    )
      .pipe(
        exhaustMap(() => {
          const esPrimeraCarga = this.dashboard() === null;

          return this.consultarDashboard(esPrimeraCarga);
        })
      )
      .subscribe();
  }

  private consultarDashboard(
    mostrarCarga: boolean
  ): Observable<Dashboard> {
    if (mostrarCarga) {
      this.cargando.set(true);
    }

    return this.dashboardService.obtenerDashboard()
      .pipe(
        tap(respuesta => {
          this.dashboard.set(respuesta);
          this.mensajeError.set('');
        }),

        catchError(error => {
          console.error(
            'Error al obtener el panel de control:',
            error
          );

          if (this.dashboard() === null) {
            if (error.status === 503) {
              this.mensajeError.set(
                'No fue posible consultar la base de datos.'
              );
            } else {
              this.mensajeError.set(
                'No fue posible obtener la información del panel.'
              );
            }
          }

          return EMPTY;
        }),

        finalize(() => {
          if (mostrarCarga) {
            this.cargando.set(false);
          }
        })
      );
  }

  alturaBarraPorcentaje(cantidad: number): number {
    const maximo = this.maximoFugasSemanales();

    return maximo === 0 ? 0 : (cantidad / maximo) * 100;
  }

  claseSeveridad(severidad: string): string {
    switch (severidad.trim().toLowerCase()) {
      case 'alta':
        return 'alta';

      case 'media':
        return 'media';

      case 'baja':
        return 'baja';

      default:
        return 'sin-definir';
    }
  }

  claseRiesgo(riesgo: string): string {
    switch (riesgo.trim().toUpperCase()) {
      case 'ALTO':
        return 'alto';

      case 'MEDIO':
        return 'medio';

      default:
        return 'bajo';
    }
  }
}
