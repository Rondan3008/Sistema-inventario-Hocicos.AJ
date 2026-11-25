(() => {
    const root = document.getElementById("admin-estadisticas-graficas");
    if (!root) {
        return;
    }

    const errorMessage = document.getElementById("admin-estadisticas-error");
    const loadingMessage = document.getElementById("admin-estadisticas-cargando");
    const charts = new Map();
    const style = getComputedStyle(document.documentElement);
    const colors = [
        style.getPropertyValue("--hocicos-azul").trim(),
        style.getPropertyValue("--hocicos-naranja").trim(),
        style.getPropertyValue("--hocicos-rojo").trim(),
        style.getPropertyValue("--hocicos-amarillo").trim()
    ];
    const numberFormat = new Intl.NumberFormat("es-BO", { maximumFractionDigits: 0 });
    const moneyFormat = new Intl.NumberFormat("es-BO", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const money = value => `Bs ${moneyFormat.format(value)}`;
    const points = values => values.map(item => ({
        label: item.etiqueta,
        value: Number(item.valor)
    }));

    function setEmptyState(key, hasData) {
        const canvas = document.querySelector(`[data-chart-canvas="${key}"]`);
        const message = document.querySelector(`[data-chart-empty="${key}"]`);
        if (canvas) {
            canvas.parentElement.hidden = !hasData;
        }
        if (message) {
            message.hidden = hasData;
        }
    }

    function clearCharts() {
        for (const chart of charts.values()) {
            chart.destroy();
        }
        charts.clear();
        document.querySelectorAll("[data-chart-canvas]").forEach(canvas => {
            canvas.parentElement.hidden = true;
        });
        document.querySelectorAll("[data-chart-empty]").forEach(message => {
            message.hidden = true;
        });
    }

    function draw(key, data, type, datasetLabel, color, options = {}) {
        const canvas = document.querySelector(`[data-chart-canvas="${key}"]`);
        if (!canvas) {
            return;
        }

        const hasData = data.length > 0;
        setEmptyState(key, hasData);
        const existingChart = charts.get(key);
        if (existingChart) {
            existingChart.destroy();
            charts.delete(key);
        }
        if (!hasData) {
            return;
        }

        const chart = new Chart(canvas, {
            type,
            data: {
                labels: data.map(item => item.label),
                datasets: [{
                    label: datasetLabel,
                    data: data.map(item => item.value),
                    backgroundColor: color,
                    borderColor: color,
                    borderWidth: type === "line" ? 2 : 1,
                    ...(type === "line" ? {
                        pointBackgroundColor: color,
                        pointRadius: 3,
                        tension: 0.2
                    } : {}),
                    ...options.dataset
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: type === "pie", position: "bottom" },
                    tooltip: options.tooltip || {},
                    ...options.plugins
                },
                ...options.chart
            }
        });
        charts.set(key, chart);
    }

    function drawAll(data) {
        const units = points(data.unidadesPorProducto);
        draw("unidadesPorProducto", units, "bar", "Unidades", colors[0], {
            chart: {
                indexAxis: "y",
                scales: {
                    x: {
                        beginAtZero: true,
                        title: { display: true, text: "Unidades" },
                        ticks: { callback: value => numberFormat.format(value) }
                    }
                }
            }
        });

        const productAmounts = points(data.importePorProducto);
        draw("importePorProducto", productAmounts, "bar", "Importe registrado (Bs)", colors[1], {
            chart: {
                indexAxis: "y",
                scales: {
                    x: {
                        beginAtZero: true,
                        title: { display: true, text: "Importe registrado (Bs)" },
                        ticks: { callback: value => money(value) }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: context => `${context.dataset.label}: ${money(context.parsed.x)}`
                        }
                    }
                }
            }
        });

        const categories = points(data.participacionPorCategoria);
        draw("participacionPorCategoria", categories, "pie", "Importe registrado", colors[0], {
            dataset: {
                backgroundColor: categories.map((_, index) => colors[index % colors.length]),
                borderColor: style.getPropertyValue("--hocicos-blanco").trim(),
                borderWidth: 2
            },
            tooltip: {
                callbacks: {
                    label: context => {
                        const total = context.dataset.data.reduce((sum, value) => sum + Number(value), 0);
                        const share = total > 0 ? Number(context.parsed) / total * 100 : 0;
                        return `${context.label}: ${share.toLocaleString("es-BO", {
                            minimumFractionDigits: 2,
                            maximumFractionDigits: 2
                        })}% (${money(context.parsed)})`;
                    }
                }
            }
        });

        const histogram = points(data.histogramaImportes);
        draw("histogramaImportes", histogram, "bar", "Compras", colors[0], {
            dataset: { categoryPercentage: 1, barPercentage: 1 },
            chart: {
                scales: {
                    x: {
                        title: { display: true, text: "Intervalo del importe (Bs)" },
                        ticks: { maxRotation: 60, minRotation: 30 }
                    },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Frecuencia (compras)" },
                        ticks: { callback: value => numberFormat.format(value) }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            title: items => `Importe: ${items[0].label}`,
                            label: context => `${numberFormat.format(context.parsed.y)} compras`
                        }
                    }
                }
            }
        });

        const purchasesByDate = points(data.comprasPorFecha);
        draw("comprasPorFecha", purchasesByDate, "line", "Compras registradas", colors[0], {
            chart: {
                scales: {
                    x: { title: { display: true, text: "Fecha" } },
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: "Compras (cantidad)" },
                        ticks: { callback: value => numberFormat.format(value) }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: context => `${numberFormat.format(context.parsed.y)} compras`
                        }
                    }
                }
            }
        });

        const paymentMethods = points(data.metodosPago);
        draw("metodosPago", paymentMethods, "bar", "Comprobantes", colors[2], {
            chart: {
                indexAxis: "y",
                scales: {
                    x: {
                        beginAtZero: true,
                        title: { display: true, text: "Comprobantes (frecuencia)" },
                        ticks: { callback: value => numberFormat.format(value) }
                    }
                }
            }
        });
    }

    let activeRequest;
    async function loadCharts() {
        if (!window.Chart) {
            loadingMessage.hidden = true;
            clearCharts();
            errorMessage.textContent = "No se pudo cargar Chart.js desde el recurso local.";
            errorMessage.classList.remove("d-none");
            return;
        }

        if (activeRequest) {
            activeRequest.abort();
        }
        activeRequest = new AbortController();
        errorMessage.classList.add("d-none");
        loadingMessage.hidden = false;
        clearCharts();

        const url = new URL(root.dataset.endpoint, window.location.origin);
        const desde = document.querySelector('input[name="Filtros.Desde"]')?.value;
        const hasta = document.querySelector('input[name="Filtros.Hasta"]')?.value;
        if (desde) {
            url.searchParams.set("desde", desde);
        }
        if (hasta) {
            url.searchParams.set("hasta", hasta);
        }

        try {
            const response = await fetch(url, {
                headers: { Accept: "application/json" },
                signal: activeRequest.signal
            });
            if (!response.ok) {
                throw new Error(`El servidor respondió con estado ${response.status}.`);
            }
            drawAll(await response.json());
            loadingMessage.hidden = true;
        } catch (error) {
            if (error.name === "AbortError") {
                return;
            }
            console.error("No se pudieron cargar las gráficas administrativas.", error);
            loadingMessage.hidden = true;
            errorMessage.textContent = "No fue posible cargar las gráficas. Revise las fechas e inténtelo nuevamente.";
            errorMessage.classList.remove("d-none");
        }
    }

    document.querySelectorAll('input[name="Filtros.Desde"], input[name="Filtros.Hasta"]')
        .forEach(input => input.addEventListener("change", loadCharts));

    loadCharts();
})();
