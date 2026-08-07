// Variables globales
let datosCompletosAPI = [];
let miGraficaChart = null;
let modalDetalleBS = null;

// Configuración de Paginación y Filtros en Servidor
let paginaActual = 1;
const registrosPorPagina = 5;

// URLs base de la API en .NET 8
const API_URL = "http://localhost:5240/api/parametros";
const API_KPIS_URL = "http://localhost:5240/api/parametros/kpis";

// =====================================================================
// CONSUMO DE KPIS Y TARJETAS EN TIEMPO REAL
// =====================================================================
async function obtenerKPIsAPI() {
    try {
        const respuesta = await fetch(API_KPIS_URL);
        if (!respuesta.ok) throw new Error(`Error HTTP KPI: ${respuesta.status}`);

        const data = await respuesta.json();

        // 1. Actualizar Tarjetas KPI principales
        const elTotal = document.getElementById("lbl-total-disparos");
        const elOk = document.getElementById("lbl-calidad-ok");
        const elNok = document.getElementById("lbl-desviaciones-nok");
        const elFtt = document.getElementById("lbl-efectividad-ftt");

        if (elTotal) elTotal.innerText = data.totalDisparos.toLocaleString();
        if (elOk) elOk.innerText = data.calidadOk.toLocaleString();
        if (elNok) elNok.innerText = data.desviacionesNok.toLocaleString();
        if (elFtt) elFtt.innerText = `${data.efectividadFtt}%`;

        // 2. Actualizar Tiempos Relativos y Estatus de Celdas
        const elTiempoC1 = document.getElementById("lbl-tiempo-celda-1");
        const elTiempoC3 = document.getElementById("lbl-tiempo-celda-3");
        const elBadgeC1 = document.getElementById("badge-estatus-celda-1");
        const elBadgeC3 = document.getElementById("badge-estatus-celda-3");

        if (elTiempoC1) elTiempoC1.innerText = `Último disparo: ${data.ultimoDisparoCelda1}`;
        if (elTiempoC3) elTiempoC3.innerText = `Último disparo: ${data.ultimoDisparoCelda3}`;

        if (elBadgeC1) {
            elBadgeC1.innerText = data.estatusCelda1;
            elBadgeC1.className = data.estatusCelda1.includes("OPERANDO") 
                ? "badge bg-success" 
                : "badge bg-warning text-dark";
        }

        if (elBadgeC3) {
            elBadgeC3.innerText = data.estatusCelda3;
            elBadgeC3.className = data.estatusCelda3.includes("OPERANDO") 
                ? "badge bg-success" 
                : "badge bg-warning text-dark";
        }

    } catch (error) {
        console.error("Error al obtener KPIs:", error);
    }
}

// =====================================================================
// CONSUMO ASÍNCRONO CON FILTRADO Y PAGINACIÓN EN SERVIDOR
// =====================================================================
async function obtenerDatosAPI(pagina = 1) {
    try {
        // Capturar valores de los inputs de filtro (Fecha Inicio y Fecha Fin)
        const celda = document.getElementById("filtro-celda")?.value || "";
        const estatus = document.getElementById("filtro-estatus")?.value || "";
        const fechaInicio = document.getElementById("filtro-fecha-inicio")?.value || "";
        const fechaFin = document.getElementById("filtro-fecha-fin")?.value || "";
        const busqueda = document.getElementById("buscador-global")?.value.trim() || "";

        // Construir URL dinámica con Query Parameters
        let queryParams = new URLSearchParams({
            pagina: pagina,
            tamano: registrosPorPagina
        });

        if (celda) queryParams.append("celda", celda);
        if (estatus) queryParams.append("estatus", estatus);
        if (fechaInicio) queryParams.append("fechaInicio", fechaInicio);
        if (fechaFin) queryParams.append("fechaFin", fechaFin);
        if (busqueda) queryParams.append("busqueda", busqueda);

        const urlPaginada = `${API_URL}?${queryParams.toString()}`;
        const respuesta = await fetch(urlPaginada);
        if (!respuesta.ok) throw new Error(`Error HTTP: ${respuesta.status}`);

        const resultadoPaginado = await respuesta.json();
        
        datosCompletosAPI = resultadoPaginado.elementos;
        paginaActual = resultadoPaginado.paginaActual;

        renderizarTablaServidor(resultadoPaginado);
    } catch (error) {
        console.error("Error al conectar con la API:", error);
        document.getElementById("tabla-body").innerHTML = `
            <tr>
                <td colspan="11" class="text-center py-4 text-danger fw-bold">
                    ⚠️ No se pudo conectar con la API de .NET 8 (${API_URL}). 
                    Verifica que el servicio esté corriendo.
                </td>
            </tr>`;
    }
    


}

