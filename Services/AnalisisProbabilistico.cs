using System.Globalization;
using HocicosAJ.Models.ViewModels;

namespace HocicosAJ.Services;

internal static class AnalisisProbabilistico
{
    private const int MaximoBarrasPoisson = 60;
    private const int MinimoIntervalosExponencial = 30;

    public static AnalisisProbabilisticoViewModel Calcular(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        IReadOnlyCollection<DetalleEstadisticaRegistro> detalles,
        AdminEstadisticasFiltrosViewModel filtros,
        AdminEstadisticasViewModel descriptivos)
    {
        return new AnalisisProbabilisticoViewModel
        {
            Probabilidades = CalcularProbabilidades(compras, detalles, descriptivos),
            Poisson = CalcularPoisson(compras, filtros),
            DistribucionesContinuas = CalcularDistribucionesContinuas(compras, filtros)
        };
    }

    private static ProbabilidadEmpiricaViewModel CalcularProbabilidades(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        IReadOnlyCollection<DetalleEstadisticaRegistro> detalles,
        AdminEstadisticasViewModel descriptivos)
    {
        var resultado = new ProbabilidadEmpiricaViewModel
        {
            EspacioMuestral = compras.Count,
            UmbralImporte = descriptivos.Mediana
        };
        if (compras.Count == 0 || !descriptivos.Mediana.HasValue)
        {
            return resultado;
        }

        var eventoAIds = compras
            .Where(compra => compra.Total >= descriptivos.Mediana.Value)
            .Select(compra => compra.Id)
            .ToHashSet();
        resultado.CantidadEventoA = eventoAIds.Count;
        resultado.ProbabilidadA = (double)eventoAIds.Count / compras.Count;

        var categoriasVendidas = detalles
            .GroupBy(detalle => detalle.CategoriaId)
            .Select(grupo => new
            {
                CategoriaId = grupo.Key,
                Nombre = grupo.First().NombreCategoria,
                Unidades = grupo.Sum(detalle => (long)detalle.Cantidad)
            })
            .ToArray();
        if (categoriasVendidas.Length == 0)
        {
            return resultado;
        }

        var maximoUnidades = categoriasVendidas.Max(categoria => categoria.Unidades);
        if (maximoUnidades <= 0)
        {
            return resultado;
        }

        var categoriasMaximas = categoriasVendidas
            .Where(categoria => categoria.Unidades == maximoUnidades)
            .Select(categoria => categoria.CategoriaId)
            .ToHashSet();
        resultado.CategoriasEventoB = string.Join(
            ", ",
            categoriasVendidas
                .Where(categoria => categoriasMaximas.Contains(categoria.CategoriaId))
                .Select(categoria => categoria.Nombre)
                .Distinct());
        var eventoBIds = detalles
            .Where(detalle => categoriasMaximas.Contains(detalle.CategoriaId) && detalle.Cantidad > 0)
            .Select(detalle => detalle.CompraId)
            .ToHashSet();
        var interseccion = eventoAIds.Intersect(eventoBIds).Count();

        resultado.CantidadEventoB = eventoBIds.Count;
        resultado.CantidadInterseccion = interseccion;
        resultado.ProbabilidadB = (double)eventoBIds.Count / compras.Count;
        resultado.ProbabilidadInterseccion = (double)interseccion / compras.Count;
        if (eventoBIds.Count > 0)
        {
            resultado.ProbabilidadCondicional = (double)interseccion / eventoBIds.Count;
        }

        resultado.Grafico = CrearBarrasProbabilidad(new[]
        {
            ("P(A)", resultado.ProbabilidadA),
            ("P(B)", resultado.ProbabilidadB),
            ("P(A ∩ B)", resultado.ProbabilidadInterseccion),
            ("P(A | B)", resultado.ProbabilidadCondicional)
        });
        return resultado;
    }

