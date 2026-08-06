// URL base de la API de Recetas / Tolerancias en .NET 8
const API_RECETAS_URL = "http://localhost:5240/api/recetas";

let recetasBD = {};
let modalConfirm = null;
let toastOk = null;

// =====================================================================
// CONSUMO / OBTENCIÓN DE RECETAS DE LA API
// =====================================================================
async function obtenerRecetasAPI() {
    try {
        const respuesta = await fetch(API_RECETAS_URL);
        if (!respuesta.ok) throw new Error(`Error HTTP: ${respuesta.status}`);

        recetasBD = await respuesta.json();
        cargarRecetaSeleccionada();
    } catch (error) {
        console.warn("API de recetas no disponible. Utilizando caché local o modo sin conexión.", error);
        cargarRecetaSeleccionada();
    }
}

// =====================================================================
// RENDERIZADO DE TABLA DE TOLERANCIAS
// =====================================================================
function cargarRecetaSeleccionada() {
    const celda = document.getElementById("select-receta-celda").value;
    const salida = document.getElementById("select-receta-salida").value;
    const tbody = document.getElementById("tabla-recetas-body");
    const lblFechaGeneral = document.getElementById("lbl-ultima-modificacion");

    tbody.innerHTML = "";

    const params = (recetasBD[celda] && recetasBD[celda][salida]) ? recetasBD[celda][salida] : [];

    if (params.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-muted">No hay parámetros configurados para la combinación seleccionada.</td></tr>`;
        lblFechaGeneral.innerText = "N/A";
        return;
    }

    // Buscar la fecha de modificación más reciente para mostrar en el encabezado
    const ultimasFechas = params.map(p => p.fechaMod).sort().reverse();
    lblFechaGeneral.innerText = ultimasFechas[0] || "N/A";

    params.forEach((row, index) => {
        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><strong>${row.parametro}</strong></td>
            <td><span class="badge bg-secondary">${row.unidad}</span></td>
            <td>
                <input type="number" step="0.1" class="form-control form-control-sm border-secondary fw-bold" 
                       id="min-${index}" value="${row.min}">
            </td>
            <td>
                <input type="number" step="0.1" class="form-control form-control-sm border-secondary fw-bold" 
                       id="max-${index}" value="${row.max}">
            </td>
            <td><small class="text-muted font-monospace"><i class="bi bi-clock-history me-1"></i>${row.fechaMod}</small></td>
            <td><small class="badge bg-light text-dark border">${row.usuario}</small></td>
            <td><span class="badge bg-success">ACTIVO</span></td>
        `;
        tbody.appendChild(tr);
    });
}

function obtenerFechaActualFormateada() {
    const ahora = new Date();
    const pad = (n) => n.toString().padStart(2, '0');
    return `${ahora.getFullYear()}-${pad(ahora.getMonth() + 1)}-${pad(ahora.getDate())} ${pad(ahora.getHours())}:${pad(ahora.getMinutes())}:${pad(ahora.getSeconds())}`;
}

function confirmarGuardado() {
    document.getElementById("fecha-actual-modal").innerText = obtenerFechaActualFormateada();
    modalConfirm.show();
}

// =====================================================================
// GUARDADO Y ENVÍO DEL HISTORIAL
// =====================================================================
async function guardarReceta() {
    modalConfirm.hide();

    const celda = document.getElementById("select-receta-celda").value;
    const salida = document.getElementById("select-receta-salida").value;
    const fechaActual = obtenerFechaActualFormateada();

    const params = (recetasBD[celda] && recetasBD[celda][salida]) ? recetasBD[celda][salida] : [];
    const listaAuditoria = [];

    params.forEach((row, index) => {
        const minVal = parseFloat(document.getElementById(`min-${index}`).value);
        const maxVal = parseFloat(document.getElementById(`max-${index}`).value);

        // Detectar si hubo cambios respecto al valor anterior
        if (row.min !== minVal || row.max !== maxVal) {
            listaAuditoria.push({
                Parametro: row.parametro,
                MinAnterior: row.min,
                MaxAnterior: row.max,
                MinNuevo: minVal,
                MaxNuevo: maxVal,
                Usuario: "Usuario_Web"
            });

            row.min = minVal;
            row.max = maxVal;
            row.fechaMod = fechaActual;
            row.usuario = "Usuario_Web";
        }
    });

    // Envío asíncrono hacia el endpoint de auditoría si se detectaron modificaciones
    if (listaAuditoria.length > 0) {
        try {
            await fetch(`${API_RECETAS_URL}/guardar`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ celda, salida, cambios: listaAuditoria })
            });
            // Refrescar automáticamente la tabla de auditoría
            cargarHistorialAuditoria();
        } catch (err) {
            console.warn("Sin conexión con el servidor para guardar el historial en SQL Server.", err);
        }
    }

    cargarRecetaSeleccionada();

    document.getElementById("toast-mensaje").innerHTML = `<i class="bi bi-check-circle-fill me-2"></i>¡Receta actualizada con fecha ${fechaActual}!`;
    toastOk.show();
}

// =====================================================================
// CONSULTA DE HISTORIAL Y COMPARATIVA DE AUDITORÍA
// =====================================================================
async function cargarHistorialAuditoria() {
    const celda = document.getElementById("select-receta-celda").value;
    const fInicio = document.getElementById("filtro-fecha-inicio").value;
    const fFin = document.getElementById("filtro-fecha-fin").value;

    const url = `${API_RECETAS_URL}/historial?celda=${celda}&fechaInicio=${fInicio}&fechaFin=${fFin}`;

    try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`Error HTTP: ${res.status}`);

        const datos = await res.json();
        const tbody = document.getElementById("tabla-historial-body");
        tbody.innerHTML = "";

        if (datos.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-3">No hay cambios registrados dentro del rango de fechas seleccionado.</td></tr>`;
            return;
        }

        datos.forEach(h => {
            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><small class="font-monospace">${h.fecha}</small></td>
                <td><span class="badge bg-secondary">${h.celda} - ${h.salida}</span></td>
                <td><strong>${h.parametro}</strong></td>
                <td><span class="badge bg-danger text-wrap">${h.valorAnterior}</span></td>
                <td><span class="badge bg-success text-wrap">${h.valorNuevo}</span></td>
                <td><small class="badge bg-light text-dark border">${h.usuario}</small></td>
            `;
            tbody.appendChild(tr);
        });
    } catch (e) {
        console.warn("No se pudo obtener el historial de auditoría desde la API.", e);
    }
}

// Inicialización
document.addEventListener("DOMContentLoaded", () => {
    modalConfirm = new bootstrap.Modal(document.getElementById('modalConfirmar'));
    toastOk = new bootstrap.Toast(document.getElementById('toastSuccess'));

    obtenerRecetasAPI();
});