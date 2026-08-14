let chartSPC = null;
let paginaActual = 1;
const registrosPorPagina = 5;
let datosCache = [];

document.addEventListener('DOMContentLoaded', function () {
    cargarCeldas();
    inicializarGrafica();
    cargarDatos(1);

    // Escuchar cambios en el filtro de celda para actualizar la tarjeta superior
    const selectCelda = document.getElementById('filtro-celda');
    if (selectCelda) {
        selectCelda.addEventListener('change', function () {
            actualizarTarjetaCeldaFiltrada();
        });
    }
});

// Cargar catálogo de celdas en el selector superior
async function cargarCeldas() {
    const select = document.getElementById('filtro-celda');
    if (!select) return;

    try {
        const response = await fetch('/Home/ObtenerCeldas');
        const celdas = await response.json();

        select.innerHTML = '<option value="">-- Todas las Celdas --</option>';
        celdas.forEach(c => {
            const option = document.createElement('option');
            option.value = c.id || c.Id;
            option.textContent = `${c.celda || c.Celda} (${c.idMaquina || c.IdMaquina})`;
            select.appendChild(option);
        });
    } catch (e) {
        select.innerHTML = '<option value="">-- Error al cargar celdas --</option>';
    }
}

// Actualizar el texto y estado de la tarjeta dinámica según la Celda Filtrada
function actualizarTarjetaCeldaFiltrada() {
    const selectCelda = document.getElementById('filtro-celda');
    const lblNombre = document.getElementById('lbl-celda-activa-nombre');
    const lblId = document.getElementById('lbl-celda-activa-id');

    if (!selectCelda || !lblNombre || !lblId) return;

    const idSeleccionado = selectCelda.value;
    const textoOption = selectCelda.options[selectCelda.selectedIndex]?.text || "";

    if (!idSeleccionado) {
        lblNombre.innerText = "TODAS LAS CELDAS";
        lblId.innerText = "MONITOREO GLOBAL";
    } else {
        lblNombre.innerText = textoOption.toUpperCase();
        lblId.innerText = `MÁQUINA SELECCIONADA (ID: ${idSeleccionado})`;
    }
}

// Cargar telemetría paginada de forma segura
async function cargarDatos(pagina = 1) {
    paginaActual = pagina;

    const selectCelda = document.getElementById('filtro-celda');
    const idMaquina = selectCelda ? selectCelda.value : '';
    const estatus = document.getElementById('filtro-estatus')?.value || '';
    const fechaInicio = document.getElementById('filtro-fecha-inicio')?.value || '';
    const fechaFin = document.getElementById('filtro-fecha-fin')?.value || '';
    const busqueda = document.getElementById('buscador-global')?.value || '';

    actualizarTarjetaCeldaFiltrada();

    const url = `/Home/ObtenerParametros?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${encodeURIComponent(busqueda)}&pagina=${pagina}&registrosPorPagina=${registrosPorPagina}`;

    try {
        const response = await fetch(url);
        const result = await response.json();

        if (result.success) {
            datosCache = result.data || [];
            
            // 1. Renderizar KPIs
            renderizarKPIs(result.kpis);

            // 2. Renderizar Tabla y Paginación
            renderizarTabla(datosCache);
            
            const totalReg = result.total ?? result.Total ?? datosCache.length;
            const totalPag = result.totalPaginas ?? result.TotalPaginas ?? Math.ceil(totalReg / registrosPorPagina);
            const pagAct = result.paginaActual ?? result.PaginaActual ?? pagina;

            renderizarPaginacion(pagAct, totalPag, totalReg);

            // 3. Renderizar Gráfica SPC protegida
            try {
                actualizarGraficaSPC(datosCache);
            } catch (errChart) {
                console.warn("Advertencia al actualizar gráfica SPC:", errChart);
            }
        }
    } catch (error) {
        console.error("Error al cargar datos de telemetría:", error);
    }
}

function aplicarFiltros() {
    cargarDatos(1);
}