    private static PoissonViewModel CalcularPoisson(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        AdminEstadisticasFiltrosViewModel filtros)
    {
        var resultado = new PoissonViewModel
        {
            K = filtros.K,
            ComprasObservadas = compras.Count
        };
        if (compras.Count == 0)
        {
            resultado.Advertencia = "No hay compras para estimar la tasa de eventos.";
            return resultado;
        }

        var fechas = compras
            .Select(compra => DateOnly.FromDateTime(compra.Fecha))
            .Order()
            .ToArray();
        var inicio = filtros.Desde ?? fechas[0];
        var fin = filtros.Hasta ?? fechas[^1];
        var granularidadDiaria = filtros.Desde.HasValue
            && filtros.Hasta.HasValue
            && fin.DayNumber - inicio.DayNumber <= 30;
        resultado.Periodo = granularidadDiaria ? "día" : "mes";

        var inicioPeriodo = granularidadDiaria
            ? inicio
            : new DateOnly(inicio.Year, inicio.Month, 1);
        var finPeriodo = granularidadDiaria
            ? fin
            : new DateOnly(fin.Year, fin.Month, 1);
        var cantidadPeriodos = granularidadDiaria
            ? (long)finPeriodo.DayNumber - inicioPeriodo.DayNumber + 1
            : (long)(finPeriodo.Year - inicioPeriodo.Year) * 12
                + finPeriodo.Month - inicioPeriodo.Month + 1;
        if (cantidadPeriodos <= 0)
        {
            resultado.Advertencia = "El intervalo no contiene períodos válidos para el modelo.";
            return resultado;
        }

        var conteosPorPeriodo = compras
            .GroupBy(compra =>
            {
                var fecha = DateOnly.FromDateTime(compra.Fecha);
                return granularidadDiaria
                    ? fecha
                    : new DateOnly(fecha.Year, fecha.Month, 1);
            })
            .Select(grupo => (double)grupo.Count())
            .ToArray();
        var lambda = compras.Count / (double)cantidadPeriodos;
        var sumaCuadrados = conteosPorPeriodo.Sum(conteo => conteo * conteo);
        resultado.Disponible = true;
        resultado.PeriodosObservados = cantidadPeriodos;
        resultado.Lambda = lambda;
        resultado.ValorEsperado = lambda;
        resultado.VarianzaTeorica = lambda;
        if (cantidadPeriodos > 1)
        {
            var sumaDesviacionesCuadradas =
                sumaCuadrados - (double)compras.Count * compras.Count / cantidadPeriodos;
            resultado.VarianzaEmpirica = Math.Max(
                0,
                sumaDesviacionesCuadradas / (cantidadPeriodos - 1));
        }

        var k = filtros.K;
        resultado.ProbabilidadExacta = ProbabilidadPoisson(k, lambda);
        resultado.ProbabilidadMenorIgual = ProbabilidadAcumuladaPoisson(k, lambda);
        resultado.ProbabilidadMayorIgual = ProbabilidadDesdePoisson(k, lambda);

        var limiteGraficoTeorico = Math.Max(10, (int)Math.Ceiling(lambda + 4 * Math.Sqrt(lambda)));
        var limiteGrafico = Math.Min(MaximoBarrasPoisson, limiteGraficoTeorico);
        resultado.PeriodosDelGraficoRecortados = limiteGraficoTeorico > MaximoBarrasPoisson;
        var probabilidades = Enumerable.Range(0, limiteGrafico + 1)
            .Select(valor => ($"k={valor}", (double?)ProbabilidadPoisson(valor, lambda)))
            .ToArray();
        resultado.Grafico = CrearBarrasProbabilidad(probabilidades);

        var advertencias = new List<string>();
        if (compras.Count < 30 || cantidadPeriodos < 30)
        {
            advertencias.Add(
                "Hay menos de 30 compras o períodos; la estimación es exploratoria y no basta para justificar el modelo.");
        }

        if (!granularidadDiaria
            && (inicio.Day != 1
                || fin.Day != DateTime.DaysInMonth(fin.Year, fin.Month)))
        {
            advertencias.Add(
                "El rango incluye meses parciales; λ se expresa por cada período mensual seleccionado y no se ajusta por exposición parcial.");
        }

        advertencias.Add(
            "La independencia de las compras, una tasa constante y la ausencia de estacionalidad no se pueden confirmar solo con este resumen.");
        resultado.Advertencia = string.Join(" ", advertencias);
        return resultado;
    }

    private static double ProbabilidadPoisson(int k, double lambda)
    {
        if (lambda == 0)
        {
            return k == 0 ? 1 : 0;
        }

        return LimitarProbabilidad(Math.Exp(-lambda + k * Math.Log(lambda) - LogFactorial(k)));
    }