// =====================================================================
// RENDERIZADO DE TABLA Y PAGINADOR ACOTADO (VENTANA DESLIZANTE)
// =====================================================================
function renderizarTablaServidor(respuestaPaginada) {
    const datos = respuestaPaginada.elementos;
    const tbody = document.getElementById("tabla-body");
    tbody.innerHTML = "";

    const totalRegistros = respuestaPaginada.totalRegistros;
    const totalPaginas = respuestaPaginada.totalPaginas;

    if (datos.length === 0) {
        tbody.innerHTML = `<tr><td colspan="11" class="text-center py-4 text-muted">No se encontraron registros que coincidan con la búsqueda.</td></tr>`;
    } else {
        datos.forEach((row) => {
            const tr = document.createElement("tr");
            tr.style.cursor = "pointer";
            if (row.estatus === "NOK") tr.classList.add("fila-nok");

            const badgeCalidad = row.estatus === "OK" 
                ? `<span class="badge bg-success">🟢 OK</span>` 
                : `<span class="badge bg-danger">🔴 NOK</span>`;

            const detalle = row.detalles 
                ? `<small class="text-danger fw-bold">${row.detalles}</small>` 
                : `<span class="text-muted">-</span>`;

            tr.innerHTML = `
                <td><small>${row.fecha}</small></td>
                <td><span class="badge bg-secondary">${row.celda}</span></td>
                <td><span class="badge bg-dark">${row.salida}</span></td>
                <td><span class="badge bg-light text-dark border">${row.turno}</span></td>
                <td><strong>#${row.numSol}</strong></td>
                <td>${row.corriente} A</td>
                <td>${row.energia} J</td>
                <td>${row.tiempo} ms</td>
                <td>${row.penetracion} mm</td>
                <td>${badgeCalidad}</td>
                <td>${detalle}</td>
            `;

            tr.addEventListener("click", () => abrirFichaTecnica(row));
            tbody.appendChild(tr);
        });
    }

    const inicio = totalRegistros === 0 ? 0 : ((paginaActual - 1) * registrosPorPagina) + 1;
    const fin = Math.min(inicio + datos.length - 1, totalRegistros);

    document.getElementById("lbl-paginacion-info").innerText = `Mostrando ${inicio}-${fin} de ${totalRegistros} registros`;
    
    // Renderizar los botones de paginación de forma acotada
    renderizarPaginadorAcotadoUI(totalPaginas);

    // Actualizar la gráfica con los datos visibles
    inicializarGrafica(datos);
}

// Renderiza un máximo de 5 números de página más los botones Anterior/Siguiente
function renderizarPaginadorAcotadoUI(totalPaginas) {
    const ul = document.getElementById("ul-paginacion");
    ul.innerHTML = "";

    if (totalPaginas <= 1) return;

    // Botón "Anterior"
    const liAnt = document.createElement("li");
    liAnt.className = `page-item ${paginaActual === 1 ? "disabled" : ""}`;
    liAnt.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${paginaActual - 1}); return false;">&laquo;</a>`;
    ul.appendChild(liAnt);

    let maxBotones = 5;
    let inicioVentana = Math.max(1, paginaActual - Math.floor(maxBotones / 2));
    let finVentana = inicioVentana + maxBotones - 1;

    if (finVentana > totalPaginas) {
        finVentana = totalPaginas;
        inicioVentana = Math.max(1, finVentana - maxBotones + 1);
    }

    if (inicioVentana > 1) {
        ul.appendChild(crearItemPagina(1));
        if (inicioVentana > 2) {
            const liDots = document.createElement("li");
            liDots.className = "page-item disabled";
            liDots.innerHTML = `<span class="page-link">...</span>`;
            ul.appendChild(liDots);
        }
    }

    for (let i = inicioVentana; i <= finVentana; i++) {
        ul.appendChild(crearItemPagina(i));
    }

    if (finVentana < totalPaginas) {
        if (finVentana < totalPaginas - 1) {
            const liDots = document.createElement("li");
            liDots.className = "page-item disabled";
            liDots.innerHTML = `<span class="page-link">...</span>`;
            ul.appendChild(liDots);
        }
        ul.appendChild(crearItemPagina(totalPaginas));
    }

    // Botón "Siguiente"
    const liSig = document.createElement("li");
    liSig.className = `page-item ${paginaActual === totalPaginas ? "disabled" : ""}`;
    liSig.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${paginaActual + 1}); return false;">&raquo;</a>`;
    ul.appendChild(liSig);
}

