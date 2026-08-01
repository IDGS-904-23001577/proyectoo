export interface Prediccion {
  seccion: string;
  etiqueta: string;
  riesgo: string;
  pred24h: number;
  pred48h: number;
  pred72h: number;
  porcentajeRelativo: number;
  fechaCalculo: string;
}

export interface PrediccionHistorialPunto {
  fecha: string;
  presionPromedio: number;
  caudalPromedio: number;
}