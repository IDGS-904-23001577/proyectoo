import { Component, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { finalize, timeout } from 'rxjs';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
  standalone: false
})
export class LoginComponent {
  correo: string = '';
  password: string = '';

  cargando = signal(false);
  errorMessage = signal('');

  constructor(
    private http: HttpClient,
    private router: Router
  ) { }

  onLogin(): void {
    if (!this.correo.trim() || !this.password) {
      this.errorMessage.set(
        'Por favor ingresa correo y contraseña'
      );
      return;
    }

    this.cargando.set(true);
    this.errorMessage.set('');

    const loginData = {
      correo: this.correo.trim(),
      password: this.password
    };

    this.http.post<any>('/api/auth/login', loginData)
      .pipe(
        timeout(15000),
        finalize(() => {
          this.cargando.set(false);
        })
      )
      .subscribe({
        next: (response) => {
          if (response.ok) {
            localStorage.setItem(
              'sesionSIAF',
              JSON.stringify(response)
            );

            this.router.navigate(['/alertas']);
          } else {
            this.errorMessage.set(
              'El servidor devolvió una respuesta no válida'
            );
          }
        },
        error: (err) => {
          console.error('Error del login:', err);

          if (err.status === 401) {
            this.errorMessage.set(
              'Correo o contraseña incorrectos'
            );
          } else if (err.status === 503) {
            this.errorMessage.set(
              'No fue posible conectar con la base de datos'
            );
          } else if (err.name === 'TimeoutError') {
            this.errorMessage.set(
              'El servidor tardó demasiado en responder'
            );
          } else {
            this.errorMessage.set(
              'No se pudo conectar con el servidor'
            );
          }
        }
      });
  }
}
