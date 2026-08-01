export interface EndpointApi {
  metodo: string;
  ruta: string;
  descripcion: string;
  modulo: string;
}

export interface EstadoApi {
  estado: string;
  version: string;
  ambiente: string;
  baseDatos: boolean;
  fechaServidor: string;
  tiempoRespuestaMs: number;
  endpoints: EndpointApi[];
}