import {
  Component,
  OnInit,
  computed,
  signal
} from '@angular/core';

import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { Alerta } from '../models/alerta';
import { AlertasService } from '../services/alertas.service';

@Component({
  selector: 'app-historial',
  templateUrl: './historial.html',
  styleUrls: ['./historial.css'],
  standalone: false
})
export class HistorialComponent implements OnInit {
  eventos = signal<Alerta[]>([]);
  cargando = signal(true);
  errorMessage = signal('');

  tipoOrden = signal<'fecha' | 'severidad'>('fecha');

  eventosOrdenados = computed(() => {
    const copia = [...this.eventos()];

    if (this.tipoOrden() === 'severidad') {
      return copia.sort((a, b) => {
        return this.valorSeveridad(a.severidad) -
          this.valorSeveridad(b.severidad);
      });
    }

    return copia.sort((a, b) => {
      const fechaA = this.convertirFecha(
        a.fechaDeteccion
      ).getTime();

      const fechaB = this.convertirFecha(
        b.fechaDeteccion
      ).getTime();

      return fechaB - fechaA;
    });
  });

  constructor(
    private alertasService: AlertasService,
    private router: Router
  ) { }

  ngOnInit(): void {
    const sesion = localStorage.getItem('sesionSIAF');

    if (!sesion) {
      this.router.navigate(['/login']);
      return;
    }

    this.cargarHistorial();
  }

  cargarHistorial(): void {
    this.cargando.set(true);
    this.errorMessage.set('');

    this.alertasService.obtenerAlertas()
      .pipe(
        finalize(() => {
          this.cargando.set(false);
        })
      )
      .subscribe({
        next: (respuesta) => {
          this.eventos.set(respuesta);
        },
        error: (error) => {
          console.error(
            'Error al consultar el historial:',
            error
          );

          if (error.status === 503) {
            this.errorMessage.set(
              'No fue posible consultar la base de datos'
            );
          } else {
            this.errorMessage.set(
              'No se pudo cargar el historial'
            );
          }
        }
      });
  }

  ordenarPorFecha(): void {
    this.tipoOrden.set('fecha');
  }

  ordenarPorSeveridad(): void {
    this.tipoOrden.set('severidad');
  }

  claseSeveridad(severidad: string): string {
    switch (this.normalizar(severidad)) {
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

  claseEstado(estado: string): string {
    switch (this.normalizar(estado)) {
      case 'activa':
        return 'activa';

      case 'pendiente':
        return 'pendiente';

      case 'resuelta':
        return 'resuelta';

      default:
        return 'desconocido';
    }
  }

  obtenerFecha(fechaTexto: string): string {
    if (!fechaTexto) {
      return 'Sin fecha';
    }

    const fecha = this.convertirFecha(fechaTexto);

    if (Number.isNaN(fecha.getTime())) {
      return fechaTexto;
    }

    return new Intl.DateTimeFormat('es-MX', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    }).format(fecha);
  }

  obtenerHora(fechaTexto: string): string {
    if (!fechaTexto) {
      return '';
    }

    const fecha = this.convertirFecha(fechaTexto);

    if (Number.isNaN(fecha.getTime())) {
      return '';
    }

    return new Intl.DateTimeFormat('es-MX', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false
    }).format(fecha);
  }

  obtenerDuracion(evento: Alerta): string {
    const duracionGuardada = Number(
      evento.duracionHoras
    );

    if (
      evento.duracionHoras &&
      evento.duracionHoras !== 'null' &&
      !Number.isNaN(duracionGuardada)
    ) {
      return `${this.formatearNumero(
        duracionGuardada
      )} h`;
    }

    const fechaDeteccion = this.convertirFecha(
      evento.fechaDeteccion
    );

    if (Number.isNaN(fechaDeteccion.getTime())) {
      return 'Sin datos';
    }

    const diferencia =
      new Date().getTime() -
      fechaDeteccion.getTime();

    if (diferencia < 0) {
      return '0 h';
    }

    const horas =
      diferencia / (1000 * 60 * 60);

    return `${this.formatearNumero(horas)} h`;
  }

  obtenerVolumen(volumenTexto: string): string {
    const volumen = Number(volumenTexto);

    if (Number.isNaN(volumen)) {
      return `${volumenTexto || '0'} L`;
    }

    return `${new Intl.NumberFormat('es-MX', {
      maximumFractionDigits: 2
    }).format(volumen)} L`;
  }

  exportarCsv(): void {
    const encabezados = [
      'Fecha',
      'Hora',
      'Sección',
      'Tipo',
      'Duración',
      'Volumen perdido',
      'Severidad',
      'Estado'
    ];

    const filas = this.eventosOrdenados().map(
      evento => [
        this.obtenerFecha(evento.fechaDeteccion),
        this.obtenerHora(evento.fechaDeteccion),
        evento.seccion,
        'Fuga de tubería',
        this.obtenerDuracion(evento),
        this.obtenerVolumen(evento.volumenPerdido),
        evento.severidad,
        evento.estado
      ]
    );

    const contenido = [
      encabezados,
      ...filas
    ]
      .map(fila =>
        fila
          .map(valor =>
            `"${String(valor).replace(/"/g, '""')}"`
          )
          .join(',')
      )
      .join('\n');

    const archivo = new Blob(
      ['\uFEFF' + contenido],
      {
        type: 'text/csv;charset=utf-8;'
      }
    );

    const url = URL.createObjectURL(archivo);

    const enlace = document.createElement('a');

    enlace.href = url;
    enlace.download =
      `historial-saf-${this.fechaArchivo()}.csv`;

    enlace.click();

    URL.revokeObjectURL(url);
  }

  private fechaArchivo(): string {
    const fecha = new Date();

    const anio = fecha.getFullYear();

    const mes = String(
      fecha.getMonth() + 1
    ).padStart(2, '0');

    const dia = String(
      fecha.getDate()
    ).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }

  private convertirFecha(fechaTexto: string): Date {
    return new Date(
      fechaTexto.replace(' ', 'T')
    );
  }

  private valorSeveridad(severidad: string): number {
    switch (this.normalizar(severidad)) {
      case 'alta':
        return 1;

      case 'media':
        return 2;

      case 'baja':
        return 3;

      default:
        return 4;
    }
  }

  private formatearNumero(numero: number): string {
    return new Intl.NumberFormat('es-MX', {
      minimumFractionDigits: 1,
      maximumFractionDigits: 2
    }).format(numero);
  }

  private normalizar(valor: string): string {
    return (valor ?? '')
      .trim()
      .toLowerCase();
  }
}
