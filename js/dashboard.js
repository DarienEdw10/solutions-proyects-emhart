// Arreglo extendido con 20 registros de prueba para validar la paginación (5 por página = 4 páginas)
const datosSimulados = [
    { fecha: "2026-08-04 09:29:33", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7440, corriente: 1290, energia: 671, tiempo: 29.2, penetracion: -1.06, volArc: 21.6, volPri: 24.6, elevacion: 2.2, caida: 0, lonPer: 12.14, err: 0, alarma: 0, modo: 1, estatus: "NOK", detalles: "Tiempo: 29.2 [Min:37.0, Max:44.0]", turno: "T1" },
    { fecha: "2026-08-04 09:29:29", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7439, corriente: 1290, energia: 1350, tiempo: 41.5, penetracion: -1.06, volArc: 21.6, volPri: 24.6, elevacion: 2.2, caida: 0, lonPer: 12.14, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 09:15:10", celda: "CELDA-01", salida: "Out 1", programa: 1, numSol: 7438, corriente: 1280, energia: 1320, tiempo: 40.8, penetracion: -1.05, volArc: 21.5, volPri: 24.5, elevacion: 2.2, caida: 0, lonPer: 12.10, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 09:02:44", celda: "CELDA-01", salida: "Out 3", programa: 2, numSol: 7437, corriente: 1310, energia: 1400, tiempo: 42.1, penetracion: -1.18, volArc: 22.0, volPri: 25.0, elevacion: 2.2, caida: 0, lonPer: 12.00, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 08:50:12", celda: "CELDA-03", salida: "Out 1", programa: 2, numSol: 12504, corriente: 925, energia: 575, tiempo: 28.5, penetracion: -0.89, volArc: 20.1, volPri: 25.1, elevacion: 1.09, caida: 0, lonPer: 10.50, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 08:28:15", celda: "CELDA-03", salida: "Out 1", programa: 2, numSol: 12503, corriente: 930, energia: 580, tiempo: 28.7, penetracion: -0.90, volArc: 20.2, volPri: 25.2, elevacion: 1.09, caida: 0, lonPer: 10.50, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 08:10:00", celda: "CELDA-03", salida: "Out 2", programa: 1, numSol: 12502, corriente: 910, energia: 560, tiempo: 25.1, penetracion: -1.37, volArc: 19.4, volPri: 25.0, elevacion: 1.09, caida: 0, lonPer: 10.45, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 07:55:30", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7436, corriente: 1295, energia: 1360, tiempo: 41.2, penetracion: -1.08, volArc: 21.7, volPri: 24.7, elevacion: 2.2, caida: 0, lonPer: 12.12, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 07:40:18", celda: "CELDA-01", salida: "Out 1", programa: 1, numSol: 7435, corriente: 1150, energia: 1100, tiempo: 35.0, penetracion: -0.95, volArc: 21.0, volPri: 24.0, elevacion: 2.2, caida: 0, lonPer: 12.00, err: 1, alarma: 0, modo: 1, estatus: "NOK", detalles: "Corriente: 1150 [Min:1270, Max:1330]", turno: "T1" },
    { fecha: "2026-08-04 07:27:00", celda: "CELDA-01", salida: "Out 3", programa: 1, numSol: 23000, corriente: 1300, energia: 1482, tiempo: 41.8, penetracion: -1.18, volArc: 21.8, volPri: 24.8, elevacion: 2.2, caida: 0, lonPer: 12.00, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 06:45:10", celda: "CELDA-01", salida: "Out 3", programa: 1, numSol: 22999, corriente: 1298, energia: 1475, tiempo: 41.6, penetracion: -1.17, volArc: 21.8, volPri: 24.8, elevacion: 2.2, caida: 0, lonPer: 12.02, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-04 06:15:22", celda: "CELDA-03", salida: "Out 1", programa: 2, numSol: 12501, corriente: 928, energia: 578, tiempo: 28.6, penetracion: -0.91, volArc: 20.2, volPri: 25.1, elevacion: 1.09, caida: 0, lonPer: 10.48, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T1" },
    { fecha: "2026-08-03 22:40:05", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7434, corriente: 1288, energia: 1345, tiempo: 41.0, penetracion: -1.07, volArc: 21.6, volPri: 24.6, elevacion: 2.2, caida: 0, lonPer: 12.10, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 21:10:14", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7433, corriente: 1292, energia: 1355, tiempo: 41.4, penetracion: -1.06, volArc: 21.6, volPri: 24.6, elevacion: 2.2, caida: 0, lonPer: 12.11, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 20:05:00", celda: "CELDA-03", salida: "Out 2", programa: 1, numSol: 12500, corriente: 912, energia: 565, tiempo: 25.3, penetracion: -1.36, volArc: 19.5, volPri: 25.0, elevacion: 1.09, caida: 0, lonPer: 10.46, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 19:30:12", celda: "CELDA-01", salida: "Out 1", programa: 1, numSol: 7432, corriente: 1278, energia: 1315, tiempo: 40.5, penetracion: -1.04, volArc: 21.4, volPri: 24.4, elevacion: 2.2, caida: 0, lonPer: 12.08, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 18:15:00", celda: "CELDA-01", salida: "Out 1", programa: 1, numSol: 22950, corriente: 1285, energia: 1420, tiempo: 40.2, penetracion: -1.15, volArc: 21.5, volPri: 24.5, elevacion: 2.2, caida: 0, lonPer: 12.10, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 17:00:44", celda: "CELDA-01", salida: "Out 3", programa: 1, numSol: 22949, corriente: 1302, energia: 1485, tiempo: 41.9, penetracion: -1.19, volArc: 21.9, volPri: 24.9, elevacion: 2.2, caida: 0, lonPer: 12.01, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 16:20:10", celda: "CELDA-03", salida: "Out 1", programa: 2, numSol: 12499, corriente: 932, energia: 582, tiempo: 28.8, penetracion: -0.92, volArc: 20.3, volPri: 25.3, elevacion: 1.09, caida: 0, lonPer: 10.51, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" },
    { fecha: "2026-08-03 15:45:00", celda: "CELDA-01", salida: "Out 2", programa: 1, numSol: 7431, corriente: 1290, energia: 1348, tiempo: 41.1, penetracion: -1.06, volArc: 21.6, volPri: 24.6, elevacion: 2.2, caida: 0, lonPer: 12.10, err: 0, alarma: 0, modo: 1, estatus: "OK", detalles: null, turno: "T2" }
];

let datosActualesFiltrados = [...datosSimulados];
let miGraficaChart = null;
let modalDetalleBS = null;

// Configuración de Paginación
let paginaActual = 1;
const registrosPorPagina = 5;

// =====================================================================
// RENDERIZADO DE TABLA + PAGINACIÓN + BUSCADOR
// =====================================================================
function renderizarTabla(datos) {
    datosActualesFiltrados = datos;
    const tbody = document.getElementById("tabla-body");
    tbody.innerHTML = "";

    // Paginación Lógica
    const totalRegistros = datos.length;
    const totalPaginas = Math.ceil(totalRegistros / registrosPorPagina) || 1;

    if (paginaActual > totalPaginas) paginaActual = totalPaginas;

    const inicio = (paginaActual - 1) * registrosPorPagina;
    const fin = inicio + registrosPorPagina;
    const datosPagina = datos.slice(inicio, fin);

    if (datosPagina.length === 0) {
        tbody.innerHTML = `<tr><td colspan="11" class="text-center py-4 text-muted">No se encontraron registros que coincidan con los filtros.</td></tr>`;
    } else {
        datosPagina.forEach((row) => {
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

    // Actualizar indicador de paginación
    document.getElementById("lbl-paginacion-info").innerText = `Mostrando ${totalRegistros === 0 ? 0 : inicio + 1}-${Math.min(fin, totalRegistros)} de ${totalRegistros} registros`;
    renderizarPaginadorUI(totalPaginas);

    // Actualizar gráfica de control con los datos visibles/filtrados
    inicializarGrafica(datos);
}

function renderizarPaginadorUI(totalPaginas) {
    const ul = document.getElementById("ul-paginacion");
    ul.innerHTML = "";

    if (totalPaginas <= 1) return;

    for (let i = 1; i <= totalPaginas; i++) {
        const li = document.createElement("li");
        li.className = `page-item ${i === paginaActual ? "active" : ""}`;
        li.innerHTML = `<a class="page-link" href="#" onclick="cambiarPagina(${i}); return false;">${i}</a>`;
        ul.appendChild(li);
    }
}

function cambiarPagina(num) {
    paginaActual = num;
    renderizarTabla(datosActualesFiltrados);
}

// =====================================================================
// FILTRADO GLOBAL EN TIEMPO REAL
// =====================================================================
function aplicarFiltros() {
    const celda = document.getElementById("filtro-celda").value;
    const estatus = document.getElementById("filtro-estatus").value;
    const turno = document.getElementById("filtro-turno").value;
    const busqueda = document.getElementById("buscador-global").value.toLowerCase().trim();

    let filtrados = datosSimulados.filter(item => {
        const matchCelda = !celda || item.celda === celda;
        const matchEstatus = !estatus || item.estatus === estatus;
        const matchTurno = !turno || item.turno === turno;
        
        const matchBusqueda = !busqueda || 
            item.numSol.toString().includes(busqueda) ||
            item.salida.toLowerCase().includes(busqueda) ||
            item.celda.toLowerCase().includes(busqueda) ||
            item.fecha.includes(busqueda);

        return matchCelda && matchEstatus && matchTurno && matchBusqueda;
    });

    paginaActual = 1;
    renderizarTabla(filtrados);
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

    document.getElementById("modal-volarc").innerText = data.volArc;
    document.getElementById("modal-volpri").innerText = data.volPri;
    document.getElementById("modal-corriente").innerText = data.corriente;
    document.getElementById("modal-energia").innerText = data.energia;
    document.getElementById("modal-tiempo").innerText = data.tiempo;
    document.getElementById("modal-penetracion").innerText = data.penetracion;
    document.getElementById("modal-elevacion").innerText = data.elevacion;
    document.getElementById("modal-lonper").innerText = data.lonPer;

    document.getElementById("modal-err").innerText = `Cód. Error: ${data.err}`;
    document.getElementById("modal-alarma").innerText = `Alarma: ${data.alarma}`;

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
// EXPORTACIONES
// =====================================================================
function exportarExcel() {
    if (datosActualesFiltrados.length === 0) return alert("Sin datos.");
    const dataExcel = datosActualesFiltrados.map(i => ({
        "Fecha": i.fecha, "Celda": i.celda, "Salida": i.salida, "Turno": i.turno,
        "N° Soldadura": i.numSol, "Corriente (A)": i.corriente, "Energía (J)": i.energia,
        "Tiempo (ms)": i.tiempo, "Penetración (mm)": i.penetracion, "Estatus": i.estatus, "Desviaciones": i.detalles || "OK"
    }));
    const ws = XLSX.utils.json_to_sheet(dataExcel);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "Tucker");
    XLSX.writeFile(wb, `Reporte_Tucker_${new Date().toISOString().slice(0, 10)}.xlsx`);
}

function exportarPDF() {
    if (datosActualesFiltrados.length === 0) return alert("Sin datos.");
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ orientation: "landscape" });
    doc.text("AUTOTEK MÉXICO - REPORTE CELDAS TUCKER", 14, 15);
    const filasPDF = datosActualesFiltrados.map(i => [i.fecha, i.celda, i.salida, i.turno, `#${i.numSol}`, `${i.corriente} A`, `${i.energia} J`, `${i.tiempo} ms`, `${i.penetracion} mm`, i.estatus, i.detalles || "-"]);
    doc.autoTable({
        startY: 22,
        head: [["Fecha", "Celda", "Salida", "Turno", "N° Sol.", "Corriente", "Energía", "Tiempo", "Penetración", "Estatus", "Desviaciones"]],
        body: filasPDF,
        theme: "striped", headStyles: { fillColor: [33, 37, 41] }, styles: { fontSize: 8 }
    });
    doc.save(`Reporte_Tucker_${new Date().toISOString().slice(0, 10)}.pdf`);
}

document.addEventListener("DOMContentLoaded", () => {
    modalDetalleBS = new bootstrap.Modal(document.getElementById('modalDetalleSoldadura'));
    renderizarTabla(datosSimulados);
});