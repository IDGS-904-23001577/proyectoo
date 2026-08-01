import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-inicio',
  templateUrl: './inicio.html',
  standalone: false
})
export class InicioComponent {
  usuario: any = null;

  constructor(private router: Router) {
    const sesion = localStorage.getItem('sesionSIAF');

    if (!sesion) {
      this.router.navigate(['/login']);
      return;
    }

    this.usuario = JSON.parse(sesion);
  }

  cerrarSesion(): void {
    localStorage.removeItem('sesionSIAF');
    this.router.navigate(['/login']);
  }
}
