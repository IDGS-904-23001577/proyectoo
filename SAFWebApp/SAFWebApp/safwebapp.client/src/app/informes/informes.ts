import {
  Component,
  OnInit,
  signal
} from '@angular/core';

import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import {
  CatalogoInformes,
  FiltrosInforme,
  InformeGenerado,
  RegistrarInformeGenerado,
  VistaPreviaInforme
} from '../models/informe';

import {
  InformesService
} from '../services/informes.service';

@Component({
  selector: 'app-informes',
  templateUrl: './informes.html',
  styleUrls: ['./informes.css'],
  standalone: false
})
export class InformesComponent implements OnInit {
  catalogos =
    signal<CatalogoInformes | null>(null);

  vistaPrevia =
    signal<VistaPreviaInforme | null>(null);

  informesRecientes =
    signal<InformeGenerado[]>([]);

  cargandoCatalogos = signal(true);

  cargandoVistaPrevia = signal(false);

  cargandoInformesRecientes = signal(false);

  generandoPdf = signal(false);

  descargandoInformeId =
    signal<number | null>(null);

  mensajeError = signal('');

  mensajeExito = signal('');

  mensajeErrorRecientes = signal('');

  tipoInforme = 'Completo';

  seccion = 'Todas';

  fechaInicio = '';

  fechaFin = '';

  constructor(
    private router: Router,
    private informesService: InformesService
  ) {
    const fechaActual = new Date();

    const primerDiaMes = new Date(
      fechaActual.getFullYear(),
      fechaActual.getMonth(),
      1
    );

    this.fechaInicio =
      this.formatearFechaInput(primerDiaMes);

    this.fechaFin =
      this.formatearFechaInput(fechaActual);
  }

  ngOnInit(): void {
    const sesion =
      localStorage.getItem('sesionSIAF');

    if (!sesion) {
      this.router.navigate(['/login']);
      return;
    }

    this.cargarCatalogos();
    this.cargarInformesRecientes();
  }

  cargarCatalogos(): void {
    this.cargandoCatalogos.set(true);
    this.mensajeError.set('');

    this.informesService.obtenerCatalogos()
      .pipe(
        finalize(() => {
          this.cargandoCatalogos.set(false);
        })
      )
      .subscribe({
        next: respuesta => {
          this.catalogos.set(respuesta);
          this.actualizarVistaPrevia();
        },

        error: error => {
          console.error(
            'Error al obtener los catálogos de informes:',
            error
          );

          this.mensajeError.set(
            'No fue posible obtener las opciones para generar informes.'
          );
        }
      });
  }

  cargarInformesRecientes(): void {
    this.cargandoInformesRecientes.set(true);
    this.mensajeErrorRecientes.set('');

    this.informesService
      .obtenerInformesRecientes(10)
      .pipe(
        finalize(() => {
          this.cargandoInformesRecientes.set(false);
        })
      )
      .subscribe({
        next: respuesta => {
          this.informesRecientes.set(respuesta);
        },

        error: error => {
          console.error(
            'Error al obtener los informes recientes:',
            error
          );

          this.mensajeErrorRecientes.set(
            'No fue posible consultar los informes recientes.'
          );
        }
      });
  }

  actualizarVistaPrevia(): void {
    if (!this.validarFiltros()) {
      return;
    }

    const filtros =
      this.crearFiltros();

    this.cargandoVistaPrevia.set(true);
    this.mensajeError.set('');
    this.mensajeExito.set('');

    this.informesService
      .obtenerVistaPrevia(filtros)
      .pipe(
        finalize(() => {
          this.cargandoVistaPrevia.set(false);
        })
      )
      .subscribe({
        next: respuesta => {
          this.vistaPrevia.set(respuesta);
        },

        error: error => {
          console.error(
            'Error al obtener la vista previa del informe:',
            error
          );

          const mensajeApi =
            error?.error?.error;

          this.mensajeError.set(
            mensajeApi
            || 'No fue posible generar la vista previa del informe.'
          );
        }
      });
  }

