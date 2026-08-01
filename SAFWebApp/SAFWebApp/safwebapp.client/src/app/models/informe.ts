export interface OpcionCatalogo {
  valor: string;
  etiqueta: string;
}

export interface CatalogoInformes {
  tiposInforme: OpcionCatalogo[];
  secciones: OpcionCatalogo[];
}

export interface FiltrosInforme {
  tipo: string;
  fechaInicio: string;
  fechaFin: string;
  seccion: string;
}

export interface VistaPreviaInforme {
  tipoInforme: string;
  tipoInformeEtiqueta: string;
  fechaInicio: string;
  fechaFin: string;
  seccion: string;
  seccionEtiqueta: string;
  resumen: ResumenInforme;
  seccionesIncluidas: string[];
  valvulas: EstadoValvulaInforme[];
  fechaGeneracion: string;
}

export interface ResumenInforme {
  totalFugas: number;
  fugasActivas: number;
  fugasPendientes: number;
  fugasResueltas: number;
  volumenPerdidoLitros: number;
  seccionesAfectadas: number;
  totalIntervenciones: number;
  totalLecturas: number;
  flujoPromedioLpm: number;
  presionPromedioBar: number;
  valvulasAbiertas: number;
  valvulasCerradas: number;
}

export interface EstadoValvulaInforme {
  numero: number;
  nombre: string;
  secciones: string;
  estado: string;
  ultimaSeccionReportada: string;
  fechaLectura: string;
}

export interface RegistrarInformeGenerado {
  tipoInforme: string;
  fechaInicio: string;
  fechaFin: string;
  seccion: string;
  nombreArchivo: string;
  tamanoBytes: number;
  usuarioId: number | null;
}

export interface RegistroInformeRespuesta {
  ok: boolean;
  id: number;
}

export interface InformeGenerado {
  id: number;
  tipoInforme: string;
  tipoInformeEtiqueta: string;
  fechaInicio: string;
  fechaFin: string;
  seccion: string;
  seccionEtiqueta: string;
  nombreArchivo: string;
  tamanoBytes: number;
  usuarioId: number | null;
  fechaGeneracion: string;
}
