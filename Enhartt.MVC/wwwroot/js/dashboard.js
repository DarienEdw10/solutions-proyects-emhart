let chartSPC = null;
let paginaActual = 1;
const registrosPorPagina = 5; // <-- Cambiado de 10 a 5 para no saturar
let datosCache = [];

document.addEventListener('DOMContentLoaded', function () {
    cargarCeldas();
    inicializarGrafica();
    cargarDatos(1);
});

// Cargar catálogo de celdas
async function cargarCeldas() {
    const select = document.getElementById('filtro-celda');
    try {
        const response = await fetch('/Home/ObtenerCeldas');
        const celdas = await response.json();

        select.innerHTML = '<option value="">-- Todas las Celdas --</option>';
        celdas.forEach(c => {
            const option = document.createElement('option');
            option.value = c.id;
            option.textContent = `${c.celda} (${c.idMaquina})`;
            select.appendChild(option);
        });
    } catch (e) {
        select.innerHTML = '<option value="">-- Error al cargar --</option>';
    }
}
// Cargar tarjetas superiores dinámicas según el catálogo de Celdas
async function cargarEstadoCeldas() {
    const contenedor = document.getElementById('contenedor-estado-celdas');
    try {
        const response = await fetch('/Home/ObtenerCeldas');
        const celdas = await response.json();

        if (!celdas || celdas.length === 0) {
            contenedor.innerHTML = '<div class="col-12 text-muted small text-center">No hay celdas registradas.</div>';
            return;
        }

        contenedor.innerHTML = '';
        celdas.forEach(c => {
            const col = document.createElement('div');
            col.className = 'col-12 col-md-6 col-xl-4';

            col.innerHTML = `
                <div class="card border-0 shadow-sm p-3 bg-white border-start border-success border-4 h-100">
                    <div class="d-flex align-items-center justify-content-between">
                        <div>
                            <span class="text-muted small fw-bold text-uppercase d-block">Celda Operativa</span>
                            <h5 class="fw-bold mb-0 text-dark">${c.celda}</h5>
                            <small class="text-muted">${c.idMaquina}</small>
                        </div>
                        <div class="text-end">
                            <span class="badge bg-success mb-1">🟢 OPERANDO</span>
                            <div class="small text-muted font-monospace">Último disparo: Activo</div>
                        </div>
                    </div>
                </div>
            `;
            contenedor.appendChild(col);
        });
    } catch (e) {
        contenedor.innerHTML = '<div class="col-12 text-danger small text-center">Error al cargar estado de celdas.</div>';
    }
}

// Llamar al iniciar la página en DOMContentLoaded
document.addEventListener('DOMContentLoaded', function () {
    cargarEstadoCeldas(); // <-- Carga celdas reales arriba
    cargarCeldas();       // <-- Carga dropdown de filtros
    inicializarGrafica();
    cargarDatos(1);
});

// Cargar telemetría paginada de 5 en 5
async function cargarDatos(pagina = 1) {
    paginaActual = pagina;

    const idMaquina = document.getElementById('filtro-celda').value;
    const estatus = document.getElementById('filtro-estatus').value;
    const fechaInicio = document.getElementById('filtro-fecha-inicio').value;
    const fechaFin = document.getElementById('filtro-fecha-fin').value;
    const busqueda = document.getElementById('buscador-global').value;

    const url = `/Home/ObtenerParametros?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${busqueda}&pagina=${pagina}&registrosPorPagina=${registrosPorPagina}`;

    try {
        const response = await fetch(url);
        const result = await response.json();

        if (result.success) {
            datosCache = result.data;
            renderizarKPIs(result.kpis);
            renderizarTabla(result.data);
            renderizarPaginacion(result.paginaActual, result.totalPaginas, result.total);
            actualizarGraficaSPC(result.data);
        }
    } catch (error) {
        console.error("Error al cargar datos:", error);
    }
}

function aplicarFiltros() {
    cargarDatos(1);
}

function renderizarKPIs(kpis) {
    document.getElementById('lbl-total-disparos').innerText = kpis.totalDisparos;
    document.getElementById('lbl-calidad-ok').innerText = kpis.calidadOk;
    document.getElementById('lbl-desviaciones-nok').innerText = kpis.desviacionesNok;
    document.getElementById('lbl-efectividad-ftt').innerText = kpis.efectividadFtt + '%';
}

function renderizarTabla(registros) {
    const tbody = document.getElementById('tabla-body');
    tbody.innerHTML = '';

    if (!registros || registros.length === 0) {
        tbody.innerHTML = `<tr><td colspan="11" class="text-center text-muted py-4">No hay registros de telemetría para los filtros seleccionados.</td></tr>`;
        return;
    }

    registros.forEach(r => {
        const tr = document.createElement('tr');
        tr.style.cursor = 'pointer';
        tr.onclick = () => abrirModalDetalle(r);

        const isOk = r.estatusCalidad === 'OK' || r.estatusCalidad === 'Solo OK';

        tr.innerHTML = `
            <td class="small font-monospace">${r.fechaFormatted}</td>
            <td><span class="badge bg-secondary">${r.celda}</span></td>
            <td>Salida ${r.salida || 1}</td>
            <td>Turno ${r.turno || 1}</td>
            <td class="fw-bold text-primary">#${r.numSol || r.idRegistro}</td>
            <td><strong>${r.corriente || 0}</strong> A</td>
            <td>${r.energia || 0} J</td>
            <td>${r.tiempo || 0} ms</td>
            <td>${r.penetracion || 0} mm</td>
            <td><span class="badge ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? '🟢 OK' : '🔴 NOK'}</span></td>
            <td class="small text-muted">${r.detallesFallas}</td>
        `;
        tbody.appendChild(tr);
    });
}