  generarInforme(): void {
    if (!this.validarFiltros()) {
      return;
    }

    const filtros =
      this.crearFiltros();

    this.generandoPdf.set(true);
    this.mensajeError.set('');
    this.mensajeExito.set('');

    this.informesService
      .descargarPdf(filtros)
      .pipe(
        finalize(() => {
          this.generandoPdf.set(false);
        })
      )
      .subscribe({
        next: respuesta => {
          const archivo = respuesta.body;

          if (!archivo || archivo.size === 0) {
            this.mensajeError.set(
              'El servidor generó un archivo vacío.'
            );

            return;
          }

          const nombrePredeterminado =
            this.construirNombreArchivo(filtros);

          const nombreArchivo =
            this.obtenerNombreArchivo(
              respuesta.headers.get(
                'content-disposition'
              ),
              nombrePredeterminado
            );

          this.descargarArchivo(
            archivo,
            nombreArchivo
          );

          this.registrarInformeGenerado(
            filtros,
            nombreArchivo,
            archivo.size
          );
        },

        error: error => {
          console.error(
            'Error al descargar el informe PDF:',
            error
          );

          this.mensajeError.set(
            'No fue posible generar el archivo PDF.'
          );
        }
      });
  }

  descargarInformeReciente(
    informe: InformeGenerado
  ): void {
    const filtros: FiltrosInforme = {
      tipo: informe.tipoInforme,
      fechaInicio: informe.fechaInicio,
      fechaFin: informe.fechaFin,
      seccion: informe.seccion
    };

    this.descargandoInformeId.set(informe.id);
    this.mensajeError.set('');
    this.mensajeExito.set('');

    this.informesService
      .descargarPdf(filtros)
      .pipe(
        finalize(() => {
          this.descargandoInformeId.set(null);
        })
      )
      .subscribe({
        next: respuesta => {
          const archivo = respuesta.body;

          if (!archivo || archivo.size === 0) {
            this.mensajeError.set(
              'El servidor generó un archivo vacío.'
            );

            return;
          }

          const nombreArchivo =
            this.obtenerNombreArchivo(
              respuesta.headers.get(
                'content-disposition'
              ),
              informe.nombreArchivo
            );

          this.descargarArchivo(
            archivo,
            nombreArchivo
          );

          this.mensajeExito.set(
            `El informe ${nombreArchivo} se descargó correctamente.`
          );
        },

        error: error => {
          console.error(
            'Error al volver a descargar el informe:',
            error
          );

          this.mensajeError.set(
            'No fue posible volver a descargar el informe.'
          );
        }
      });
  }

  validarFiltros(): boolean {
    this.mensajeError.set('');

    if (
      !this.tipoInforme
      || !this.fechaInicio
      || !this.fechaFin
      || !this.seccion
    ) {
      this.mensajeError.set(
        'Completa todos los campos del formulario.'
      );

      return false;
    }

    if (this.fechaFin < this.fechaInicio) {
      this.mensajeError.set(
        'La fecha final no puede ser menor que la fecha inicial.'
      );

      return false;
    }

    return true;
  }

  esInformeDeFugas(
    informe: VistaPreviaInforme
  ): boolean {
    return informe.tipoInforme !== 'Valvulas';
  }

  esInformeDeValvulas(
    informe: VistaPreviaInforme
  ): boolean {
    return informe.tipoInforme !== 'Fugas';
  }

  obtenerClaseValvula(
    estado: string
  ): string {
    switch (estado.toLowerCase()) {
      case 'abierta':
        return 'estado-abierta';

      case 'cerrada':
        return 'estado-cerrada';

      default:
        return 'estado-sin-datos';
    }
  }

  obtenerClaseTipoInforme(
    tipo: string
  ): string {
    switch (tipo) {
      case 'Fugas':
        return 'tipo-fugas';

      case 'Valvulas':
        return 'tipo-valvulas';

      default:
        return 'tipo-completo';
    }
  }

