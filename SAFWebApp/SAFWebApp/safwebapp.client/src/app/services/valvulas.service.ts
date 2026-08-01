import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import {
  RespuestaComandoValvula,
  Valvula
} from '../models/valvulas';

@Injectable({
  providedIn: 'root'
})
export class ValvulasService {

  private readonly url = '/api/valvulas';

  private hubConnection?: HubConnection;

  constructor(private http: HttpClient) { }

  /**
   * Obtiene el estado actual de todas las válvulas.
   */
  obtenerTodas(): Observable<Valvula[]> {
    return this.http.get<Valvula[]>(this.url);
  }

  /**
   * Envía al backend el comando para abrir o cerrar una válvula.
   */
  cambiarEstado(
    numero: number,
    estado: 'abrir' | 'cerrar'
  ): Observable<RespuestaComandoValvula> {

    return this.http.post<RespuestaComandoValvula>(
      `${this.url}/${numero}/comando`,
      { estado }
    );
  }

  /**
   * Se conecta al Hub de SignalR para recibir actualizaciones en
   * tiempo real del estado de las válvulas, confirmadas por el
   * ESP32 vía MQTT. Si ya hay una conexión activa, no abre otra.
   */
  conectarTiempoReal(
    onActualizacion: (numero: number, estado: string) => void
  ): void {

    if (this.hubConnection) {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/hubs/valvulas')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on(
      'ValvulaActualizada',
      (numero: number, estado: string) => {
        onActualizacion(numero, estado);
      }
    );

    this.hubConnection.start()
      .catch(error => {
        console.error('No fue posible conectar al Hub de válvulas:', error);
      });
  }
}
