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
  forkJoin,
  tap,
  timer
} from 'rxjs';

import { LecturaActual, LecturaHistorialPunto } from '../models/lectura';
import { LecturasService } from '../services/lecturas.service';

interface RespuestaConsulta {
  actual: LecturaActual[];
  historial: LecturaHistorialPunto[];
}

@Component({
  selector: 'app-en-vivo',
  templateUrl: './en-vivo.html',
  styleUrls: ['./en-vivo.css'],
  standalone: false
})
export class EnVivoComponent implements OnInit, OnDestroy {
  secciones = signal<LecturaActual[]>([]);

  historial = signal<LecturaHistorialPunto[]>([]);

  seccionSeleccionada = signal('Entrada');

  cargando = signal(true);

  mensajeError = signal('');

  ultimaActualizacion = signal<Date | null>(null);

  readonly intervaloActualizacionMs = 4000;

  readonly minutosHistorial = 15;

  private actualizacionSubscription?: Subscription;

  readonly seccionActual = computed(() => {
    return this.secciones().find(
      s => s.seccion === this.seccionSeleccionada()
    ) ?? null;
  });

  readonly puntosPresion = computed(() =>
    this.calcularPuntos(this.historial().map(p => p.presionBar))
  );

  readonly puntosCaudal = computed(() =>
    this.calcularPuntos(this.historial().map(p => p.caudalLmin))
  );

  constructor(
    private router: Router,
    private lecturasService: LecturasService
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

  seleccionarSeccion(seccion: string): void {
    if (seccion === this.seccionSeleccionada()) {
      return;
    }

    this.seccionSeleccionada.set(seccion);

    this.lecturasService
      .obtenerHistorial(seccion, this.minutosHistorial)
      .subscribe({
        next: puntos => this.historial.set(puntos),
        error: error => console.error(
          'Error al obtener historial:',
          error
        )
      });
  }

  claseEstado(estado: string): string {
    switch (estado.trim().toLowerCase()) {
      case 'fuga':
        return 'fuga';

      case 'advertencia':
        return 'advertencia';

      default:
        return 'normal';
    }
  }

  private iniciarActualizacionAutomatica(): void {
    this.actualizacionSubscription = timer(
      0,
      this.intervaloActualizacionMs
    )
      .pipe(
        exhaustMap(() => {
          const esPrimeraCarga = this.secciones().length === 0;

          return this.consultarTodo(esPrimeraCarga);
        })
      )
      .subscribe();
  }

  private consultarTodo(
    mostrarCarga: boolean
  ): Observable<RespuestaConsulta> {
    if (mostrarCarga) {
      this.cargando.set(true);
    }

    return forkJoin({
      actual: this.lecturasService.obtenerActual(),
      historial: this.lecturasService.obtenerHistorial(
        this.seccionSeleccionada(),
        this.minutosHistorial
      )
    }).pipe(
      tap(respuesta => {
        this.secciones.set(respuesta.actual);
        this.historial.set(respuesta.historial);
        this.mensajeError.set('');
        this.ultimaActualizacion.set(new Date());
      }),

      catchError(error => {
        console.error('Error al obtener lecturas en vivo:', error);

        if (this.secciones().length === 0) {
          this.mensajeError.set(
            error.status === 503
              ? 'No fue posible consultar la base de datos.'
              : 'No fue posible obtener las lecturas en vivo.'
          );
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

  private calcularPuntos(valores: number[]): string {
    if (valores.length === 0) {
      return '';
    }

    const ancho = 600;
    const alto = 160;
    const minimo = Math.min(...valores);
    const maximo = Math.max(...valores);
    const rango = maximo - minimo || 1;

    return valores
      .map((valor, indice) => {
        const x = valores.length === 1
          ? ancho / 2
          : (indice / (valores.length - 1)) * ancho;

        const y = alto - ((valor - minimo) / rango) * alto;

        return `${x.toFixed(1)},${y.toFixed(1)}`;
      })
      .join(' ');
  }
}