function renderizarKPIs(kpis) {
    if (!kpis) return;

    const elTotal = document.getElementById('lbl-total-disparos');
    const elOk = document.getElementById('lbl-calidad-ok');
    const elNok = document.getElementById('lbl-desviaciones-nok');
    const elCpk = document.getElementById('lbl-spc-cpk');
    const elCp = document.getElementById('lbl-spc-cp');

    if (elTotal) elTotal.innerText = kpis.totalDisparos ?? kpis.TotalDisparos ?? 0;
    if (elOk) elOk.innerText = kpis.calidadOk ?? kpis.CalidadOk ?? 0;
    if (elNok) elNok.innerText = kpis.desviacionesNok ?? kpis.DesviacionesNok ?? 0;
    
    // Soporte para KPIs de Cp y Cpk directos si vienen calculados del backend
    if (elCpk && (kpis.spcCpk !== undefined || kpis.SpcCpk !== undefined)) {
        const cpkVal = kpis.spcCpk ?? kpis.SpcCpk ?? 0;
        elCpk.innerText = cpkVal.toFixed(2);
    }
    if (elCp && (kpis.spcCp !== undefined || kpis.SpcCp !== undefined)) {
        const cpVal = kpis.spcCp ?? kpis.SpcCp ?? 0;
        elCp.innerText = cpVal.toFixed(2);
    }
}

function renderizarTabla(registros) {
    const tbody = document.getElementById('tabla-body');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (!registros || registros.length === 0) {
        tbody.innerHTML = `<tr><td colspan="11" class="text-center text-muted py-4">No hay registros de telemetría para los filtros seleccionados.</td></tr>`;
        return;
    }

    registros.forEach(r => {
        const tr = document.createElement('tr');
        tr.style.cursor = 'pointer';
        tr.onclick = () => abrirModalDetalle(r);

        const estatusVal = r.estatusCalidad || r.EstatusCalidad || r.estatus || r.Estatus || '';
        const isOk = estatusVal === 'OK' || estatusVal === 'Solo OK';
        const numSolVal = r.numSol ?? r.NumSol ?? r.idRegistro ?? r.IdRegistro ?? 0;
        const celdaVal = r.celda || r.Celda || 'N/A';
        const fechaVal = r.fechaFormatted || r.FechaFormatted || r.fecha || r.Fecha || '-';

        tr.innerHTML = `
            <td class="small font-monospace">${fechaVal}</td>
            <td><span class="badge bg-secondary">${celdaVal}</span></td>
            <td>Salida ${r.salida ?? r.Salida ?? 1}</td>
            <td>Turno ${r.turno ?? r.Turno ?? 1}</td>
            <td class="fw-bold text-primary">#${numSolVal}</td>
            <td><strong>${r.corriente ?? r.Corriente ?? 0}</strong> A</td>
            <td>${r.energia ?? r.Energia ?? 0} J</td>
            <td>${r.tiempo ?? r.Tiempo ?? 0} ms</td>
            <td>${r.penetracion ?? r.Penetracion ?? 0} mm</td>
            <td><span class="badge ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? '🟢 OK' : '🔴 NOK'}</span></td>
            <td class="small text-muted">${r.detallesFallas || r.DetallesFallas || ''}</td>
        `;
        tbody.appendChild(tr);
    });
}

