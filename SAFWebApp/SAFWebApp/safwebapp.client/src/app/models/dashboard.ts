export interface Dashboard {
  fugasActivas: number;
  aguaPerdidaHoyLitros: number;
  seccionesOperativas: number;
  seccionesTotales: number;
  valvulasActivas: number;
  eventosRecientes: EventoReciente[];
  fugasSemanales: FugaSemanal[];
  prediccionRiesgo: RiesgoResumen[];
  fechaActualizacion: string;
}

export interface EventoReciente {
  seccion: string;
  severidad: string;
  fechaDeteccion: string;
}

export interface FugaSemanal {
  dia: string;
  cantidad: number;
}

export interface RiesgoResumen {
  seccion: string;
  riesgo: string;
  porcentajeMasReciente: number;
}