    private static double ProbabilidadAcumuladaPoisson(int k, double lambda)
    {
        if (lambda == 0)
        {
            return 1;
        }

        if (k < lambda)
        {
            var termino = ProbabilidadPoisson(k, lambda);
            var acumulada = termino;
            for (var indice = k; indice > 0; indice--)
            {
                termino *= indice / lambda;
                acumulada += termino;
            }

            return LimitarProbabilidad(acumulada);
        }

        return LimitarProbabilidad(1 - SumaColaPoisson(k + 1, lambda));
    }

    private static double ProbabilidadDesdePoisson(int k, double lambda)
    {
        if (k == 0 || lambda == 0)
        {
            return k == 0 ? 1 : 0;
        }

        if (k - 1 < lambda)
        {
            return LimitarProbabilidad(1 - ProbabilidadAcumuladaPoisson(k - 1, lambda));
        }

        return LimitarProbabilidad(SumaColaPoisson(k, lambda));
    }

    private static double SumaColaPoisson(int inicio, double lambda)
    {
        var termino = ProbabilidadPoisson(inicio, lambda);
        var suma = termino;
        var indice = inicio;
        const int limiteIteraciones = 100_000;

        while (indice < limiteIteraciones && termino > 0)
        {
            indice++;
            termino *= lambda / indice;
            suma += termino;
            if (indice > lambda && termino <= suma * 1e-15)
            {
                break;
            }
        }

        return LimitarProbabilidad(suma);
    }

    private static double LogFactorial(int valor)
    {
        var resultado = 0d;
        for (var indice = 2; indice <= valor; indice++)
        {
            resultado += Math.Log(indice);
        }

        return resultado;
    }

    private static DistribucionesContinuasViewModel CalcularDistribucionesContinuas(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        AdminEstadisticasFiltrosViewModel filtros)
    {
        return new DistribucionesContinuasViewModel
        {
            Normal = CalcularNormalTeorica(filtros),
            Exponencial = CalcularExponencial(compras, filtros)
        };
    }

    private static NormalTeoricaViewModel CalcularNormalTeorica(
        AdminEstadisticasFiltrosViewModel filtros)
    {
        var modelo = new NormalTeoricaViewModel();
        var entradas = new[]
        {
            filtros.MediaNormalTeorica,
            filtros.DesviacionNormalTeorica,
            filtros.UmbralNormal
        };
        if (entradas.All(entrada => !entrada.HasValue))
        {
            modelo.Advertencia =
                "No se ajusta una normal a importes reales sin justificar sus supuestos. Ingrese parámetros para un cálculo teórico.";
            return modelo;
        }

        if (entradas.Any(entrada => !entrada.HasValue))
        {
            modelo.Advertencia = "Complete media, desviación estándar y umbral para calcular el modelo normal teórico.";
            return modelo;
        }

        var media = filtros.MediaNormalTeorica!.Value;
        var desviacion = filtros.DesviacionNormalTeorica!.Value;
        var umbral = filtros.UmbralNormal!.Value;
        if (desviacion <= 0)
        {
            modelo.Advertencia = "La desviación estándar teórica debe ser mayor que cero.";
            return modelo;
        }

        var z = (umbral - media) / desviacion;
        var probabilidadMenorIgual = FuncionDistribucionNormal(z);
        var percentil = filtros.Percentil / 100d;
        var zPercentil = InversaNormal(percentil);
        const double z95 = 1.959963984540054;

        modelo.Calculada = true;
        modelo.Media = media;
        modelo.DesviacionEstandar = desviacion;
        modelo.Umbral = umbral;
        modelo.Z = z;
        modelo.ProbabilidadMenorIgual = probabilidadMenorIgual;
        modelo.ProbabilidadMayorIgual = LimitarProbabilidad(1 - probabilidadMenorIgual);
        modelo.PercentilSeleccionado = zPercentil.HasValue
            ? media + desviacion * zPercentil.Value
            : null;
        modelo.LimiteInferior95 = media - z95 * desviacion;
        modelo.LimiteSuperior95 = media + z95 * desviacion;
        modelo.Advertencia =
            "Cálculo teórico con parámetros ingresados; no representa un ajuste ni una conclusión sobre las compras observadas.";
        modelo.Grafico = CrearCurvaNormal(media, desviacion);
        return modelo;
    }

