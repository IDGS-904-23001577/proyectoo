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
  Subscription,
  catchError,
  finalize,
  tap
} from 'rxjs';

import {
  Prediccion,
  PrediccionHistorialPunto
} from '../models/prediccion';

import { PrediccionesService } from '../services/predicciones.service';

@Component({
  selector: 'app-predicciones',
  templateUrl: './predicciones.html',
  styleUrls: ['./predicciones.css'],
  standalone: false
})
export class PrediccionesComponent implements OnInit, OnDestroy {
  predicciones = signal<Prediccion[]>([]);

  historial = signal<PrediccionHistorialPunto[]>([]);

  seccionSeleccionada = signal('Entrada');

  diasSeleccionados = signal(30);

  cargando = signal(true);

  cargandoHistorial = signal(false);

  mensajeError = signal('');

  readonly opcionesDias = [7, 30, 90];

  readonly opcionesSeccion = [
    { valor: 'Entrada', etiqueta: 'Entrada' },
    { valor: 'Tramo_Izquierdo', etiqueta: 'Tramo Izquierdo' },
    { valor: 'Tramo_Derecho', etiqueta: 'Tramo Derecho' },
    { valor: 'Parte_Abajo', etiqueta: 'Parte Abajo' },
    { valor: 'Salida', etiqueta: 'Salida' }
  ];

  private prediccionesSubscription?: Subscription;

  private historialSubscription?: Subscription;

  readonly puntosPresion = computed(() =>
    this.calcularPuntos(this.historial().map(p => p.presionPromedio))
  );

  readonly puntosCaudal = computed(() =>
    this.calcularPuntos(this.historial().map(p => p.caudalPromedio))
  );

  constructor(
    private router: Router,
    private prediccionesService: PrediccionesService
  ) { }

  ngOnInit(): void {
    const sesion = localStorage.getItem('sesionSIAF');

    if (!sesion) {
      this.router.navigate(['/login']);
      return;
    }

    this.cargarPredicciones();
    this.cargarHistorial();
  }

  ngOnDestroy(): void {
    this.prediccionesSubscription?.unsubscribe();
    this.historialSubscription?.unsubscribe();
  }

  cambiarSeccion(seccion: string): void {
    this.seccionSeleccionada.set(seccion);
    this.cargarHistorial();
  }

  cambiarDias(dias: number): void {
    this.diasSeleccionados.set(dias);
    this.cargarHistorial();
  }

  claseRiesgo(riesgo: string): string {
    switch (riesgo.trim().toUpperCase()) {
      case 'ALTO':
        return 'alto';

      case 'MEDIO':
        return 'medio';

      case 'BAJO':
        return 'bajo';

      default:
        return 'sin-definir';
    }
  }

  private cargarPredicciones(): void {
    this.cargando.set(true);

    this.prediccionesSubscription = this.prediccionesService
      .obtenerPredicciones()
      .pipe(
        tap(respuesta => {
          this.predicciones.set(respuesta);
          this.mensajeError.set('');
        }),

        catchError(error => {
          console.error('Error al obtener predicciones:', error);

          this.mensajeError.set(
            error.status === 503
              ? 'No fue posible consultar la base de datos.'
              : 'No fue posible obtener las predicciones.'
          );

          return EMPTY;
        }),

        finalize(() => this.cargando.set(false))
      )
      .subscribe();
  }

  private cargarHistorial(): void {
    this.cargandoHistorial.set(true);

    this.historialSubscription = this.prediccionesService
      .obtenerHistorial(
        this.seccionSeleccionada(),
        this.diasSeleccionados()
      )
      .pipe(
        tap(respuesta => this.historial.set(respuesta)),

        catchError(error => {
          console.error('Error al obtener histórico:', error);
          return EMPTY;
        }),

        finalize(() => this.cargandoHistorial.set(false))
      )
      .subscribe();
  }

  private calcularPuntos(valores: number[]): string {
    if (valores.length === 0) {
      return '';
    }

    const ancho = 600;
    const alto = 220;
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