export interface LecturaActual {
  seccion: string;
  etiqueta: string;
  presionBar: number;
  caudalLmin: number;
  estadoValvula: string;
  estado: string;
  sensorEnLinea: boolean;
  ultimaLectura: string;
}

export interface LecturaHistorialPunto {
  timestamp: string;
  presionBar: number;
  caudalLmin: number;
}