    private static ExponencialViewModel CalcularExponencial(
        IReadOnlyCollection<CompraEstadisticaRegistro> compras,
        AdminEstadisticasFiltrosViewModel filtros)
    {
        var modelo = new ExponencialViewModel
        {
            TiempoConsultaHoras = filtros.TiempoEsperaHoras
        };

        var fechas = compras.Select(compra => compra.Fecha).Order().ToArray();
        var intervalos = fechas
            .Skip(1)
            .Select((fecha, indice) => (fecha - fechas[indice]).TotalHours)
            .ToArray();
        modelo.IntervalosEntreCompras = intervalos.Length;

        var tieneIntervalosNoPositivos = intervalos.Any(intervalo => intervalo <= 0);
        double? tasaEmpirica = null;
        if (intervalos.Length >= MinimoIntervalosExponencial
            && !tieneIntervalosNoPositivos)
        {
            var tiempoTotal = intervalos.Sum();
            if (tiempoTotal > 0 && double.IsFinite(tiempoTotal))
            {
                tasaEmpirica = intervalos.Length / tiempoTotal;
            }
        }

        double? tasa = null;
        if (filtros.TasaExponencialTeorica.HasValue)
        {
            tasa = filtros.TasaExponencialTeorica.Value;
            modelo.EsAjusteEmpirico = false;
            modelo.FuenteParametro = "Teórica: tasa ingresada por el usuario.";
        }
        else if (tasaEmpirica.HasValue)
        {
            tasa = tasaEmpirica.Value;
            modelo.EsAjusteEmpirico = true;
            modelo.FuenteParametro = "Estimación empírica MLE a partir de intervalos positivos.";
        }

        var advertencias = new List<string>();
        if (tieneIntervalosNoPositivos)
        {
            advertencias.Add(
                "Hay compras con marcas de tiempo iguales; la resolución temporal no permite ajustar una exponencial empírica.");
        }
        else if (intervalos.Length < MinimoIntervalosExponencial)
        {
            advertencias.Add(
                $"Se requieren al menos {MinimoIntervalosExponencial} intervalos positivos para mostrar el ajuste empírico.");
        }

        advertencias.Add(
            "La independencia y una tasa constante son supuestos del proceso; no se validan con este cálculo.");

        if (!tasa.HasValue || tasa.Value <= 0)
        {
            modelo.Advertencia = string.Join(" ", advertencias)
                + " Ingrese una tasa teórica positiva para obtener resultados teóricos.";
            return modelo;
        }

        var lambda = tasa.Value;
        var tiempo = filtros.TiempoEsperaHoras;
        var percentil = filtros.Percentil / 100d;
        modelo.Calculada = true;
        modelo.TasaPorHora = lambda;
        modelo.MediaEsperaHoras = 1 / lambda;
        modelo.ProbabilidadHastaTiempo = 1 - Math.Exp(-lambda * tiempo);
        modelo.PercentilSeleccionadoHoras = PercentilExponencial(lambda, percentil);
        modelo.LimiteInferior95Horas = PercentilExponencial(lambda, 0.025);
        modelo.LimiteSuperior95Horas = PercentilExponencial(lambda, 0.975);
        modelo.Advertencia = string.Join(" ", advertencias);
        modelo.Grafico = CrearCurvaExponencial(lambda);
        return modelo;
    }

    private static CurvaDistribucionViewModel CrearCurvaNormal(double media, double desviacion)
    {
        const int cantidadPuntos = 81;
        const int xInicial = 40;
        const int ancho = 720;
        const int yBase = 220;
        const int alto = 180;
        var minimo = media - 4 * desviacion;
        var maximo = media + 4 * desviacion;
        var maximoDensidad = 1 / (desviacion * Math.Sqrt(2 * Math.PI));
        var puntos = new List<PuntoCurvaViewModel>(cantidadPuntos);

        for (var indice = 0; indice < cantidadPuntos; indice++)
        {
            var proporcion = indice / (double)(cantidadPuntos - 1);
            var valor = minimo + (maximo - minimo) * proporcion;
            var z = (valor - media) / desviacion;
            var densidad = Math.Exp(-0.5 * z * z) / maximoDensidad;
            puntos.Add(new PuntoCurvaViewModel
            {
                X = xInicial + (int)Math.Round(proporcion * ancho),
                Y = yBase - (int)Math.Round(densidad * alto)
            });
        }

        return new CurvaDistribucionViewModel
        {
            EtiquetaEjeX = "Importe (Bs)",
            EtiquetaMinimo = minimo.ToString("N2", CultureInfo.CurrentCulture),
            EtiquetaMaximo = maximo.ToString("N2", CultureInfo.CurrentCulture),
            Puntos = puntos
        };
    }

