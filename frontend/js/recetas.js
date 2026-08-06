// URL base de la API de Recetas / Tolerancias en .NET 8
const API_RECETAS_URL = "http://localhost:5240/api/recetas";

let parametrosActuales = [];
let modalConfirm = null;
let toastOk = null;

// Convertir selector a ID de máquina (CELDA-03 -> 3, CELDA-01 -> 1)
function obtenerIdMaquina(valorSelect) {
    return valorSelect === "CELDA-03" ? 3 : 1;
}

// Extraer el número entero de la salida ("Out 3" -> 3)
function obtenerNumeroSalida(valorSelect) {
    if (!valorSelect) return 3;
    const match = valorSelect.match(/\d+/);
    return match ? parseInt(match[0], 10) : 3;
}

// =====================================================================
// CONSUMO DE RECETAS ACTIVAS DESDE LA API (estado = true)
// =====================================================================
async function obtenerRecetasAPI() {
    const celdaStr = document.getElementById("select-receta-celda")?.value || "CELDA-01";
    const salidaStr = document.getElementById("select-receta-salida")?.value || "Out 3";

    const idMaquina = obtenerIdMaquina(celdaStr);
    const numSalida = obtenerNumeroSalida(salidaStr);

    try {
        const respuesta = await fetch(`${API_RECETAS_URL}?maquina=${idMaquina}&salida=${numSalida}`);
        if (!respuesta.ok) throw new Error(`Error HTTP: ${respuesta.status}`);

        parametrosActuales = await respuesta.json();
        cargarRecetaSeleccionada();
    } catch (error) {
        console.warn("No se pudo obtener la receta desde la API .NET 8.", error);
        parametrosActuales = [];
        cargarRecetaSeleccionada();
    }
}

