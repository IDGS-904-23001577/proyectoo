import {
  Component,
  OnInit,
  signal
} from '@angular/core';

import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { Valvula } from '../models/valvulas';
import { ValvulasService } from '../services/valvulas.service';

@Component({
  selector: 'app-valvulas',
  templateUrl: './valvulas.html',
  styleUrls: ['./valvulas.css'],
  standalone: false
})
export class ValvulasComponent implements OnInit {

  valvulas = signal<Valvula[]>([]);
  cargando = signal(true);

  mensajeError = signal('');
  mensajeExito = signal('');

  procesando = signal<number | null>(null);

  constructor(
    private router: Router,
    private valvulasService: ValvulasService
  ) { }

  ngOnInit(): void {

    // Verifica que el usuario tenga una sesión iniciada.
    if (!localStorage.getItem('sesionSIAF')) {
      this.router.navigate(['/login']);
      return;
    }

    this.cargar();

    // Escucha cambios en tiempo real: si la válvula se abre o
    // cierra desde la app móvil (o desde aquí mismo), esta pantalla
    // se actualiza sola sin necesidad de recargar.
    this.valvulasService.conectarTiempoReal((numero, estado) => {
      this.valvulas.update(actuales =>
        actuales.map(item =>
          item.numero === numero
            ? { ...item, estado }
            : item
        )
      );
    });
  }

  /**
   * Consulta las válvulas desde el backend.
   */
  cargar(): void {

    this.cargando.set(true);
    this.mensajeError.set('');

    this.valvulasService.obtenerTodas()
      .pipe(
        finalize(() => this.cargando.set(false))
      )
      .subscribe({
        next: datos => {
          this.valvulas.set(datos);
        },
        error: error => {
          console.error(error);

          this.mensajeError.set(
            'No fue posible consultar las válvulas.'
          );
        }
      });
  }

  /**
   * Envía una orden para abrir o cerrar la válvula.
   */
  ejecutar(
    valvula: Valvula,
    estado: 'abrir' | 'cerrar'
  ): void {

    this.procesando.set(valvula.numero);

    this.mensajeError.set('');
    this.mensajeExito.set('');

    this.valvulasService
      .cambiarEstado(valvula.numero, estado)
      .pipe(
        finalize(() => this.procesando.set(null))
      )
      .subscribe({
        next: respuesta => {

          // Actualiza solamente la válvula modificada.
          this.valvulas.update(actuales =>
            actuales.map(item =>
              item.numero === respuesta.valvula.numero
                ? respuesta.valvula
                : item
            )
          );

          this.mensajeExito.set(respuesta.mensaje);
        },
        error: error => {
          console.error(error);

          this.mensajeError.set(
            error.error?.error ??
            'No fue posible enviar el comando.'
          );
        }
      });
  }

  /**
   * Devuelve una clase CSS dependiendo del estado.
   */
  claseEstado(estado: string): string {

    return estado.trim().toLowerCase() === 'abierta'
      ? 'abierta'
      : 'cerrada';
  }
}
