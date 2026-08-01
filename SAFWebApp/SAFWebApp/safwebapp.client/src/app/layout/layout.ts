import {
  Component,
  OnInit,
  signal
} from '@angular/core';

import { Router } from '@angular/router';

interface SesionUsuario {
  id?: number | string;
  nombre?: string;
  correo?: string;
  rol?: string;
  empleado_id?: number | string;
}

@Component({
  selector: 'app-layout',
  templateUrl: './layout.html',
  styleUrls: ['./layout.css'],
  standalone: false
})
export class LayoutComponent implements OnInit {
  nombreUsuario = signal('Usuario');

  constructor(
    private router: Router
  ) { }

  ngOnInit(): void {
    const sesionTexto =
      localStorage.getItem('sesionSIAF');

    if (!sesionTexto) {
      this.router.navigate(['/login']);
      return;
    }

    try {
      const sesion =
        JSON.parse(sesionTexto) as SesionUsuario;

      this.nombreUsuario.set(
        sesion.nombre?.trim() || 'Usuario'
      );
    } catch {
      this.cerrarSesion();
    }
  }

  cerrarSesion(): void {
    localStorage.removeItem('sesionSIAF');
    this.router.navigate(['/login']);
  }
}
