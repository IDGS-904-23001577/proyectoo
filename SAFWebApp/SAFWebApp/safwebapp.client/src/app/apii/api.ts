import {
  Component,
  OnInit,
  signal
} from '@angular/core';

import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { EstadoApi } from '../models/api-estado';
import { ApiEstadoService } from '../services/api-estado.service';

@Component({
  selector: 'app-api',
  templateUrl: './api.html',
  styleUrls: ['./api.css'],
  standalone: false
})
export class ApiComponent implements OnInit {

  estado = signal<EstadoApi | null>(null);
  cargando = signal(true);
  mensajeError = signal('');

  constructor(
    private router: Router,
    private apiService: ApiEstadoService
  ) { }

  ngOnInit(): void {

    if (!localStorage.getItem('sesionSIAF')) {
      this.router.navigate(['/login']);
      return;
    }

    this.consultar();
  }

  /**
   * Consulta el diagnóstico del backend.
   */
  consultar(): void {

    this.cargando.set(true);
    this.mensajeError.set('');

    this.apiService.obtenerEstado()
      .pipe(
        finalize(() => this.cargando.set(false))
      )
      .subscribe({
        next: estado => {
          this.estado.set(estado);
        },
        error: error => {
          console.error(error);

          this.mensajeError.set(
            'No fue posible consultar el estado de la API.'
          );
        }
      });
  }

  /**
   * Convierte GET o POST en una clase de CSS.
   */
  claseMetodo(metodo: string): string {
    return metodo.toLowerCase();
  }
}