import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { EstadoApi } from '../models/api-estado';

@Injectable({
  providedIn: 'root'
})
export class ApiEstadoService {

  constructor(private http: HttpClient) { }

  /**
   * Consulta el estado general del backend.
   */
  obtenerEstado(): Observable<EstadoApi> {
    return this.http.get<EstadoApi>('/api/estadoapi');
  }
}