function renderizarPaginacion(actual, totalPaginas, totalRegistros) {
    const lblInfo = document.getElementById('lbl-paginacion-info');
    const ul = document.getElementById('ul-paginacion');

    const inicio = (actual - 1) * registrosPorPagina + 1;
    const fin = Math.min(actual * registrosPorPagina, totalRegistros);
    lblInfo.innerText = `Mostrando ${totalRegistros > 0 ? inicio : 0}-${fin} de ${totalRegistros} registros`;

    ul.innerHTML = '';
    if (totalPaginas <= 1) return;

    // Rango de páginas a mostrar alrededor de la actual (ej. 2 atrás, 2 adelante)
    const maxVisibles = 2;
    let inicioPag = Math.max(1, actual - maxVisibles);
    let finPag = Math.min(totalPaginas, actual + maxVisibles);

    // Botón "Primero / Anterior"
    if (actual > 1) {
        ul.appendChild(crearItemPaginacion('«', 1));
        ul.appendChild(crearItemPaginacion('‹', actual - 1));
    }

    // Puntos suspensivos si hay páginas antes
    if (inicioPag > 1) {
        ul.appendChild(crearItemInactivo('...'));
    }

    // Páginas numéricas acotadas
    for (let i = inicioPag; i <= finPag; i++) {
        const li = document.createElement('li');
        li.className = `page-item ${i === actual ? 'active' : ''}`;
        li.innerHTML = `<a class="page-link" href="#" onclick="cargarDatos(${i}); return false;">${i}</a>`;
        ul.appendChild(li);
    }

    // Puntos suspensivos si hay páginas después
    if (finPag < totalPaginas) {
        ul.appendChild(crearItemInactivo('...'));
    }

    // Botón "Siguiente / Último"
    if (actual < totalPaginas) {
        ul.appendChild(crearItemPaginacion('›', actual + 1));
        ul.appendChild(crearItemPaginacion('»', totalPaginas));
    }
}

function crearItemPaginacion(texto, pagina) {
    const li = document.createElement('li');
    li.className = 'page-item';
    li.innerHTML = `<a class="page-link" href="#" onclick="cargarDatos(${pagina}); return false;">${texto}</a>`;
    return li;
}

function crearItemInactivo(texto) {
    const li = document.createElement('li');
    li.className = 'page-item disabled';
    li.innerHTML = `<span class="page-link">${texto}</span>`;
    return li;
}
// -------------------------------------------------------------
// FUNCIONES DE EXPORTACIÓN (EXCEL / PDF)
// -------------------------------------------------------------
// Exportar el 100% de los registros filtrados a Excel (.CSV)
function exportarExcel() {
    const idMaquina = document.getElementById('filtro-celda').value;
    const estatus = document.getElementById('filtro-estatus').value;
    const fechaInicio = document.getElementById('filtro-fecha-inicio').value;
    const fechaFin = document.getElementById('filtro-fecha-fin').value;
    const busqueda = document.getElementById('buscador-global').value;

    const url = `/Home/ExportarExcel?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${encodeURIComponent(busqueda)}`;
    
    // Descargar el archivo directamente desde el servidor
    window.location.href = url;
}

