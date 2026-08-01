import { Injectable } from '@angular/core';

import {
  HttpClient,
  HttpParams,
  HttpResponse
} from '@angular/common/http';

import { Observable } from 'rxjs';

import {
  CatalogoInformes,
  FiltrosInforme,
  InformeGenerado,
  RegistrarInformeGenerado,
  RegistroInformeRespuesta,
  VistaPreviaInforme
} from '../models/informe';

@Injectable({
  providedIn: 'root'
})
export class InformesService {
  private readonly apiUrl = '/api/informes';

  constructor(
    private http: HttpClient
  ) { }

  obtenerCatalogos(): Observable<CatalogoInformes> {
    return this.http.get<CatalogoInformes>(
      `${this.apiUrl}/catalogos`
    );
  }

  obtenerVistaPrevia(
    filtros: FiltrosInforme
  ): Observable<VistaPreviaInforme> {
    return this.http.get<VistaPreviaInforme>(
      `${this.apiUrl}/vista-previa`,
      {
        params: this.crearParametros(filtros)
      }
    );
  }

  descargarPdf(
    filtros: FiltrosInforme
  ): Observable<HttpResponse<Blob>> {
    return this.http.get(
      `${this.apiUrl}/pdf`,
      {
        params: this.crearParametros(filtros),
        observe: 'response',
        responseType: 'blob'
      }
    );
  }

  obtenerInformesRecientes(
    limite: number = 10
  ): Observable<InformeGenerado[]> {
    const parametros = new HttpParams()
      .set('limite', limite.toString());

    return this.http.get<InformeGenerado[]>(
      `${this.apiUrl}/recientes`,
      {
        params: parametros
      }
    );
  }

  registrarInforme(
    informe: RegistrarInformeGenerado
  ): Observable<RegistroInformeRespuesta> {
    return this.http.post<RegistroInformeRespuesta>(
      `${this.apiUrl}/recientes`,
      informe
    );
  }

  private crearParametros(
    filtros: FiltrosInforme
  ): HttpParams {
    return new HttpParams({
      fromObject: {
        tipo: filtros.tipo,
        fechaInicio: filtros.fechaInicio,
        fechaFin: filtros.fechaFin,
        seccion: filtros.seccion
      }
    });
  }
}
