import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  RedDistribucion
} from '../models/red-distribucion';

@Injectable({
  providedIn: 'root'
})
export class RedService {
  private readonly apiUrl = '/api/red';

  constructor(private http: HttpClient) { }

  obtenerRed(): Observable<RedDistribucion> {
    return this.http.get<RedDistribucion>(this.apiUrl);
  }
}