function renderizarPaginacion(actual, totalPaginas, totalRegistros) {
    const lblInfo = document.getElementById('lbl-paginacion-info');
    const ul = document.getElementById('ul-paginacion');

    if (!lblInfo || !ul) return;

    const inicio = totalRegistros > 0 ? (actual - 1) * registrosPorPagina + 1 : 0;
    const fin = Math.min(actual * registrosPorPagina, totalRegistros);
    lblInfo.innerText = `Mostrando ${inicio}-${fin} de ${totalRegistros} registros`;

    ul.innerHTML = '';
    if (totalPaginas <= 1) return;

    const maxVisibles = 2;
    let inicioPag = Math.max(1, actual - maxVisibles);
    let finPag = Math.min(totalPaginas, actual + maxVisibles);

    if (actual > 1) {
        ul.appendChild(crearItemPaginacion('«', 1));
        ul.appendChild(crearItemPaginacion('‹', actual - 1));
    }

    if (inicioPag > 1) {
        ul.appendChild(crearItemInactivo('...'));
    }

    for (let i = inicioPag; i <= finPag; i++) {
        const li = document.createElement('li');
        li.className = `page-item ${i === actual ? 'active' : ''}`;
        li.innerHTML = `<a class="page-link" href="#" onclick="cargarDatos(${i}); return false;">${i}</a>`;
        ul.appendChild(li);
    }

    if (finPag < totalPaginas) {
        ul.appendChild(crearItemInactivo('...'));
    }

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
function exportarExcel() {
    const idMaquina = document.getElementById('filtro-celda')?.value || '';
    const estatus = document.getElementById('filtro-estatus')?.value || '';
    const fechaInicio = document.getElementById('filtro-fecha-inicio')?.value || '';
    const fechaFin = document.getElementById('filtro-fecha-fin')?.value || '';
    const busqueda = document.getElementById('buscador-global')?.value || '';

    const url = `/Home/ExportarExcel?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${encodeURIComponent(busqueda)}`;
    window.location.href = url;
}

async function exportarPDF() {
    const idMaquina = document.getElementById('filtro-celda')?.value || '';
    const estatus = document.getElementById('filtro-estatus')?.value || '';
    const fechaInicio = document.getElementById('filtro-fecha-inicio')?.value || '';
    const fechaFin = document.getElementById('filtro-fecha-fin')?.value || '';
    const busqueda = document.getElementById('buscador-global')?.value || '';

    const url = `/Home/ObtenerTodosParaImpresion?identificadorId=${idMaquina}&estatus=${estatus}&fechaInicio=${fechaInicio}&fechaFin=${fechaFin}&busqueda=${encodeURIComponent(busqueda)}`;

    try {
        const response = await fetch(url);
        const result = await response.json();

        if (!result.success || !result.data || result.data.length === 0) {
            alert("No hay registros disponibles para exportar.");
            return;
        }

        const registros = result.data;
        let filasHtml = '';
        registros.forEach(r => {
            const isOk = (r.estatusCalidad || r.EstatusCalidad) === 'OK' || (r.estatusCalidad || r.EstatusCalidad) === 'Solo OK';
            filasHtml += `
                <tr>
                    <td style="font-size: 11px;">${r.fechaFormatted || r.FechaFormatted || '-'}</td>
                    <td>${r.celda || r.Celda || 'N/A'}</td>
                    <td>Salida ${r.salida ?? r.Salida ?? 1}</td>
                    <td>Turno ${r.turno ?? r.Turno ?? 1}</td>
                    <td><b>#${r.numSol ?? r.NumSol ?? r.idRegistro ?? 0}</b></td>
                    <td><b>${r.corriente ?? r.Corriente ?? 0}</b> A</td>
                    <td>${r.energia ?? r.Energia ?? 0} J</td>
                    <td>${r.tiempo ?? r.Tiempo ?? 0} ms</td>
                    <td>${r.penetracion ?? r.Penetracion ?? 0} mm</td>
                    <td><span class="badge ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? 'OK' : 'NOK'}</span></td>
                    <td style="font-size: 11px;">${r.detallesFallas || r.DetallesFallas || ''}</td>
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

// -------------------------------------------------------------
// CONTROL ESTADÍSTICO DE PROCESO (SPC) Y GRÁFICA
// -------------------------------------------------------------
function calcularIndicadoresSPC(datos) {
    if (!datos || datos.length === 0) {
        return { cp: 0, cpk: 0, media: 0, ucl: 1330, lcl: 1270, usl: 1330, lsl: 1270 };
    }

    const corrientes = datos.map(r => parseFloat(r.corriente ?? r.Corriente) || 0).filter(c => c > 0);
    if (corrientes.length === 0) return { cp: 0, cpk: 0, media: 0, ucl: 1330, lcl: 1270, usl: 1330, lsl: 1270 };

    // 1. Media (X̄)
    const media = corrientes.reduce((acc, v) => acc + v, 0) / corrientes.length;

    // Tolerancia adaptativa (+/- 30A respecto al promedio nominal si no es 1300A)
    const usl = media > 1200 ? 1330 : Math.round(media + 30);
    const lsl = media > 1200 ? 1270 : Math.round(media - 30);

    // 2. Desviación Estándar (σ)
    const varianza = corrientes.reduce((acc, v) => acc + Math.pow(v - media, 2), 0) / (corrientes.length > 1 ? corrientes.length - 1 : 1);
    const sigma = Math.sqrt(varianza) || 0.0001;

    // 3. Cp y Cpk
    const cp = (usl - lsl) / (6 * sigma);
    const cpu = (usl - media) / (3 * sigma);
    const cpl = (media - lsl) / (3 * sigma);
    const cpk = Math.min(cpu, cpl);

    // 4. Límites de Control Estadístico (3-Sigma)
    const ucl = media + (3 * sigma);
    const lcl = media - (3 * sigma);

    return {
        cp: parseFloat(cp.toFixed(2)),
        cpk: parseFloat(cpk.toFixed(2)),
        media: parseFloat(media.toFixed(1)),
        ucl: parseFloat(ucl.toFixed(1)),
        lcl: parseFloat(lcl.toFixed(1)),
        usl: usl,
        lsl: lsl
    };
}

function actualizarGraficaSPC(registros) {
    if (!chartSPC) return;

    const ultimos = [...registros].reverse();
    const spcStats = calcularIndicadoresSPC(ultimos);

    const lblCp = document.getElementById('lbl-spc-cp');
    const lblCpk = document.getElementById('lbl-spc-cpk');
    if (lblCp) lblCp.innerText = spcStats.cp;
    if (lblCpk) {
        lblCpk.innerText = spcStats.cpk;
        lblCpk.className = `h3 mb-0 fw-bold ${spcStats.cpk >= 1.33 ? 'text-success' : (spcStats.cpk >= 1.0 ? 'text-warning' : 'text-danger')}`;
    }

    chartSPC.data.labels = ultimos.map(r => `#${r.numSol || r.NumSol || r.idRegistro || r.IdRegistro}`);
    chartSPC.data.datasets[0].data = ultimos.map(r => r.corriente ?? r.Corriente ?? 0);
    chartSPC.data.datasets[1].data = ultimos.map(() => spcStats.usl);
    chartSPC.data.datasets[2].data = ultimos.map(() => spcStats.lsl);
    chartSPC.data.datasets[3].data = ultimos.map(() => spcStats.media);
    chartSPC.data.datasets[4].data = ultimos.map(() => spcStats.ucl);
    chartSPC.data.datasets[5].data = ultimos.map(() => spcStats.lcl);

    chartSPC.update();
}

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
                    label: 'USL Tolerancia',
                    data: [],
                    borderColor: '#dc3545',
                    borderDash: [4, 4],
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'LSL Tolerancia',
                    data: [],
                    borderColor: '#dc3545',
                    borderDash: [4, 4],
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'Media Process (X̄)',
                    data: [],
                    borderColor: '#198754',
                    borderDash: [2, 2],
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'UCL (Control +3σ)',
                    data: [],
                    borderColor: '#ffc107',
                    borderDash: [6, 2],
                    fill: false,
                    pointRadius: 0
                },
                {
                    label: 'LCL (Control -3σ)',
                    data: [],
                    borderColor: '#ffc107',
                    borderDash: [6, 2],
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

function abrirModalDetalle(r) {
    document.getElementById('modal-num-sol').innerText = `#${r.numSol || r.NumSol || r.idRegistro || r.IdRegistro}`;
    document.getElementById('modal-serial').innerText = r.serialTrazabilidad || r.SerialTrazabilidad || 'N/A';
    document.getElementById('modal-fecha').innerText = r.fechaFormatted || r.FechaFormatted || r.fecha || r.Fecha || '-';

    const estatusVal = r.estatusCalidad || r.EstatusCalidad || r.estatus || r.Estatus || '';
    const isOk = estatusVal === 'OK' || estatusVal === 'Solo OK';
    document.getElementById('modal-estatus-badge').innerHTML = `<span class="badge fs-6 ${isOk ? 'bg-success' : 'bg-danger'}">${isOk ? '🟢 OK' : '🔴 NOK'}</span>`;

    document.getElementById('modal-volarc').innerText = r.volArc ?? r.VolArc ?? 0;
    document.getElementById('modal-volpri').innerText = r.volPri ?? r.VolPri ?? 0;
    document.getElementById('modal-corriente').innerText = r.corriente ?? r.Corriente ?? 0;
    document.getElementById('modal-energia').innerText = r.energia ?? r.Energia ?? 0;
    document.getElementById('modal-tiempo').innerText = r.tiempo ?? r.Tiempo ?? 0;
    document.getElementById('modal-penetracion').innerText = r.penetracion ?? r.Penetracion ?? 0;
    document.getElementById('modal-elevacion').innerText = r.elevacion ?? r.Elevacion ?? 0;
    document.getElementById('modal-lonper').innerText = r.lonPer ?? r.LonPer ?? 0;

    document.getElementById('modal-err').innerText = `Err: ${r.err ?? r.Err ?? 0}`;
    document.getElementById('modal-alarma').innerText = `Alarma: ${r.alarma ?? r.Alarma ?? 0}`;
    document.getElementById('modal-modo').innerText = `Modo: Auto (${r.modo ?? r.Modo ?? 1})`;

    const alertDiv = document.getElementById('modal-alert-desviaciones');
    if (!isOk) {
        alertDiv.classList.remove('d-none');
        document.getElementById('modal-detalles-fallas').innerText = r.detallesFallas || r.DetallesFallas || 'Sin detalle especificado';
    } else {
        alertDiv.classList.add('d-none');
    }

    const modal = new bootstrap.Modal(document.getElementById('modalDetalleSoldadura'));
    modal.show();
}