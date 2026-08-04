// Simulación de Recetas/Tolerancias con trazabilidad de fechas (Mismo esquema que emhart.referencia_tolerancia)
const recetasBD = {
    "CELDA-01": {
        3: [
            { parametro: "VolArc", unidad: "V", min: 16.0, max: 33.0, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" },
            { parametro: "VolPri", unidad: "V", min: 21.0, max: 33.0, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" },
            { parametro: "Corriente", unidad: "A", min: 1270.0, max: 1330.0, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" },
            { parametro: "Tiempo", unidad: "ms", min: 35.0, max: 47.0, fechaMod: "2026-08-03 11:13:00", usuario: "Ajuste_Tiempo_Tucker" },
            { parametro: "Energia", unidad: "J", min: 1250.0, max: 1800.0, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" },
            { parametro: "Penetracion", unidad: "mm", min: -1.80, max: -0.80, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" },
            { parametro: "Elevacion", unidad: "mm", min: 1.8, max: 2.5, fechaMod: "2026-07-28 14:20:10", usuario: "Calidad_Admin" }
        ],
        1: [
            { parametro: "VolArc", unidad: "V", min: 16.0, max: 33.0, fechaMod: "2026-07-25 09:15:00", usuario: "Calidad_Admin" },
            { parametro: "Corriente", unidad: "A", min: 1250.0, max: 1320.0, fechaMod: "2026-07-25 09:15:00", usuario: "Calidad_Admin" },
            { parametro: "Tiempo", unidad: "ms", min: 35.0, max: 45.0, fechaMod: "2026-07-25 09:15:00", usuario: "Calidad_Admin" }
        ]
    },
    "CELDA-03": {
        1: [
            { parametro: "VolArc", unidad: "V", min: 18.0, max: 25.0, fechaMod: "2026-07-30 10:00:00", usuario: "Ingenieria" },
            { parametro: "Corriente", unidad: "A", min: 880.0, max: 960.0, fechaMod: "2026-07-30 10:00:00", usuario: "Ingenieria" },
            { parametro: "Tiempo", unidad: "ms", min: 25.0, max: 32.0, fechaMod: "2026-07-30 10:00:00", usuario: "Ingenieria" }
        ]
    }
};

let modalConfirm;
let toastOk;

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

    // Buscar la fecha de modificación más reciente para mostrar en la cabecera
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

function guardarReceta() {
    modalConfirm.hide();

    const celda = document.getElementById("select-receta-celda").value;
    const salida = document.getElementById("select-receta-salida").value;
    const fechaActual = obtenerFechaActualFormateada();

    const params = (recetasBD[celda] && recetasBD[celda][salida]) ? recetasBD[celda][salida] : [];

    // Actualizar valores y fechas simuladas
    params.forEach((row, index) => {
        const minVal = parseFloat(document.getElementById(`min-${index}`).value);
        const maxVal = parseFloat(document.getElementById(`max-${index}`).value);

        // Si cambió algún valor, actualizamos la fecha de modificación
        if (row.min !== minVal || row.max !== maxVal) {
            row.min = minVal;
            row.max = maxVal;
            row.fechaMod = fechaActual;
            row.usuario = "Usuario_Web";
        }
    });

    // Recargar vista para mostrar los nuevos sellos de fecha
    cargarRecetaSeleccionada();

    // Notificar al usuario con el Toast
    document.getElementById("toast-mensaje").innerHTML = `<i class="bi bi-check-circle-fill me-2"></i>¡Receta actualizada con fecha ${fechaActual}!`;
    toastOk.show();
}

document.addEventListener("DOMContentLoaded", () => {
    modalConfirm = new bootstrap.Modal(document.getElementById('modalConfirmar'));
    toastOk = new bootstrap.Toast(document.getElementById('toastSuccess'));

    cargarRecetaSeleccionada();
});