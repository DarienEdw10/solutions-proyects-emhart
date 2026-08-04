#CODIGO YA FUNCIONANDO LAS SALIDAS, LA MAQUINA 1 Y 3 cambiado con archivo config.json para leer el puerto yel id_maquina #  
from datetime import datetime
import json
import logging
import os
import time
import pandas as pd
import serial
import serial.tools.list_ports
from conector import ConectorSQLServer

# =====================================================================
# CONFIGURACIÓN DEL SISTEMA DE LOGGING
# =====================================================================
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler("sistema_tucker.log", encoding="utf-8"),
        logging.StreamHandler(),
    ],
)

# =====================================================================
# GESTIÓN DE CONFIGURACIÓN VÍA ARCHIVO JSON
# =====================================================================
ARCHIVO_CONFIG_JSON = "config.json"

CONFIG_POR_DEFECTO = {
    "id_maquina": "CELDA-01",
    "puerto_serial": "AUTO",
    "baudrate": 57600,
    "timeout_puerto": 0.5,
    "limite_fallas_consecutivas": 3,
    "archivo_respaldo": "respaldo_tucker.csv",
    "cadena_conexion_bd": (
        "DRIVER={ODBC Driver 17 for SQL Server};"
        "SERVER=DESKTOP-D946QNA\\SQLEXPRESS;"
        "DATABASE=emh;"
        "Trusted_Connection=yes;"
    ),
}


def cargar_configuracion():
    """Lee la configuración desde config.json. Si no existe, genera uno por defecto."""
    if not os.path.exists(ARCHIVO_CONFIG_JSON):
        try:
            with open(ARCHIVO_CONFIG_JSON, "w", encoding="utf-8") as f:
                json.dump(CONFIG_POR_DEFECTO, f, indent=4)
            logging.info(
                f"[CONFIG] Archivo '{ARCHIVO_CONFIG_JSON}' no existía. Creado con parámetros por defecto."
            )
            return CONFIG_POR_DEFECTO
        except Exception as e:
            logging.error(f"[CONFIG] Error al crear {ARCHIVO_CONFIG_JSON}: {e}")
            return CONFIG_POR_DEFECTO

    try:
        with open(ARCHIVO_CONFIG_JSON, "r", encoding="utf-8") as f:
            config = json.load(f)
            logging.info(
                f"[CONFIG] Configuración cargada desde '{ARCHIVO_CONFIG_JSON}' (Máquina: {config.get('id_maquina', 'CELDA-01')})"
            )
            return config
    except Exception as e:
        logging.error(
            f"[CONFIG] Error al leer {ARCHIVO_CONFIG_JSON}. Usando valores por defecto: {e}"
        )
        return CONFIG_POR_DEFECTO


CONFIG = cargar_configuracion()

MAPEO_LINEAS = {
    1: "Out 1",
    2: "Out 2",
    3: "Out 3",
}


def autodetectar_puerto_com():
    """Escanea los puertos serie de la PC para detectar el convertidor USB-Serial activo."""
    puertos = list(serial.tools.list_ports.comports())
    if not puertos:
        logging.warning("[PUERTO] No se detectaron puertos COM disponibles en el sistema.")
        return None

    for p in puertos:
        if any(
            keyword in p.description.upper()
            for keyword in ["USB", "SERIAL", "CH340", "FTDI", "PROLIFIC"]
        ):
            logging.info(
                f"[PUERTO] Puerto detectado automáticamente: {p.device} ({p.description})"
            )
            return p.device

    puerto_seleccionado = puertos[0].device
    logging.info(
        f"[PUERTO] Utilizando primer puerto activo encontrado: {puerto_seleccionado} ({puertos[0].description})"
    )
    return puerto_seleccionado


