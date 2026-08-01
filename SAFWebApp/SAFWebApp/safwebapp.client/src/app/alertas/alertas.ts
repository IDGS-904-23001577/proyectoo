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
  selector: 'app-alertas',
  templateUrl: './alertas.html',
  styleUrls: ['./alertas.css'],
  standalone: false
})
export class AlertasComponent implements OnInit {
  alertas = signal<Alerta[]>([]);
  cargando = signal(true);
  errorMessage = signal('');

  busqueda = signal('');
  filtroSeveridad = signal('Todas');
  filtroEstado = signal('Todos');

  fechaActual = new Intl.DateTimeFormat('es-MX', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date());

  cantidadAltas = computed(() =>
    this.alertas().filter(
      alerta => this.normalizar(alerta.severidad) === 'alta'
    ).length
  );

  cantidadMedias = computed(() =>
    this.alertas().filter(
      alerta => this.normalizar(alerta.severidad) === 'media'
    ).length
  );

  cantidadBajas = computed(() =>
    this.alertas().filter(
      alerta => this.normalizar(alerta.severidad) === 'baja'
    ).length
  );

  cantidadActivas = computed(() =>
    this.alertas().filter(
      alerta => this.normalizar(alerta.estado) === 'activa'
    ).length
  );

  alertasFiltradas = computed(() => {
    const texto = this.normalizar(this.busqueda());
    const severidad = this.normalizar(
      this.filtroSeveridad()
    );
    const estado = this.normalizar(
      this.filtroEstado()
    );

    return this.alertas().filter(alerta => {
      const coincideBusqueda =
        !texto ||
        this.normalizar(alerta.seccion).includes(texto) ||
        this.normalizar(alerta.severidad).includes(texto) ||
        this.normalizar(alerta.estado).includes(texto);

      const coincideSeveridad =
        severidad === 'todas' ||
        this.normalizar(alerta.severidad) === severidad;

      const coincideEstado =
        estado === 'todos' ||
        this.normalizar(alerta.estado) === estado;

      return coincideBusqueda &&
        coincideSeveridad &&
        coincideEstado;
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

    this.cargarAlertas();
  }

  cargarAlertas(): void {
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
          this.alertas.set(respuesta);
        },
        error: (error) => {
          console.error(
            'Error al consultar las alertas:',
            error
          );

          if (error.status === 503) {
            this.errorMessage.set(
              'No fue posible consultar la base de datos'
            );
          } else {
            this.errorMessage.set(
              'No se pudieron cargar las alertas'
            );
          }
        }
      });
  }

  seleccionarSeveridad(severidad: string): void {
    this.filtroSeveridad.set(severidad);
  }

  seleccionarEstado(estado: string): void {
    this.filtroEstado.set(estado);
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

  descripcionAlerta(alerta: Alerta): string {
    const volumen =
      alerta.volumenPerdido &&
        alerta.volumenPerdido !== 'null'
        ? alerta.volumenPerdido
        : '0';

    if (
      this.normalizar(alerta.estado) === 'resuelta' &&
      alerta.duracionHoras &&
      alerta.duracionHoras !== 'null'
    ) {
      return `Volumen perdido: ${volumen} L · Duración: ${alerta.duracionHoras} h`;
    }

    return `Volumen perdido: ${volumen} L`;
  }

  formatearFecha(fechaTexto: string): string {
    if (!fechaTexto) {
      return 'Sin fecha';
    }

    const fecha = this.convertirFecha(fechaTexto);

    if (Number.isNaN(fecha.getTime())) {
      return fechaTexto;
    }

    return new Intl.DateTimeFormat('es-MX', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    }).format(fecha);
  }

  tiempoTranscurrido(fechaTexto: string): string {
    if (!fechaTexto) {
      return 'Sin fecha';
    }

    const fecha = this.convertirFecha(fechaTexto);
    const ahora = new Date();

    const diferencia =
      ahora.getTime() - fecha.getTime();

    if (
      Number.isNaN(fecha.getTime()) ||
      diferencia < 0
    ) {
      return this.formatearFecha(fechaTexto);
    }

    const minutos = Math.floor(
      diferencia / (1000 * 60)
    );

    const horas = Math.floor(
      diferencia / (1000 * 60 * 60)
    );

    const dias = Math.floor(
      diferencia / (1000 * 60 * 60 * 24)
    );

    if (minutos < 1) {
      return 'Ahora';
    }

    if (minutos < 60) {
      return `Hace ${minutos} min`;
    }

    if (horas < 24) {
      return `Hace ${horas} h`;
    }

    if (dias < 7) {
      return `Hace ${dias} días`;
    }

    return this.formatearFecha(fechaTexto);
  }

  private convertirFecha(fechaTexto: string): Date {
    return new Date(
      fechaTexto.replace(' ', 'T')
    );
  }

  private normalizar(valor: string): string {
    return (valor ?? '')
      .trim()
      .toLowerCase();
  }
}
