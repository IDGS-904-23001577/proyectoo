using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Services
{
    public class InformePdfService
    {
        private readonly string _rutaLogo;
        public InformePdfService(
    IWebHostEnvironment environment)
        {
            _rutaLogo = Path.Combine(
                environment.WebRootPath,
                "images",
                "saf-logo.png"
            );
        }
        public byte[] GenerarPdf(
            VistaPreviaInformeDto informe)
        {
            IDocument documento = Document.Create(contenedor =>
            {
                contenedor.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);

                    pagina.Margin(
                        1.7f,
                        Unit.Centimetre
                    );

                    pagina.PageColor(Colors.White);

                    pagina.DefaultTextStyle(estilo =>
                        estilo
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken3)
                    );

                    pagina.Header()
                        .Element(encabezado =>
                            CrearEncabezado(
    encabezado,
    informe,
    _rutaLogo
)
                        );

                    pagina.Content()
                        .PaddingVertical(18)
                        .Column(columna =>
                        {
                            columna.Spacing(16);

                            columna.Item()
                                .Element(contenedorResumen =>
                                    CrearResumenGeneral(
                                        contenedorResumen,
                                        informe
                                    )
                                );

                            if (
                                informe.TipoInforme
                                != "Valvulas"
                            )
                            {
                                columna.Item()
                                    .Element(contenedorFugas =>
                                        CrearResumenFugas(
                                            contenedorFugas,
                                            informe
                                        )
                                    );
                            }

                            if (
                                informe.TipoInforme
                                == "Completo"
                            )
                            {
                                columna.Item()
                                    .Element(contenedorLecturas =>
                                        CrearResumenLecturas(
                                            contenedorLecturas,
                                            informe
                                        )
                                    );
                            }

                            if (
                                informe.TipoInforme
                                != "Fugas"
                            )
                            {
                                columna.Item()
                                    .Element(contenedorValvulas =>
                                        CrearEstadoValvulas(
                                            contenedorValvulas,
                                            informe
                                        )
                                    );
                            }

                            columna.Item()
                                .Element(contenedorSecciones =>
                                    CrearSeccionesIncluidas(
                                        contenedorSecciones,
                                        informe
                                    )
                                );
                        });

                    pagina.Footer()
                        .AlignCenter()
                        .Text(texto =>
                        {
                            texto.Span("SAF · ");

                            texto.Span("Página ");

                            texto.CurrentPageNumber();

                            texto.Span(" de ");

                            texto.TotalPages();
                        });
                });
            });

            return documento.GeneratePdf();
        }

        private static void CrearEncabezado(
    IContainer contenedor,
    VistaPreviaInformeDto informe,
    string rutaLogo)
        {
            contenedor
                .BorderBottom(2)
                .BorderColor(Colors.Blue.Medium)
                .PaddingBottom(14)
                .Column(columna =>
                {
                    columna.Spacing(6);

                    columna.Item()
                        .AlignCenter()
                        .Row(fila =>
                        {
                            fila.Spacing(7);

                            if (File.Exists(rutaLogo))
                            {
                                byte[] logo =
                                    File.ReadAllBytes(rutaLogo);

                                fila.AutoItem()
                                    .Width(25)
                                    .Height(25)
                                    .Image(logo)
                                    .FitArea();
                            }

                            fila.AutoItem()
                                .PaddingTop(3)
                                .Text("SAF")
                                .FontSize(18)
                                .Bold()
                                .FontColor(
                                    Colors.Blue.Darken2
                                );
                        });

                    columna.Item()
                        .AlignCenter()
                        .Text(informe.TipoInformeEtiqueta)
                        .FontSize(16)
                        .SemiBold()
                        .FontColor(
                            Colors.Grey.Darken4
                        );

                    columna.Item()
                        .AlignCenter()
                        .Text(
                            $"Periodo: " +
                            $"{FormatearFecha(informe.FechaInicio)} " +
                            $"al {FormatearFecha(informe.FechaFin)}"
                        )
                        .FontSize(9)
                        .FontColor(
                            Colors.Grey.Darken1
                        );
                });
        }

        private static void CrearResumenGeneral(
            IContainer contenedor,
            VistaPreviaInformeDto informe)
        {
            contenedor
                .Background(Colors.Grey.Lighten4)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(12)
                .Column(columna =>
                {
                    columna.Spacing(5);

                    columna.Item()
                        .Text("Información del informe")
                        .FontSize(12)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);

                    columna.Item()
                        .Text(
                            $"Tipo: {informe.TipoInformeEtiqueta}"
                        );

                    columna.Item()
                        .Text(
                            $"Sección: {informe.SeccionEtiqueta}"
                        );

                    columna.Item()
                        .Text(
                            $"Fecha de generación: {FormatearFechaHora(informe.FechaGeneracion)}"
                        );
                });
        }

        private static void CrearResumenFugas(
            IContainer contenedor,
            VistaPreviaInformeDto informe)
        {
            ResumenInformeDto resumen =
                informe.Resumen;

            contenedor.Column(columna =>
            {
                columna.Spacing(8);

                columna.Item()
                    .Text("Resumen de fugas")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                columna.Item()
                    .Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn();
                            columnas.ConstantColumn(110);
                        });

                        AgregarFilaTabla(
                            tabla,
                            "Total de fugas detectadas",
                            resumen.TotalFugas.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Fugas activas",
                            resumen.FugasActivas.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Fugas pendientes",
                            resumen.FugasPendientes.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Fugas resueltas",
                            resumen.FugasResueltas.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Volumen perdido",
                            $"{resumen.VolumenPerdidoLitros:N2} litros"
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Secciones afectadas",
                            resumen.SeccionesAfectadas.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Intervenciones registradas",
                            resumen.TotalIntervenciones.ToString()
                        );
                    });
            });
        }

        private static void CrearResumenLecturas(
            IContainer contenedor,
            VistaPreviaInformeDto informe)
        {
            ResumenInformeDto resumen =
                informe.Resumen;

            contenedor.Column(columna =>
            {
                columna.Spacing(8);

                columna.Item()
                    .Text("Resumen de lecturas")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                columna.Item()
                    .Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn();
                            columnas.ConstantColumn(110);
                        });

                        AgregarFilaTabla(
                            tabla,
                            "Total de lecturas",
                            resumen.TotalLecturas.ToString()
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Flujo promedio",
                            $"{resumen.FlujoPromedioLpm:N2} L/min"
                        );

                        AgregarFilaTabla(
                            tabla,
                            "Presión promedio",
                            $"{resumen.PresionPromedioBar:N2} bar"
                        );
                    });
            });
        }

        private static void CrearEstadoValvulas(
            IContainer contenedor,
            VistaPreviaInformeDto informe)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(8);

                columna.Item()
                    .Text("Estado de válvulas")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                columna.Item()
                    .Text(
                        $"Abiertas: {informe.Resumen.ValvulasAbiertas} · Cerradas: {informe.Resumen.ValvulasCerradas}"
                    )
                    .FontSize(9)
                    .FontColor(Colors.Grey.Darken1);

                if (informe.Valvulas.Count == 0)
                {
                    columna.Item()
                        .Text(
                            "No existen datos de válvulas para el periodo seleccionado."
                        )
                        .Italic()
                        .FontColor(Colors.Grey.Darken1);

                    return;
                }

                columna.Item()
                    .Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(80);
                            columnas.RelativeColumn();
                            columnas.ConstantColumn(70);
                            columnas.ConstantColumn(105);
                        });

                        tabla.Header(encabezado =>
                        {
                            AgregarCeldaEncabezado(
                                encabezado,
                                "Válvula"
                            );

                            AgregarCeldaEncabezado(
                                encabezado,
                                "Secciones"
                            );

                            AgregarCeldaEncabezado(
                                encabezado,
                                "Estado"
                            );

                            AgregarCeldaEncabezado(
                                encabezado,
                                "Última lectura"
                            );
                        });

                        foreach (
                            EstadoValvulaInformeDto valvula
                            in informe.Valvulas
                        )
                        {
                            AgregarCeldaContenido(
                                tabla,
                                valvula.Nombre
                            );

                            AgregarCeldaContenido(
                                tabla,
                                valvula.Secciones
                            );

                            AgregarCeldaContenido(
                                tabla,
                                valvula.Estado
                            );

                            AgregarCeldaContenido(
                                tabla,
                                string.IsNullOrWhiteSpace(
                                    valvula.FechaLectura
                                )
                                    ? "Sin datos"
                                    : valvula.FechaLectura
                            );
                        }
                    });
            });
        }

        private static void CrearSeccionesIncluidas(
            IContainer contenedor,
            VistaPreviaInformeDto informe)
        {
            contenedor.Column(columna =>
            {
                columna.Spacing(7);

                columna.Item()
                    .Text("Secciones incluidas")
                    .FontSize(13)
                    .SemiBold()
                    .FontColor(Colors.Blue.Darken2);

                string secciones =
                    informe.SeccionesIncluidas.Count == 0
                        ? "No se seleccionaron secciones"
                        : string.Join(
                            ", ",
                            informe.SeccionesIncluidas
                        );

                columna.Item()
                    .Background(Colors.Blue.Lighten5)
                    .Border(1)
                    .BorderColor(Colors.Blue.Lighten3)
                    .Padding(10)
                    .Text(secciones)
                    .FontSize(9);
            });
        }

        private static void AgregarFilaTabla(
            TableDescriptor tabla,
            string etiqueta,
            string valor)
        {
            tabla.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(6)
                .Text(etiqueta);

            tabla.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(6)
                .AlignRight()
                .Text(valor)
                .SemiBold();
        }

        private static void AgregarCeldaEncabezado(
            TableCellDescriptor encabezado,
            string texto)
        {
            encabezado.Cell()
                .Background(Colors.Blue.Darken2)
                .Padding(7)
                .Text(texto)
                .FontColor(Colors.White)
                .SemiBold()
                .FontSize(8);
        }

        private static void AgregarCeldaContenido(
            TableDescriptor tabla,
            string texto)
        {
            tabla.Cell()
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(7)
                .Text(texto)
                .FontSize(8);
        }

        private static string FormatearFecha(
            string fecha)
        {
            return DateTime.TryParse(
                fecha,
                out DateTime fechaConvertida
            )
                ? fechaConvertida.ToString("dd/MM/yyyy")
                : fecha;
        }

        private static string FormatearFechaHora(
            string fecha)
        {
            return DateTimeOffset.TryParse(
                fecha,
                out DateTimeOffset fechaConvertida
            )
                ? fechaConvertida.ToLocalTime()
                    .ToString("dd/MM/yyyy HH:mm:ss")
                : fecha;
        }
    }
}