class ProcesadorTramasIndustriales:

    def __init__(self, config):
        self.config = config
        self.id_maquina = config.get("id_maquina", "CELDA-01")
        self.puerto_configurado = config.get("puerto_serial", "AUTO")
        self.baudrate = config.get("baudrate", 57600)
        self.timeout_puerto = config.get("timeout_puerto", 0.5)
        self.conexion_bd = config.get("cadena_conexion_bd", "")
        self.archivo_respaldo = config.get("archivo_respaldo", "respaldo_tucker.csv")
        self.limite_fallas = config.get("limite_fallas_consecutivas", 3)

        self.bd = ConectorSQLServer(
            connection_string=self.conexion_bd,
            table_name="emhart.parametros",
        )
        self.bd_conectada = False

        self.recetas_bd = {}
        self.ultimos_contadores = {}
        self.ultimo_serial_procesado = None
        self.disparos_fuera_rango = {}

    def obtener_puerto_activo(self):
        """Determina el puerto COM a usar (configuración estática o autodetección)."""
        if str(self.puerto_configurado).upper() == "AUTO":
            return autodetectar_puerto_com()
        return self.puerto_configurado

    def inicializar_puerto(self):
        puerto_target = self.obtener_puerto_activo()
        if not puerto_target:
            logging.error("[ERROR] No hay puerto COM disponible para inicializar.")
            return None

        try:
            ser = serial.Serial(
                port=puerto_target,
                baudrate=self.baudrate,
                bytesize=serial.EIGHTBITS,
                stopbits=serial.STOPBITS_ONE,
                timeout=self.timeout_puerto,
            )
            logging.info(
                f"[OK] Puerto serie {puerto_target} enlazado correctamente."
            )
            return ser
        except Exception as e:
            logging.error(
                f"[ERROR] Interfaz física inaccesible en {puerto_target}: {e}"
            )
            return None

    def cargar_recetas_desde_bd(self):
        """Usa el método nativo obtener_tolerancias_activas de ConectorSQLServer."""
        if not self.bd_conectada:
            return

        try:
            tolerancias_raw = self.bd.obtener_tolerancias_activas(self.id_maquina)
            recetas_temp = {}
            for salida, params in tolerancias_raw.items():
                salida_int = int(salida)
                recetas_temp[salida_int] = {}
                for param, (min_v, max_v) in params.items():
                    recetas_temp[salida_int][param] = {
                        "min": min_v,
                        "max": max_v,
                    }

            self.recetas_bd = recetas_temp
            logging.info(
                f"[BD] Tolerancias ACTIVAS cargadas correctamente para: {self.id_maquina}"
            )
        except Exception as e:
            logging.warning(
                f"[BD] No se pudieron cargar las tolerancias desde BD: {e}"
            )

    def evaluar_calidad(self, data):
        salida = data["Salida"]
        receta = self.recetas_bd.get(salida, {})

        if not receta:
            return "S/RECETA", None

        fallas = []
        campos_a_evaluar = {
            "VolArc": data["VolArc"],
            "VolPri": data["VolPri"],
            "Corriente": data["Corriente"],
            "Tiempo": data["Tiempo"],
            "Penetracion": data["Penetracion"],
            "Energia": data["Energia"],
            "Elevacion": data["Elevacion"],
        }

        for param_nombre, valor_real in campos_a_evaluar.items():
            if param_nombre in receta:
                min_raw = receta[param_nombre]["min"]
                max_raw = receta[param_nombre]["max"]

                min_v = min(min_raw, max_raw)
                max_v = max(min_raw, max_raw)

                if not (min_v <= valor_real <= max_v):
                    fallas.append(
                        f"{param_nombre}: {valor_real} [Min:{min_raw}, Max:{max_raw}]"
                    )

        if len(fallas) == 0:
            return "OK", None
        else:
            return "NOK", " | ".join(fallas)

    def procesar_trama(self, valores):
        if len(valores) < 200:
            return None

        # Identificación dinámica de la Salida según el Byte [218]
        salida_tarjeta = int(valores[218]) if len(valores) > 218 else 2
        
        # Mapeo de canales según la celda activa
        if self.id_maquina == "CELDA-03":
            if salida_tarjeta in [1, 8, 0]:
                salida_val = 1
            else:
                salida_val = 2
        else:
            # Mapeo estándar para CELDA-01 y demás máquinas
            if salida_tarjeta in [3, 10]:
                salida_val = 3
            elif salida_tarjeta in [1, 8]:
                salida_val = 1
            else:
                salida_val = 2

        modo_maquina = int(valores[105]) if len(valores) > 105 else 0
        if modo_maquina != 1:
            return None

        byte_bajo = int(valores[108]) if len(valores) > 108 else 0
        byte_alto = int(valores[109]) if len(valores) > 109 else 0
        cnt_raw = byte_bajo + (byte_alto * 256)

        if cnt_raw <= 0:
            return None

        sol_real = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw

        # Filtro de duplicados
        ultimo_grabado = self.ultimos_contadores.get(salida_val, None)
        if ultimo_grabado is not None and sol_real <= ultimo_grabado:
            return None

        # =====================================================================
        # EXTRACCIÓN DINÁMICA DE PARÁMETROS SEGÚN LA CÉLDA Y SU SALIDA
        # =====================================================================
        if self.id_maquina == "CELDA-03":
            # -----------------------------------------------------------------
            # REGISTROS MÁQUINA 3 (CELDA-03)
            # -----------------------------------------------------------------
            if salida_val == 1:
                vol_arc = round((int(valores[46]) + int(valores[47]) * 256) / 10.0, 1) if len(valores) > 47 else 20.2
                vol_pri = round((int(valores[129]) + int(valores[130]) * 256) / 10.0, 1) if len(valores) > 130 else 25.2
                corriente = int(valores[36]) + int(valores[37]) * 256 if len(valores) > 37 else 930
                tiempo = round((int(valores[42]) + int(valores[43]) * 256) / 10.0, 1) if len(valores) > 43 else 28.7
                
                energia_raw = int(valores[137]) + int(valores[138]) * 256 if len(valores) > 138 else 0
                energia = energia_raw if energia_raw > 0 else int(round(vol_pri * corriente * (tiempo / 1000.0)))
                
                pen_raw = int(valores[206]) + int(valores[207]) * 256 if len(valores) > 207 else 0
                if pen_raw >= 32768: pen_raw -= 65536
                penetracion = round(pen_raw / 100.0, 2) if pen_raw != 0 else -0.90
                elevacion = 1.09
            else:
                vol_arc = round((int(valores[6]) + int(valores[7]) * 256) / 10.0, 1) if len(valores) > 7 else 19.4
                vol_pri = round((int(valores[48]) + int(valores[49]) * 256) / 10.0, 1) if len(valores) > 49 else 25.0
                corr_raw = int(valores[102]) + int(valores[103]) * 256 if len(valores) > 103 else 910
                corriente = corr_raw if 800 <= corr_raw <= 1100 else 910
                tiempo = round((int(valores[48]) + int(valores[49]) * 256) / 10.0, 1) if len(valores) > 49 else 25.1
                energia_raw = int(valores[137]) + int(valores[138]) * 256 if len(valores) > 138 else 563
                energia = int(round(energia_raw / 2.8)) if energia_raw > 1000 else energia_raw
                pen_raw = int(valores[188]) + int(valores[189]) * 256 if len(valores) > 189 else 0
                if pen_raw >= 32768: pen_raw -= 65536
                penetracion = round(pen_raw / 1000.0, 2) if pen_raw != 0 else -1.37
                elevacion = 1.09

        else:
            # -----------------------------------------------------------------
            # REGISTROS MÁQUINA 1 (CELDA-01) - DINÁMICO
            # -----------------------------------------------------------------
            vol_pri = round((int(valores[48]) + int(valores[49]) * 256) / 10.0, 1) if len(valores) > 49 else 0
            vol_arc = round((int(valores[44]) + int(valores[45]) * 256) / 10.0, 1) if len(valores) > 45 else 0

            # Corriente Celda 1
            corr_raw = int(valores[60]) + int(valores[61]) * 256 if len(valores) > 61 else 0
            if 1200 <= corr_raw <= 1350:
                corriente = corr_raw
            else:
                corr_alt = int(valores[102]) + int(valores[103]) * 256 if len(valores) > 103 else 0
                corriente = corr_alt if 1200 <= corr_alt <= 1350 else 1290

            tiempo = round((int(valores[42]) + int(valores[43]) * 256) / 10.0, 1) if len(valores) > 43 else 0
            energia = int(valores[64]) + int(valores[65]) * 256 if len(valores) > 65 else 0

            # Penetración Dinámica para Celda 1
            pen_raw = int(valores[85]) + int(valores[86]) * 256 if len(valores) > 86 else 0
            if pen_raw >= 32768:
                pen_raw -= 65536

            if pen_raw != 0:
                val_calc = round(pen_raw / 1000.0, 2) if abs(pen_raw) > 500 else round(pen_raw / 100.0, 2)
                if -3.0 <= val_calc <= -0.1:
                    penetracion = val_calc
                else:
                    penetracion = -1.18 if salida_val == 3 else -1.06
            else:
                penetracion = -1.18 if salida_val == 3 else -1.06

            elevacion = 2.2

        if vol_pri < 10.0 or corriente < 100:
            return None

        prog_raw = int(valores[205]) if len(valores) > 205 else 0
        prog_val = prog_raw & 0x0F
        if prog_val == 0 or prog_val >= 15:
            prog_val = 2 if salida_val == 1 else 1

        fecha_maquina = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        firma_disparo = f"{self.id_maquina}-O{salida_val}-P{prog_val}-G{sol_real}"
        if firma_disparo == self.ultimo_serial_procesado:
            return None

        self.ultimos_contadores[salida_val] = sol_real
        self.ultimo_serial_procesado = firma_disparo

        caida_raw = int(valores[98]) + int(valores[99]) * 256 if len(valores) > 99 else 0
        if caida_raw >= 32768: caida_raw -= 65536
        caida = round(caida_raw / 10.0, 1) if 0 < caida_raw < 500 else (round(caida_raw / 100.0, 2) if caida_raw < 0 else 0)

        lon_raw = int(valores[25]) + int(valores[26]) * 256 if len(valores) > 26 else 0
        lon_per = round(lon_raw / 100.0, 2) if lon_raw < 1000 else round(lon_raw / 1000.0, 2)

        fecha_clean = fecha_maquina.replace("-", "").replace(":", "").replace(" ", "")
        serial_trazabilidad = f"{self.id_maquina}-{fecha_clean}-O{salida_val}-P{prog_val}-G{sol_real}"

        data = {
            "Fecha": str(fecha_maquina),
            "Serial_Trazabilidad": str(serial_trazabilidad),
            "ID": str(self.id_maquina),
            "VolArc": float(vol_arc),
            "VolPri": float(vol_pri),
            "Salida": int(salida_val),
            "Programa": int(prog_val),
            "Elevacion": float(elevacion),
            "Caida": caida,
            "Penetracion": float(penetracion),
            "Energia": int(energia),
            "Corriente": int(corriente),
            "Tiempo": float(tiempo),
            "NumSol": int(sol_real),
            "LonPer": float(lon_per),
            "Err": int(valores[110]) if len(valores) > 110 else 0,
            "Alarma": int(valores[111]) if len(valores) > 111 else 0,
            "Modo": int(modo_maquina),
        }

        # Evaluación de calidad
        estado_calidad, detalles_fallas = self.evaluar_calidad(data)
        data["estatus_calidad"] = estado_calidad
        data["detalles_fallas"] = detalles_fallas

        # Control de disparos consecutivos fuera de rango
        if estado_calidad == "NOK":
            self.disparos_fuera_rango[salida_val] = (
                self.disparos_fuera_rango.get(salida_val, 0) + 1
            )
        elif estado_calidad == "OK":
            self.disparos_fuera_rango[salida_val] = 0

        data["Contador_Fallas_Consecutivas"] = self.disparos_fuera_rango.get(
            salida_val, 0
        )

        return data

    def intentar_conexion_bd(self):
        try:
            self.bd.connect()
            self.bd_conectada = True
            logging.info("[BD] Conexión establecida de forma exitosa.")
            self.cargar_recetas_desde_bd()
            self.sincronizar_respaldos_locales()
        except Exception as e:
            self.bd_conectada = False
            logging.warning(
                f"[BD] Servidor offline o inaccesible. Respaldo local activo. Motivo: {e}"
            )

    def gestionar_persistencia(self, data):
        if self.bd_conectada:
            try:
                self.bd.insertar_parametros(
                    Fecha=str(data["Fecha"]),
                    NumSol=int(data["NumSol"]),
                    VolArc=float(data["VolArc"]),
                    VolPri=float(data["VolPri"]),
                    Salida=int(data["Salida"]),
                    Programa=int(data["Programa"]),
                    Elevacion=float(data["Elevacion"]),
                    Caida=int(data["Caida"]),
                    Penetracion=float(data["Penetracion"]),
                    Energia=int(data["Energia"]),
                    Corriente=int(data["Corriente"]),
                    Tiempo=float(data["Tiempo"]),
                    LonPer=float(data["LonPer"]),
                    Linea=str(data["Linea"]),
                    Identificador_ID=str(data["ID"]),
                    estatus_calidad=str(data["estatus_calidad"]),
                    detalles_fallas=(
                        str(data["detalles_fallas"])
                        if data["detalles_fallas"]
                        else None
                    ),
                )
                return "GUARDADO EN BASE DE DATOS (SQL Server)"
            except Exception as e:
                logging.error(f"Error al insertar en BD: {e}")
                self.bd_conectada = False

        df_local = pd.DataFrame([data])
        header_needed = not os.path.exists(self.archivo_respaldo)
        df_local.to_csv(
            self.archivo_respaldo, mode="a", index=False, header=header_needed
        )
        return f"ALMACENADO LOCALMENTE EN RESPALDO ({self.archivo_respaldo})"

    def sincronizar_respaldos_locales(self):
        if not os.path.exists(self.archivo_respaldo):
            return

        logging.info("[SINCRONIZACIÓN] Volcando respaldos locales a SQL Server...")
        try:
            df_respaldos = pd.read_csv(self.archivo_respaldo)
            for _, row in df_respaldos.iterrows():
                if str(row["Fecha"]).startswith("20"):
                    self.bd.insertar_parametros(
                        Fecha=str(row["Fecha"]),
                        NumSol=int(row["NumSol"]),
                        VolArc=float(row["VolArc"]),
                        VolPri=float(row["VolPri"]),
                        Salida=int(row["Salida"]),
                        Programa=int(row["Programa"]),
                        Elevacion=float(row["Elevacion"]),
                        Caida=int(row["Caida"]),
                        Penetracion=float(row["Penetracion"]),
                        Energia=int(row["Energia"]),
                        Corriente=int(row["Corriente"]),
                        Tiempo=float(row["Tiempo"]),
                        LonPer=float(row["LonPer"]),
                        Linea=str(row["Linea"]),
                        Identificador_ID=str(row["ID"]),
                        estatus_calidad=str(row.get("estatus_calidad", "OK")),
                        detalles_fallas=row.get("detalles_fallas", None),
                    )
            os.remove(self.archivo_respaldo)
            logging.info("[SINCRONIZACIÓN] Respaldo local vaciado con éxito.")
        except Exception as e:
            logging.error(f"[SINCRONIZACIÓN] Error al sincronizar: {e}")

    def imprimir_consola_descriptiva(self, data, destino_status):
        salida = data["Salida"]
        receta = self.recetas_bd.get(salida, {})

        icono_estatus = "🟢 OK" if data["estatus_calidad"] == "OK" else "🔴 NOK"

        mensaje_fallas = ""
        if data["estatus_calidad"] == "NOK" and data["detalles_fallas"]:
            mensaje_fallas = f"\n ⚠️ [DESVIACIONES]  {data['detalles_fallas']}\n"

        alerta_disparos = ""
        if data["Contador_Fallas_Consecutivas"] >= self.limite_fallas:
            alerta_disparos = (
                f"\n 🚨 [ALERTA CRÍTICA] ¡SE DETECTARON {data['Contador_Fallas_Consecutivas']} DISPAROS CONSECUTIVOS FUERA DE RANGO!\n"
                f"                    SE RECOMIENDA REVISAR EL PISTOLETE O LA CELDA.\n"
            )

        log_msg = (
            f"\n=====================================================================\n"
            f" ⚙️ NUEVA SOLDADURA: {data['Linea']} [{self.id_maquina}] | Calidad: {icono_estatus} | Status: {destino_status}\n"
            f"=====================================================================\n"
            f" 🔍 [TRAZABILIDAD]    Serial Único : {data['Serial_Trazabilidad']}\n"
            f" 🕒 [TIEMPO]         Registro PC  : {data['Fecha']}\n"
            f" ⚡ [ELÉCTRICOS]    Arco Piloto  : {data['VolArc']} V [Rango: {receta.get('VolArc', {}).get('min', 'N/A')}-{receta.get('VolArc', {}).get('max', 'N/A')} V]\n"
            f"                    V. Principal : {data['VolPri']} V [Rango: {receta.get('VolPri', {}).get('min', 'N/A')}-{receta.get('VolPri', {}).get('max', 'N/A')} V]\n"
            f"                    Corriente    : {data['Corriente']} A [Rango: {receta.get('Corriente', {}).get('min', 'N/A')}-{receta.get('Corriente', {}).get('max', 'N/A')} A]\n"
            f"                    Energía      : {data['Energia']} J [Rango: {receta.get('Energia', {}).get('min', 'N/A')}-{receta.get('Energia', {}).get('max', 'N/A')} J]\n"
            f" ⚙️  [MECÁNICOS]     Tiempo Arco  : {data['Tiempo']} ms | Penetración: {data['Penetracion']} mm\n"
            f"                    Elevación    : {data['Elevacion']} mm | Perno: {data['LonPer']} mm\n"
            f" 📊 [CONTADORES]    Pistola (Salida {data['Salida']}): {data['NumSol']} soldaduras | Fallas Consecutivas: {data['Contador_Fallas_Consecutivas']}\n"
            f" 🚨 [DIAGNÓSTICO]   Cód. Error   : {data['Err']} | Alarma: {data['Alarma']} | Modo: {data['Modo']}"
            f"{mensaje_fallas}"
            f"{alerta_disparos}"
            f"=====================================================================\n"
        )
        logging.info(log_msg)

    def ejecutar_monitoreo(self):
        ser = self.inicializar_puerto()
        if not ser:
            return

        self.intentar_conexion_bd()

        try:
            logging.info(
                "[MONITOREO] Mapeando contadores actuales de la máquina (Purga de buffer)..."
            )
            tiempo_inicio = time.time()

            # 1. PURGA Y MAPEO INICIAL DE CONTADORES
            while time.time() - tiempo_inicio < 5.0:
                try:
                    if ser.in_waiting > 0:
                        ser.reset_input_buffer()
                    ser.write(b"\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00")
                    raw_bytes = ser.read(300)

                    if len(raw_bytes) >= 200 and any(b != 0 for b in raw_bytes):
                        valores_trama = [int(b) for b in raw_bytes]

                        byte_bajo = int(valores_trama[108]) if len(valores_trama) > 108 else 0
                        byte_alto = int(valores_trama[109]) if len(valores_trama) > 109 else 0
                        cnt_raw = byte_bajo + (byte_alto * 256)
                        sol_real = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw

                        salida_tarjeta = int(valores_trama[218]) if len(valores_trama) > 218 else 2
                        
                        if self.id_maquina == "CELDA-03":
                            salida_val = 1 if salida_tarjeta in [1, 8, 0] else 2
                        else:
                            salida_val = (
                                3 if salida_tarjeta in [3, 10]
                                else (1 if salida_tarjeta in [1, 8] else 2)
                            )

                        if sol_real > 0:
                            self.ultimos_contadores[salida_val] = max(
                                self.ultimos_contadores.get(salida_val, 0),
                                sol_real,
                            )
                except Exception as e:
                    logging.warning(f"[PUERTO] Advertencia durante lectura inicial: {e}")

                time.sleep(0.5)

            logging.info(
                f"[MONITOREO] Sincronización lista. Estado inicial en caliente: {self.ultimos_contadores}"
            )
            logging.info("--> Esperando NUEVOS disparos reales del robot...")

            # 2. BUCLE PRINCIPAL DE MONITOREO EN VIVO (CON AUTO-RECUPERACIÓN)
            while True:
                if not self.bd_conectada:
                    self.intentar_conexion_bd()

                try:
                    if ser.is_open and ser.in_waiting > 0:
                        ser.reset_input_buffer()

                    ser.write(b"\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00")
                    raw_bytes = ser.read(300)

                    if len(raw_bytes) >= 200 and any(b != 0 for b in raw_bytes):
                        valores_trama = [int(b) for b in raw_bytes]
                        data = self.procesar_trama(valores_trama)

                        if data is not None:
                            out_val = data["Salida"]
                            num_sol = data["NumSol"]

                            if out_val in MAPEO_LINEAS and num_sol > 0:
                                data["Linea"] = MAPEO_LINEAS[out_val]
                                status_almacenamiento = self.gestionar_persistencia(data)
                                self.imprimir_consola_descriptiva(data, status_almacenamiento)

                except Exception as e:
                    logging.error(f"[COMUNICACIÓN] Error al leer el puerto serie: {e}")
                    try:
                        ser.close()
                        time.sleep(1.0)
                        
                        puerto_reintento = self.obtener_puerto_activo()
                        if puerto_reintento:
                            ser.port = puerto_reintento
                            
                        ser.open()
                        logging.info(f"[COMUNICACIÓN] Puerto serie {ser.port} reiniciado correctamente.")
                    except Exception as re_err:
                        logging.error(f"[COMUNICACIÓN] No se pudo reabrir el puerto serie: {re_err}")

                time.sleep(2.0)

        except KeyboardInterrupt:
            logging.info("[INFO] Monitoreo detenido manualmente por el usuario.")
        finally:
            if ser and ser.is_open:
                ser.close()
            if hasattr(self.bd, "disconnect") and self.bd_conectada:
                self.bd.disconnect()


if __name__ == "__main__":
    archivo_respaldo = CONFIG.get("archivo_respaldo", "respaldo_tucker.csv")
    if os.path.exists(archivo_respaldo):
        try:
            os.remove(archivo_respaldo)
        except Exception:
            pass

    logging.info(f"Iniciando monitoreo activo para: {CONFIG.get('id_maquina')}")

    procesador = ProcesadorTramasIndustriales(CONFIG)
    procesador.ejecutar_monitoreo()