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

import {
  EstadoNodoRed,
  NodoRed,
  RedDistribucion
} from '../models/red-distribucion';

import {
  RedService
} from '../services/red.service';

type FiltroRed =
  | 'Todas'
  | 'Normal'
  | 'Advertencia'
  | 'Fuga';

interface FiltroRedOpcion {
  valor: FiltroRed;
  etiqueta: string;
}

interface ConexionVisual {
  origen: string;
  destino: string;
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

@Component({
  selector: 'app-red',
  templateUrl: './red.html',
  styleUrls: ['./red.css'],
  standalone: false
})
export class RedComponent implements OnInit, OnDestroy {
  red = signal<RedDistribucion | null>(null);

  cargando = signal(true);

  mensajeError = signal('');

  filtroActivo = signal<FiltroRed>('Todas');

  readonly intervaloActualizacionMs = 3000;

  private actualizacionSubscription?: Subscription;

  readonly filtros: FiltroRedOpcion[] = [
    {
      valor: 'Todas',
      etiqueta: 'Todas'
    },
    {
      valor: 'Normal',
      etiqueta: 'Normal'
    },
    {
      valor: 'Advertencia',
      etiqueta: 'Advertencia'
    },
    {
      valor: 'Fuga',
      etiqueta: 'Fugas'
    }
  ];

  readonly conexionesVisuales = computed<ConexionVisual[]>(() => {
    const mapa = this.red();

    if (!mapa) {
      return [];
    }

    const nodosPorId = new Map(
      mapa.nodos.map(nodo => [
        nodo.id,
        nodo
      ])
    );

    return mapa.conexiones.flatMap(conexion => {
      const nodoOrigen = nodosPorId.get(conexion.origen);
      const nodoDestino = nodosPorId.get(conexion.destino);

      if (!nodoOrigen || !nodoDestino) {
        return [];
      }

      return [
        {
          origen: conexion.origen,
          destino: conexion.destino,
          x1: nodoOrigen.posicionX,
          y1: nodoOrigen.posicionY,
          x2: nodoDestino.posicionX,
          y2: nodoDestino.posicionY
        }
      ];
    });
  });

  constructor(
    private router: Router,
    private redService: RedService
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
          const esPrimeraCarga = this.red() === null;

          return this.consultarRed(esPrimeraCarga);
        })
      )
      .subscribe();
  }

  cargarRed(): void {
    const mostrarCarga = this.red() === null;

    this.consultarRed(mostrarCarga).subscribe();
  }

  private consultarRed(
    mostrarCarga: boolean
  ): Observable<RedDistribucion> {
    if (mostrarCarga) {
      this.cargando.set(true);
    }

    return this.redService.obtenerRed()
      .pipe(
        tap(respuesta => {
          this.red.set(respuesta);
          this.mensajeError.set('');
        }),

        catchError(error => {
          console.error(
            'Error al obtener el mapa de distribución:',
            error
          );

          if (this.red() === null) {
            if (error.status === 503) {
              this.mensajeError.set(
                'No fue posible consultar la base de datos.'
              );
            } else {
              this.mensajeError.set(
                'No fue posible obtener la información de la red.'
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

  seleccionarFiltro(filtro: FiltroRed): void {
    this.filtroActivo.set(filtro);
  }

  nodoCoincideFiltro(nodo: NodoRed): boolean {
    const filtro = this.filtroActivo();

    return filtro === 'Todas' || nodo.estado === filtro;
  }

  obtenerClaseEstado(estado: EstadoNodoRed): string {
    switch (estado) {
      case 'Fuga':
        return 'nodo-fuga';

      case 'Advertencia':
        return 'nodo-advertencia';

      default:
        return 'nodo-normal';
    }
  }

  obtenerTextoEstado(estado: EstadoNodoRed): string {
    switch (estado) {
      case 'Fuga':
        return 'Fuga activa';

      case 'Advertencia':
        return 'Advertencia';

      default:
        return 'Normal';
    }
  }

  obtenerDetalleNodo(nodo: NodoRed): string {
    if (nodo.estado === 'Normal') {
      return `${nodo.etiqueta}: operación normal`;
    }

    const seccion =
      nodo.seccionAfectada || nodo.etiqueta;

    const severidad =
      nodo.severidad || 'Sin definir';

    return `${this.obtenerTextoEstado(
      nodo.estado
    )} en ${seccion}. Severidad: ${severidad}`;
  }
}
