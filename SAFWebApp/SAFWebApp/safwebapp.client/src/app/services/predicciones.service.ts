import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Prediccion, PrediccionHistorialPunto } from '../models/prediccion';

@Injectable({
  providedIn: 'root'
})
export class PrediccionesService {
  private readonly apiUrl = '/api/predicciones';

  constructor(private http: HttpClient) { }

  obtenerPredicciones(): Observable<Prediccion[]> {
    return this.http.get<Prediccion[]>(this.apiUrl);
  }

  obtenerHistorial(
    seccion: string,
    dias: number
  ): Observable<PrediccionHistorialPunto[]> {
    const parametros = new HttpParams()
      .set('seccion', seccion)
      .set('dias', dias);

    return this.http.get<PrediccionHistorialPunto[]>(
      `${this.apiUrl}/historial`,
      { params: parametros }
    );
  }
}