function crearItemPagina(numero) {
    const li = document.createElement("li");
    li.className = `page-item ${numero === paginaActual ? "active" : ""}`;
    li.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${numero}); return false;">${numero}</a>`;
    return li;
}

async function cambiarPagina(num) {
    if (num < 1 || num === paginaActual) return;
    await obtenerDatosAPI(num);
}

// Aplicar filtros reiniciando a la página 1
function aplicarFiltros() {
    paginaActual = 1;
    obtenerDatosAPI(1);
}

// =====================================================================
// FICHA TÉCNICA DETALLADA (MODAL)
// =====================================================================
function abrirFichaTecnica(data) {
    document.getElementById("modal-num-sol").innerText = `#${data.numSol}`;
    document.getElementById("modal-serial").innerText = `${data.celda}-${data.fecha.replace(/[-: ]/g,"")}-${data.salida}-P${data.programa}-G${data.numSol}`;
    document.getElementById("modal-fecha").innerText = data.fecha;
    
    document.getElementById("modal-estatus-badge").innerHTML = data.estatus === "OK" 
        ? `<span class="badge bg-success">🟢 CALIDAD CONFORME (OK)</span>` 
        : `<span class="badge bg-danger">🔴 DESVIACIÓN DETECTADA (NOK)</span>`;

    document.getElementById("modal-volarc").innerText = data.volArc ?? 0;
    document.getElementById("modal-volpri").innerText = data.volPri ?? 0;
    document.getElementById("modal-corriente").innerText = data.corriente ?? 0;
    document.getElementById("modal-energia").innerText = data.energia ?? 0;
    document.getElementById("modal-tiempo").innerText = data.tiempo ?? 0;
    document.getElementById("modal-penetracion").innerText = data.penetracion ?? 0;
    document.getElementById("modal-elevacion").innerText = data.elevacion ?? 0;
    document.getElementById("modal-lonper").innerText = data.lonPer ?? 0;

    document.getElementById("modal-err").innerText = `Cód. Error: ${data.err ?? 0}`;
    document.getElementById("modal-alarma").innerText = `Alarma: ${data.alarma ?? 0}`;

    const containerAlert = document.getElementById("modal-alert-desviaciones");
    if (data.estatus === "NOK" && data.detalles) {
        containerAlert.classList.remove("d-none");
        containerAlert.classList.add("bg-danger", "text-white");
        document.getElementById("modal-detalles-fallas").innerText = data.detalles;
    } else {
        containerAlert.classList.add("d-none");
    }

    modalDetalleBS.show();
}

// =====================================================================
// GRÁFICA DE TENDENCIA SPC
// =====================================================================
function inicializarGrafica(datos) {
    const ctx = document.getElementById('graficaControl').getContext('2d');
    const datosInvertidos = [...datos].reverse();
    const etiquetas = datosInvertidos.map(d => `#${d.numSol}`);
    const corrientes = datosInvertidos.map(d => d.corriente);

    if (miGraficaChart) miGraficaChart.destroy();

    miGraficaChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: etiquetas,
            datasets: [
                {
                    label: 'Corriente Real (A)',
                    data: corrientes,
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    borderWidth: 2,
                    tension: 0.2,
                    fill: true
                },
                { label: 'Máx (1350A)', data: Array(etiquetas.length).fill(1350), borderColor: '#dc3545', borderWidth: 1.5, borderDash: [5, 5], pointRadius: 0 },
                { label: 'Mín (1200A)', data: Array(etiquetas.length).fill(1200), borderColor: '#dc3545', borderWidth: 1.5, borderDash: [5, 5], pointRadius: 0 }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top' } },
            scales: { y: { min: 800, max: 1500, title: { display: true, text: 'Amperes (A)' } } }
        }
    });
}

// =====================================================================
// EXPORTACIONES COMPLETAS
// =====================================================================
async function obtenerTodosLosDatosFiltradosAPI() {
    const celda = document.getElementById("filtro-celda")?.value || "";
    const estatus = document.getElementById("filtro-estatus")?.value || "";
    const fechaInicio = document.getElementById("filtro-fecha-inicio")?.value || "";
    const fechaFin = document.getElementById("filtro-fecha-fin")?.value || "";
    const busqueda = document.getElementById("buscador-global")?.value.trim() || "";

    let queryParams = new URLSearchParams({
        pagina: 1,
        tamano: 100000
    });

    if (celda) queryParams.append("celda", celda);
    if (estatus) queryParams.append("estatus", estatus);
    if (fechaInicio) queryParams.append("fechaInicio", fechaInicio);
    if (fechaFin) queryParams.append("fechaFin", fechaFin);
    if (busqueda) queryParams.append("busqueda", busqueda);

    const respuesta = await fetch(`${API_URL}?${queryParams.toString()}`);
    if (!respuesta.ok) throw new Error(`Error HTTP: ${respuesta.status}`);

    const resultado = await respuesta.json();
    return resultado.elementos;
}

