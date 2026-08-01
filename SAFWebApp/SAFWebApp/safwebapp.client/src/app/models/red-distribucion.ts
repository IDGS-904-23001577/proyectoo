export type EstadoNodoRed =
  | 'Normal'
  | 'Advertencia'
  | 'Fuga';

export interface RedDistribucion {
  nodos: NodoRed[];
  conexiones: ConexionRed[];
  fechaActualizacion: string;
}

export interface NodoRed {
  id: string;
  etiqueta: string;
  posicionX: number;
  posicionY: number;
  estado: EstadoNodoRed;
  estadoFuga: string;
  severidad: string;
  fugaId: string | null;
  seccionAfectada: string;
  fechaDeteccion: string;
}

export interface ConexionRed {
  origen: string;
  destino: string;
}
