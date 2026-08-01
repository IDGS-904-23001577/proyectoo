import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { LecturaActual, LecturaHistorialPunto } from '../models/lectura';

@Injectable({
  providedIn: 'root'
})
export class LecturasService {
  private readonly apiUrl = '/api/lecturas';

  constructor(private http: HttpClient) { }

  obtenerActual(): Observable<LecturaActual[]> {
    return this.http.get<LecturaActual[]>(`${this.apiUrl}/actual`);
  }

  obtenerHistorial(
    seccion: string,
    minutos: number
  ): Observable<LecturaHistorialPunto[]> {
    const parametros = new HttpParams()
      .set('seccion', seccion)
      .set('minutos', minutos);

    return this.http.get<LecturaHistorialPunto[]>(
      `${this.apiUrl}/historial`,
      { params: parametros }
    );
  }
}