    private static CurvaDistribucionViewModel CrearCurvaExponencial(double lambda)
    {
        const int cantidadPuntos = 81;
        const int xInicial = 40;
        const int ancho = 720;
        const int yBase = 220;
        const int alto = 180;
        var maximo = -Math.Log(0.005) / lambda;
        var maximoDensidad = lambda;
        var puntos = new List<PuntoCurvaViewModel>(cantidadPuntos);

        for (var indice = 0; indice < cantidadPuntos; indice++)
        {
            var proporcion = indice / (double)(cantidadPuntos - 1);
            var valor = maximo * proporcion;
            var densidad = lambda * Math.Exp(-lambda * valor) / maximoDensidad;
            puntos.Add(new PuntoCurvaViewModel
            {
                X = xInicial + (int)Math.Round(proporcion * ancho),
                Y = yBase - (int)Math.Round(densidad * alto)
            });
        }

        return new CurvaDistribucionViewModel
        {
            EtiquetaEjeX = "Tiempo entre compras (horas)",
            EtiquetaMinimo = "0",
            EtiquetaMaximo = maximo.ToString("N2", CultureInfo.CurrentCulture),
            Puntos = puntos
        };
    }

    private static IReadOnlyList<BarraProbabilidadViewModel> CrearBarrasProbabilidad(
        IEnumerable<(string Etiqueta, double? Probabilidad)> datos)
    {
        var valores = datos
            .Where(dato => dato.Probabilidad.HasValue)
            .Select(dato => (dato.Etiqueta, Probabilidad: dato.Probabilidad!.Value))
            .ToArray();
        var maximo = valores.Select(dato => dato.Probabilidad).DefaultIfEmpty(0d).Max();
        return valores
            .Select(dato => new BarraProbabilidadViewModel
            {
                Etiqueta = dato.Etiqueta,
                Probabilidad = dato.Probabilidad,
                AlturaPixels = maximo == 0
                    ? 0
                    : Math.Max(2, (int)Math.Round(dato.Probabilidad / maximo * 180))
            })
            .ToArray();
    }

    private static double FuncionDistribucionNormal(double z)
    {
        if (z <= -8)
        {
            return 0;
        }

        if (z >= 8)
        {
            return 1;
        }

        var x = Math.Abs(z) / Math.Sqrt(2);
        var t = 1 / (1 + 0.3275911 * x);
        var erf = 1
            - (((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t
                - 0.284496736) * t + 0.254829592) * t) * Math.Exp(-x * x);
        if (z < 0)
        {
            erf = -erf;
        }

        return LimitarProbabilidad((1 + erf) / 2);
    }

    private static double? InversaNormal(double probabilidad)
    {
        if (probabilidad <= 0 || probabilidad >= 1)
        {
            return null;
        }

        var inferior = -10d;
        var superior = 10d;
        for (var iteracion = 0; iteracion < 80; iteracion++)
        {
            var medio = (inferior + superior) / 2;
            if (FuncionDistribucionNormal(medio) < probabilidad)
            {
                inferior = medio;
            }
            else
            {
                superior = medio;
            }
        }

        return (inferior + superior) / 2;
    }

    private static double? PercentilExponencial(double lambda, double probabilidad)
    {
        if (probabilidad <= 0)
        {
            return 0;
        }

        if (probabilidad >= 1)
        {
            return null;
        }

        return -Math.Log(1 - probabilidad) / lambda;
    }

    private static double LimitarProbabilidad(double probabilidad)
    {
        return Math.Clamp(probabilidad, 0, 1);
    }
}
