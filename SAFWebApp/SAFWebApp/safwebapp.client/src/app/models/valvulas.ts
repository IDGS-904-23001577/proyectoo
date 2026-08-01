export interface Valvula {
  numero: number;
  nombre: string;
  secciones: string;
  topic: string;
  estado: string;
  ultimaActualizacion: string;
  disponible: boolean;
}

export interface ComandoValvula {
  estado: string;
}

export interface RespuestaComandoValvula {
  ok: boolean;
  mensaje: string;
  valvula: Valvula;
}