// EXPORTACIÓN A EXCEL DE TODOS LOS REGISTROS FILTRADOS
async function exportarExcel() {
    try {
        const datosParaExportar = await obtenerTodosLosDatosFiltradosAPI();

        if (!datosParaExportar || datosParaExportar.length === 0) {
            return alert("No hay datos disponibles para exportar con los filtros actuales.");
        }

        const dataExcel = datosParaExportar.map(i => ({
            "Fecha": i.fecha,
            "Celda": i.celda,
            "Salida": i.salida,
            "Turno": i.turno,
            "N° Soldadura": i.numSol,
            "Corriente (A)": i.corriente,
            "Energía (J)": i.energia,
            "Tiempo (ms)": i.tiempo,
            "Penetración (mm)": i.penetracion,
            "Estatus": i.estatus,
            "Desviaciones": i.detalles || "OK"
        }));

        const ws = XLSX.utils.json_to_sheet(dataExcel);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, "Tucker");
        XLSX.writeFile(wb, `Reporte_Tucker_${new Date().toISOString().slice(0, 10)}.xlsx`);
    } catch (error) {
        console.error("Error al exportar Excel:", error);
        alert("Ocurrió un error al generar el archivo Excel.");
    }
}

// EXPORTACIÓN A PDF DE TODOS LOS REGISTROS FILTRADOS
async function exportarPDF() {
    try {
        const datosParaExportar = await obtenerTodosLosDatosFiltradosAPI();

        if (!datosParaExportar || datosParaExportar.length === 0) {
            return alert("No hay datos disponibles para exportar con los filtros actuales.");
        }

        const { jsPDF } = window.jspdf;
        const doc = new jsPDF({ orientation: "landscape" });

        doc.setFontSize(14);
        doc.text("AUTOTEK MÉXICO - REPORTE CELDAS TUCKER", 14, 15);

        const filasPDF = datosParaExportar.map(i => [
            i.fecha,
            i.celda,
            i.salida,
            i.turno,
            `#${i.numSol}`,
            `${i.corriente} A`,
            `${i.energia} J`,
            `${i.tiempo} ms`,
            `${i.penetracion} mm`,
            i.estatus,
            i.detalles || "-"
        ]);

        doc.autoTable({
            startY: 22,
            head: [["Fecha", "Celda", "Salida", "Turno", "N° Sol.", "Corriente", "Energía", "Tiempo", "Penetración", "Estatus", "Desviaciones"]],
            body: filasPDF,
            theme: "striped",
            headStyles: { fillColor: [33, 37, 41] },
            styles: { fontSize: 8 }
        });

        doc.save(`Reporte_Tucker_${new Date().toISOString().slice(0, 10)}.pdf`);
    } catch (error) {
        console.error("Error al exportar PDF:", error);
        alert("Ocurrió un error al generar el archivo PDF.");
    }
}

// Inicialización
document.addEventListener("DOMContentLoaded", () => {
    modalDetalleBS = new bootstrap.Modal(document.getElementById('modalDetalleSoldadura'));
    
    // Escuchar el click en el botón de Filtrar Datos y Enter en la caja de búsqueda
    document.querySelector("button[onclick='aplicarFiltros()']")?.addEventListener("click", aplicarFiltros);
    document.getElementById("buscador-global")?.addEventListener("keyup", (e) => {
        if (e.key === "Enter") aplicarFiltros();
    });

    obtenerKPIsAPI();
    obtenerDatosAPI(1);
});
let debounceTimer = null;

// Escuchar la escritura en tiempo real sobre la caja de Búsqueda Rápida
document.getElementById("buscador-global")?.addEventListener("input", (e) => {
    // Cancela la petición anterior si el usuario sigue escribiendo
    clearTimeout(debounceTimer);

    // Espera 350ms desde la última tecla presionada para ejecutar la búsqueda
    debounceTimer = setTimeout(() => {
        paginaActual = 1;
        obtenerDatosAPI(1);
    }, 350);
});