// =====================================================================
// RENDERIZADO DE TABLA DE TOLERANCIAS ACTIVAS
// =====================================================================
function cargarRecetaSeleccionada() {
    const tbody = document.getElementById("tabla-recetas-body");
    const lblFechaGeneral = document.getElementById("lbl-ultima-modificacion");

    if (!tbody) return;
    tbody.innerHTML = "";

    if (!parametrosActuales || parametrosActuales.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-muted">No hay parámetros configurados para la combinación seleccionada.</td></tr>`;
        if (lblFechaGeneral) lblFechaGeneral.innerText = "N/A";
        return;
    }

    // Buscar la fecha más reciente
    const ultimasFechas = parametrosActuales
        .map(p => p.fechaModificacion || p.fechaCreacion || p.FechaModificacion || p.FechaCreacion)
        .filter(f => f)
        .sort()
        .reverse();

    if (lblFechaGeneral) {
        lblFechaGeneral.innerText = ultimasFechas[0] ? new Date(ultimasFechas[0]).toLocaleString() : "N/A";
    }

    parametrosActuales.forEach((row, index) => {
        const tr = document.createElement("tr");
        
        // Mapeo flexible para camelCase o PascalCase
        const paramNombre = row.parametro || row.Parametro || "";
        const minVal = row.minVal ?? row.MinVal ?? 0;
        const maxVal = row.maxVal ?? row.MaxVal ?? 0;
        const fechaRaw = row.fechaModificacion || row.fechaCreacion || row.FechaModificacion || row.FechaCreacion || "";
        const modPor = row.modificadoPor || row.ModificadoPor || "Usuario_Web";

        const fechaMostrar = fechaRaw ? fechaRaw.toString().replace("T", " ").slice(0, 19) : "Sin registro";

        tr.innerHTML = `
            <td><strong>${paramNombre}</strong></td>
            <td><span class="badge bg-secondary">Físico</span></td>
            <td>
                <input type="number" step="0.1" class="form-control form-control-sm border-secondary fw-bold" 
                       id="min-${index}" value="${minVal}">
            </td>
            <td>
                <input type="number" step="0.1" class="form-control form-control-sm border-secondary fw-bold" 
                       id="max-${index}" value="${maxVal}">
            </td>
            <td><small class="text-muted font-monospace"><i class="bi bi-clock-history me-1"></i>${fechaMostrar}</small></td>
            <td><small class="badge bg-light text-dark border">${modPor}</small></td>
            <td><span class="badge bg-success">🟢 ACTIVO</span></td>
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
    const elFecha = document.getElementById("fecha-actual-modal");
    if (elFecha) elFecha.innerText = obtenerFechaActualFormateada();
    if (modalConfirm) modalConfirm.show();
}

// =====================================================================
// GUARDADO Y VERSIONADO DINÁMICO (POST /api/recetas/guardar)
// =====================================================================
async function guardarReceta() {
    if (modalConfirm) modalConfirm.hide();

    const celdaStr = document.getElementById("select-receta-celda").value;
    const salidaStr = document.getElementById("select-receta-salida").value;

    const idMaquina = obtenerIdMaquina(celdaStr);
    const numSalida = obtenerNumeroSalida(salidaStr);
    const fechaActual = obtenerFechaActualFormateada();

    const listaNuevosParametros = [];

    parametrosActuales.forEach((row, index) => {
        const minValInput = parseFloat(document.getElementById(`min-${index}`).value);
        const maxValInput = parseFloat(document.getElementById(`max-${index}`).value);

        listaNuevosParametros.push({
            idMaquina: idMaquina,
            salida: numSalida,
            parametro: row.parametro || row.Parametro,
            minVal: minValInput,
            maxVal: maxValInput,
            modificadoPor: "Usuario_Web"
        });
    });

    const payload = {
        idMaquina: idMaquina,
        salida: numSalida,
        usuario: "Usuario_Web",
        parametros: listaNuevosParametros
    };

    try {
        const res = await fetch(`${API_RECETAS_URL}/guardar`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!res.ok) throw new Error(`Error en servidor: ${res.status}`);

        await obtenerRecetasAPI();
        cargarHistorialAuditoria();

        const elToast = document.getElementById("toast-mensaje");
        if (elToast) elToast.innerHTML = `<i class="bi bi-check-circle-fill me-2"></i>¡Receta actualizada con fecha ${fechaActual}!`;
        if (toastOk) toastOk.show();
    } catch (err) {
        console.error("Error al guardar cambios de receta:", err);
        alert("Ocurrió un error al intentar guardar los cambios en el servidor.");
    }
}

// =====================================================================
// CONSULTA DE HISTORIAL Y AUDITORÍA POR RANGO DE FECHAS
// =====================================================================
async function cargarHistorialAuditoria() {
    const celdaStr = document.getElementById("select-receta-celda")?.value || "CELDA-01";
    const idMaquina = obtenerIdMaquina(celdaStr);

    const fInicio = document.getElementById("filtro-fecha-inicio")?.value || "";
    const fFin = document.getElementById("filtro-fecha-fin")?.value || "";

    let url = `${API_RECETAS_URL}/historial?maquina=${idMaquina}`;
    if (fInicio) url += `&fechaInicio=${fInicio}`;
    if (fFin) url += `&fechaFin=${fFin}`;

    try {
        const res = await fetch(url);
        if (!res.ok) throw new Error(`Error HTTP: ${res.status}`);

        const datos = await res.json();
        const tbody = document.getElementById("tabla-historial-body");
        if (!tbody) return;

        tbody.innerHTML = "";

        if (datos.length === 0) {
            tbody.innerHTML = `<tr><td colspan="6" class="text-center text-muted py-3">No hay registros de auditoría dentro del rango seleccionado.</td></tr>`;
            return;
        }

        datos.forEach(h => {
            const tr = document.createElement("tr");

            const estatusTxt = h.estatusRegistro || h.EstatusRegistro || "";
            const badgeEstatus = estatusTxt.includes("ACTIVO")
                ? `<span class="badge bg-success">🟢 ${estatusTxt}</span>`
                : `<span class="badge bg-secondary">🔴 ${estatusTxt}</span>`;

            tr.innerHTML = `
                <td><small class="font-monospace">${h.fechaRegistro || h.FechaRegistro}</small></td>
                <td><span class="badge bg-dark">${h.celda || h.Celda} - Out ${h.salida || h.Salida}</span></td>
                <td><strong>${h.parametro || h.Parametro}</strong></td>
                <td>Mín: <strong>${h.minVal ?? h.MinVal}</strong> | Máx: <strong>${h.maxVal ?? h.MaxVal}</strong></td>
                <td>${badgeEstatus}</td>
                <td><small class="badge bg-light text-dark border">${h.modificadoPor || h.ModificadoPor}</small></td>
            `;
            tbody.appendChild(tr);
        });
    } catch (e) {
        console.warn("No se pudo obtener el historial de auditoría desde la API.", e);
    }
}

// Inicialización
document.addEventListener("DOMContentLoaded", () => {
    const elModal = document.getElementById('modalConfirmar');
    const elToast = document.getElementById('toastSuccess');

    if (elModal) modalConfirm = new bootstrap.Modal(elModal);
    if (elToast) toastOk = new bootstrap.Toast(elToast);

    // Escuchar cambios en los selectores de celda y salida
    document.getElementById("select-receta-celda")?.addEventListener("change", () => {
        obtenerRecetasAPI();
        cargarHistorialAuditoria();
    });

    document.getElementById("select-receta-salida")?.addEventListener("change", () => {
        obtenerRecetasAPI();
    });

    // Carga inicial
    obtenerRecetasAPI();
    cargarHistorialAuditoria();
});