// Exportar el 100% de los registros filtrados a PDF / Vista de Impresión
async function exportarPDF() {
    const idMaquina = document.getElementById('filtro-celda').value;
    const estatus = document.getElementById('filtro-estatus').value;
    const fechaInicio = document.getElementById('filtro-fecha-inicio').value;
    const fechaFin = document.getElementById('filtro-fecha-fin').value;
    const busqueda = document.getElementById('buscador-global').value;

    const url = `/Home/ObtenerTodosParaImpresion?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${encodeURIComponent(busqueda)}`;

    try {
        const response = await fetch(url);
        const result = await response.json();

        if (!result.success || !result.data || result.data.length === 0) {
            alert("No hay registros disponibles para exportar.");
            return;
        }

        const registros = result.data;

        // Construcción de la tabla completa para la ventana de impresión
        let filasHtml = '';
        registros.forEach(r => {
            const isOk = r.estatusCalidad === 'OK' || r.estatusCalidad === 'Solo OK';
            filasHtml += `
                <tr>
                    <td style="font-size: 11px;">${r.fechaFormatted}</td>
                    <td>${r.celda}</td>
                    <td>Salida ${r.salida}</td>
                    <td>Turno ${r.turno}</td>
                    <td><b>#${r.numSol}</b></td>
                    <td><b>${r.corriente}</b> A</td>
                    <td>${r.energia} J</td>
                    <td>${r.tiempo} ms</td>
                    <td>${r.penetracion} mm</td>
                    <td><span class="badge ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? 'OK' : 'NOK'}</span></td>
                    <td style="font-size: 11px;">${r.detallesFallas}</td>
                </tr>
            `;
        });

        const ventanaImpresion = window.open('', '', 'height=800,width=1100');
        ventanaImpresion.document.write(`
            <html>
                <head>
                    <title>Reporte Completo de Soldaduras (${registros.length} registros)</title>
                    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
                </head>
                <body class="p-4">
                    <div class="d-flex justify-content-between align-items-center mb-3">
                        <h4>Reporte Completo de Telemetría - Celdas Tucker</h4>
                        <span class="badge bg-primary fs-6">Total: ${registros.length} registros</span>
                    </div>
                    <table class="table table-striped table-bordered align-middle text-nowrap">
                        <thead class="table-dark">
                            <tr>
                                <th>Fecha / Hora</th>
                                <th>Celda</th>
                                <th>Salida</th>
                                <th>Turno</th>
                                <th>N° Soldadura</th>
                                <th>Corriente</th>
                                <th>Energía</th>
                                <th>Tiempo</th>
                                <th>Penetración</th>
                                <th>Estatus</th>
                                <th>Detalle Desviación</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${filasHtml}
                        </tbody>
                    </table>
                </body>
            </html>
        `);
        ventanaImpresion.document.close();

        setTimeout(() => {
            ventanaImpresion.focus();
            ventanaImpresion.print();
            ventanaImpresion.close();
        }, 600);

    } catch (error) {
        console.error("Error al exportar PDF:", error);
        alert("Ocurrió un error al preparar el reporte completo.");
    }
}

// Gráfica SPC y Modal
function inicializarGrafica() {
    const ctx = document.getElementById('graficaControl')?.getContext('2d');
    if (!ctx) return;

    chartSPC = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'Corriente (A)',
                    data: [],
                    borderColor: '#0d6efd',
                    backgroundColor: 'rgba(13, 110, 253, 0.1)',
                    fill: true,
                    tension: 0.2
                },
                {
                    label: 'Límite Máx (Max)',
                    data: [],
                    borderColor: '#dc3545',
                    borderDash: [5, 5],
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'Límite Mín (Min)',
                    data: [],
                    borderColor: '#ffc107',
                    borderDash: [5, 5],
                    fill: false,
                    pointRadius: 0
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'top' } },
            scales: { y: { beginAtZero: false } }
        }
    });
}

function actualizarGraficaSPC(registros) {
    if (!chartSPC) return;

    const ultimos = [...registros].reverse();
    chartSPC.data.labels = ultimos.map(r => `#${r.numSol || r.idRegistro}`);
    chartSPC.data.datasets[0].data = ultimos.map(r => r.corriente || 0);
    chartSPC.data.datasets[1].data = ultimos.map(() => 1330);
    chartSPC.data.datasets[2].data = ultimos.map(() => 1270);
    chartSPC.update();
}

function abrirModalDetalle(r) {
    document.getElementById('modal-num-sol').innerText = `#${r.numSol || r.idRegistro}`;
    document.getElementById('modal-serial').innerText = r.serialTrazabilidad || 'N/A';
    document.getElementById('modal-fecha').innerText = r.fechaFormatted;

    const isOk = r.estatusCalidad === 'OK' || r.estatusCalidad === 'Solo OK';
    document.getElementById('modal-estatus-badge').innerHTML = `<span class="badge fs-6 ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? '🟢 OK' : '🔴 NOK'}</span>`;

    document.getElementById('modal-volarc').innerText = r.volArc || 0;
    document.getElementById('modal-volpri').innerText = r.volPri || 0;
    document.getElementById('modal-corriente').innerText = r.corriente || 0;
    document.getElementById('modal-energia').innerText = r.energia || 0;
    document.getElementById('modal-tiempo').innerText = r.tiempo || 0;
    document.getElementById('modal-penetracion').innerText = r.penetracion || 0;
    document.getElementById('modal-elevacion').innerText = r.elevacion || 0;
    document.getElementById('modal-lonper').innerText = r.lonPer || 0;

    document.getElementById('modal-err').innerText = `Err: ${r.err || 0}`;
    document.getElementById('modal-alarma').innerText = `Alarma: ${r.alarma || 0}`;
    document.getElementById('modal-modo').innerText = `Modo: Auto (${r.modo || 1})`;

    const alertDiv = document.getElementById('modal-alert-desviaciones');
    if (!isOk) {
        alertDiv.classList.remove('d-none');
        document.getElementById('modal-detalles-fallas').innerText = r.detallesFallas;
    } else {
        alertDiv.classList.add('d-none');
    }

    const modal = new bootstrap.Modal(document.getElementById('modalDetalleSoldadura'));
    modal.show();
}