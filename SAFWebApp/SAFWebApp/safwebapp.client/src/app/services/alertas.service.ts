import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Alerta } from '../models/alerta';

@Injectable({
  providedIn: 'root'
})
export class AlertasService {
  private readonly url = '/api/alertas';

  constructor(private http: HttpClient) { }

  obtenerAlertas(): Observable<Alerta[]> {
    return this.http.get<Alerta[]>(this.url);
  }
}