  formatearTamano(
    tamanoBytes: number
  ): string {
    if (!tamanoBytes || tamanoBytes <= 0) {
      return '0 KB';
    }

    const kilobytes =
      tamanoBytes / 1024;

    if (kilobytes < 1024) {
      return `${kilobytes.toFixed(1)} KB`;
    }

    const megabytes =
      kilobytes / 1024;

    return `${megabytes.toFixed(2)} MB`;
  }

  private registrarInformeGenerado(
    filtros: FiltrosInforme,
    nombreArchivo: string,
    tamanoBytes: number
  ): void {
    const solicitud: RegistrarInformeGenerado = {
      tipoInforme: filtros.tipo,
      fechaInicio: filtros.fechaInicio,
      fechaFin: filtros.fechaFin,
      seccion: filtros.seccion,
      nombreArchivo,
      tamanoBytes,
      usuarioId: this.obtenerUsuarioId()
    };

    this.informesService
      .registrarInforme(solicitud)
      .subscribe({
        next: respuesta => {
          if (!respuesta.ok) {
            this.mensajeError.set(
              'El PDF se descargó, pero no pudo registrarse en el historial.'
            );

            return;
          }

          this.mensajeExito.set(
            `El informe ${nombreArchivo} se generó correctamente.`
          );

          this.cargarInformesRecientes();
        },

        error: error => {
          console.error(
            'Error al registrar el informe generado:',
            error
          );

          this.mensajeError.set(
            'El PDF se descargó, pero no pudo registrarse en Informes recientes.'
          );
        }
      });
  }

  private crearFiltros(): FiltrosInforme {
    return {
      tipo: this.tipoInforme,
      fechaInicio: this.fechaInicio,
      fechaFin: this.fechaFin,
      seccion: this.seccion
    };
  }

  private obtenerUsuarioId(): number | null {
    const sesionTexto =
      localStorage.getItem('sesionSIAF');

    if (!sesionTexto) {
      return null;
    }

    try {
      const sesion = JSON.parse(
        sesionTexto
      ) as {
        id?: number | string;
      };

      const id = Number(sesion.id);

      return Number.isInteger(id) && id > 0
        ? id
        : null;
    } catch {
      return null;
    }
  }

  private descargarArchivo(
    archivo: Blob,
    nombreArchivo: string
  ): void {
    const direccionTemporal =
      URL.createObjectURL(archivo);

    const enlace =
      document.createElement('a');

    enlace.href = direccionTemporal;
    enlace.download = nombreArchivo;
    enlace.style.display = 'none';

    document.body.appendChild(enlace);

    enlace.click();
    enlace.remove();

    window.setTimeout(() => {
      URL.revokeObjectURL(
        direccionTemporal
      );
    }, 100);
  }

  private obtenerNombreArchivo(
    contentDisposition: string | null,
    nombrePredeterminado: string
  ): string {
    if (contentDisposition) {
      const coincidencia =
        /filename\*?=(?:UTF-8''|")?([^";]+)/i
          .exec(contentDisposition);

      if (coincidencia?.[1]) {
        const nombre =
          coincidencia[1]
            .replaceAll('"', '')
            .trim();

        try {
          return decodeURIComponent(nombre);
        } catch {
          return nombre;
        }
      }
    }

    return nombrePredeterminado;
  }

  private construirNombreArchivo(
    filtros: FiltrosInforme
  ): string {
    const tipo =
      filtros.tipo.toLowerCase();

    const seccion =
      filtros.seccion
        .toLowerCase()
        .replaceAll('_', '-');

    return (
      `saf-${tipo}-${seccion}-`
      + `${filtros.fechaInicio}-${filtros.fechaFin}.pdf`
    );
  }

  private formatearFechaInput(
    fecha: Date
  ): string {
    const anio =
      fecha.getFullYear();

    const mes = String(
      fecha.getMonth() + 1
    ).padStart(2, '0');

    const dia = String(
      fecha.getDate()
    ).padStart(2, '0');

    return `${anio}-${mes}-${dia}`;
  }
}
