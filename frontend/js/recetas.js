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
// GUARDADO Y ENVÍO A LA API
// =====================================================================
async function guardarReceta() {
    modalConfirm.hide();

    const celda = document.getElementById("select-receta-celda").value;
    const salida = document.getElementById("select-receta-salida").value;
    const fechaActual = obtenerFechaActualFormateada();

    const params = (recetasBD[celda] && recetasBD[celda][salida]) ? recetasBD[celda][salida] : [];
    const datosModificados = [];

    params.forEach((row, index) => {
        const minVal = parseFloat(document.getElementById(`min-${index}`).value);
        const maxVal = parseFloat(document.getElementById(`max-${index}`).value);

        if (row.min !== minVal || row.max !== maxVal) {
            row.min = minVal;
            row.max = maxVal;
            row.fechaMod = fechaActual;
            row.usuario = "Usuario_Web";
            datosModificados.push(row);
        }
    });

    // Envío asíncrono si hay cambios
    if (datosModificados.length > 0) {
        try {
            await fetch(API_RECETAS_URL, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ celda, salida, parametros: datosModificados })
            });
        } catch (err) {
            console.warn("Sin conexión con el servidor para actualizar base de datos real. Cambios guardados localmente.", err);
        }
    }

    cargarRecetaSeleccionada();

    document.getElementById("toast-mensaje").innerHTML = `<i class="bi bi-check-circle-fill me-2"></i>¡Receta actualizada con fecha ${fechaActual}!`;
    toastOk.show();
}

// Inicialización
document.addEventListener("DOMContentLoaded", () => {
    modalConfirm = new bootstrap.Modal(document.getElementById('modalConfirmar'));
    toastOk = new bootstrap.Toast(document.getElementById('toastSuccess'));

    obtenerRecetasAPI();
});