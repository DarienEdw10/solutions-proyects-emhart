from datetime import datetime, timedelta
import logging
import os
import re
import time
import pandas as pd
import serial
from conector import ConectorSQLServer

# =====================================================================
# CONFIGURACIÓN DEL SISTEMA DE LOGGING (Consola + Archivo de Bitácora)
# =====================================================================
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler("sistema_tucker.log", encoding="utf-8"),
        logging.StreamStreamHandler() if hasattr(logging, "StreamStreamHandler") else logging.StreamHandler()
    ]
)

# =====================================================================
# CONFIGURACIONES Y CONSTANTES INDUSTRIALES
# =====================================================================
PUERTO_SERIAL = 'COM6'
BAUDRATE = 57600
TIMEOUT_PUERTO = 0.3  # Timeout no bloqueante para lecturas fluidas
ARCHIVO_RESPALDO = 'respaldo_tucker.csv'

# Configuración de zona horaria:
# 0 = Hora local (México), 7 = Hora de Europa (UTC+1/UTC+2)
OFFSET_HORAS_MAQUINA = 0 

CADENA_CONEXION_BD = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=DESKTOP-D946QNA\\SQLEXPRESS;"
    "DATABASE=emhart;"
    "Trusted_Connection=yes;"
)

MAPEO_LINEAS = {
    1: "Out 1", 
    2: "Wheel House Out 2", 
    3: "Wheel House Out 3", 
    4: "Out 4", 
    5: "Out 5"
}

# Rangos de Recetas específicos por cada Salida (Pistola 2 y Pistola 3) según la HMI
RANGOS_RECETA = {
    2: {  # Salida 2 (Wheel House Out 2)
        "VolArc": {"min": 16, "max": 33},
        "VolPri": {"min": 21, "max": 33},
        "Corriente": {"min": 1270, "max": 1330},
        "Tiempo": {"min": 37, "max": 44},
        "Energia": {"min": 1100, "max": 1500}
    },
    3: {  # Salida 3 (Wheel House Out 3)
        "VolArc": {"min": 15, "max": 33},
        "VolPri": {"min": 21, "max": 33},
        "Corriente": {"min": 1410, "max": 1470},
        "Tiempo": {"min": 46, "max": 53},
        "Energia": {"min": 1550, "max": 2280}
    }
}

def bcd_a_int(bcd_val):
    """Convierte un byte en formato BCD (Binary Coded Decimal) a entero decimal."""
    try:
        return ((bcd_val >> 4) * 10) + (bcd_val & 0x0F)
    except Exception:
        return bcd_val

class ProcesadorTramasIndustriales:
    def __init__(self, puerto, baudrate, conexion_bd):
        self.puerto = puerto
        self.baudrate = baudrate
        self.conexion_bd = conexion_bd
        
        # Buffer circulante para evitar duplicados sin bloquear secuencias
        self.historico_procesados = set() 
        
        self.bd = ConectorSQLServer(connection_string=self.conexion_bd, table_name="emhartt.dbo.parametros")
        self.bd_conectada = False
        
        # Variables de seguimiento de estado
        self.ultimo_cnt_s2 = 0
        self.ultimo_cnt_s3 = 0
        self.ultima_fecha_s2 = ""
        self.ultima_fecha_s3 = ""

    def inicializar_puerto(self):
        try:
            ser = serial.Serial(
                port=self.puerto, 
                baudrate=self.baudrate, 
                bytesize=serial.EIGHTBITS, 
                stopbits=serial.STOPBITS_ONE, 
                timeout=TIMEOUT_PUERTO
            )
            logging.info(f"[OK] Puerto serie {self.puerto} enlazado correctamente.")
            return ser
        except Exception as e:
            logging.error(f"[ERROR] Interfaz física inaccesible en {self.puerto}: {e}")
            return None

    def procesar_trama(self, valores):
        # 1. VERIFICAR MODO DE DISPARO ACTIVO (Modo 1 = Disparo Real)
        modo_maquina = int(valores[105]) if len(valores) > 105 else 2
        if modo_maquina != 1:
            return None

        # 2. CAPTURA Y DECODIFICACIÓN DE LA FECHA/HORA REAL DEL DISPARO
        fecha_maquina = None
        fecha_obj_cruda = None

        try:
            # Decodificación BCD
            b_ano = bcd_a_int(int(valores[93]))
            b_mes = bcd_a_int(int(valores[94]))
            b_dia = bcd_a_int(int(valores[95]))
            b_hor = bcd_a_int(int(valores[92]))
            b_min = bcd_a_int(int(valores[91]))
            b_seg = bcd_a_int(int(valores[90]))

            if (1 <= b_mes <= 12) and (1 <= b_dia <= 31) and (0 <= b_hor <= 23):
                ano_full = 2000 + b_ano if b_ano < 100 else b_ano
                fecha_obj_cruda = datetime(ano_full, b_mes, b_dia, b_hor, b_min, b_seg)
                fecha_maquina = (fecha_obj_cruda - timedelta(hours=OFFSET_HORAS_MAQUINA)).strftime("%Y-%m-%d %H:%M:%S")
        except Exception:
            pass

        if not fecha_maquina:
            try:
                # Interpretación entera binaria directa
                ano = 2000 + int(valores[93])
                mes = int(valores[94])
                dia = int(valores[95])
                hor = int(valores[92])
                mi = int(valores[91])
                seg = int(valores[90])

                fecha_obj_cruda = datetime(ano, mes, dia, hor, mi, seg)
                fecha_maquina = (fecha_obj_cruda - timedelta(hours=OFFSET_HORAS_MAQUINA)).strftime("%Y-%m-%d %H:%M:%S")
            except Exception:
                # Fallback de seguridad a la hora de la PC local si la trama viene corrupta
                fecha_maquina = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        if fecha_obj_cruda:
            logging.debug(
                f"⏱️ Hora Cruda: {fecha_obj_cruda.strftime('%Y-%m-%d %H:%M:%S')} | "
                f"Offset: -{OFFSET_HORAS_MAQUINA}h | Procesada: {fecha_maquina}"
            )

        # 3. LECTURA DE CONTADOR Y NORMALIZACIÓN BINARIA
        salida_fisica = int(valores[218]) if len(valores) > 218 else 2

        byte_bajo = int(valores[108]) if len(valores) > 108 else 0
        byte_alto = int(valores[109]) if len(valores) > 109 else 0
        cnt_raw = byte_bajo + (byte_alto * 256)

        if cnt_raw <= 0:
            return None

        cnt_trama = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw

        # 4. IDENTIFICACIÓN DE SALIDA Y FILTRADO ANTI-DUPLICADOS INDEPENDIENTE
        if salida_fisica in [3, 10]:
            salida_val = 3
        else:
            salida_val = 2

        sol_real = cnt_trama
        prog_val = int(valores[205]) if len(valores) > 205 else 1
        energia_raw = int(valores[64]) + int(valores[65]) * 256 if len(valores) > 65 else 0

        # Crear firma/llave única de este evento de soldadura
        llave_evento = (salida_val, fecha_maquina, sol_real, prog_val, energia_raw)

        # Filtrar ecos o polleo repetido de la misma soldadura
        if llave_evento in self.historico_procesados:
            return None

        self.historico_procesados.add(llave_evento)
        if len(self.historico_procesados) > 500:
            self.historico_procesados.pop()

        # Actualizar contadores por salida
        if salida_val == 3:
            self.ultimo_cnt_s3 = sol_real
            self.ultima_fecha_s3 = fecha_maquina
        else:
            self.ultimo_cnt_s2 = sol_real
            self.ultima_fecha_s2 = fecha_maquina

        # 5. EXTRAER ID DE MÁQUINA
        try:
            id_raw = [valores[idx] for idx in range(221, 205, -1) if idx < len(valores)]
            id_completo = "".join([chr(b) for b in id_raw if 32 <= b <= 126]).strip()
        except Exception:
            id_completo = "TCK-SYS"

        # 6. PARÁMETROS TÉCNICOS ESCALADOS
        vol_pri_raw = int(valores[48]) + int(valores[49]) * 256 if len(valores) > 49 else 0
        vol_pri = round(vol_pri_raw / 10.0, 1)

        vol_arc_raw = int(valores[44]) + int(valores[45]) * 256 if len(valores) > 45 else 0
        vol_arc = round(vol_arc_raw / 10.0, 1)

        corriente = int(valores[102]) + int(valores[103]) * 256 if len(valores) > 103 else 0
        energia = energia_raw

        tiempo_raw = int(valores[42]) + int(valores[43]) * 256 if len(valores) > 43 else 0
        tiempo = round(tiempo_raw / 10.0, 1)

        elev_raw = int(valores[100]) + int(valores[101]) * 256 if len(valores) > 101 else 0
        elevacion = round(elev_raw / 100.0, 2)

        pen_raw = int(valores[85]) + int(valores[86]) * 256 if len(valores) > 86 else 0
        if pen_raw > 32768: 
            pen_raw -= 65536
        penetracion = round(pen_raw / 100.0, 2)

        lon_raw = int(valores[25]) + int(valores[26]) * 256 if len(valores) > 26 else 0
        lon_per = round(lon_raw / 100.0, 2) if lon_raw < 1000 else round(lon_raw / 1000.0, 2)

        fecha_clean = fecha_maquina.replace("-","").replace(":","").replace(" ","")
        serial_trazabilidad = f"TCK-{fecha_clean}-O{salida_val}-P{prog_val}-G{sol_real}"

        return {
            "Fecha": str(fecha_maquina), 
            "Serial_Trazabilidad": str(serial_trazabilidad), 
            "ID": str(id_completo) if id_completo else "TCK-SYS",
            "VolArc": float(vol_arc), 
            "VolPri": float(vol_pri), 
            "Salida": int(salida_val), 
            "Programa": int(prog_val), 
            "Elevacion": float(elevacion), 
            "Caida": 0, 
            "Penetracion": float(penetracion), 
            "Energia": int(energia), 
            "Corriente": int(corriente), 
            "Tiempo": float(tiempo), 
            "NumSol": int(sol_real), 
            "LonPer": float(lon_per),
            "Err": int(valores[110]) if len(valores) > 110 else 0, 
            "Alarma": int(valores[111]) if len(valores) > 111 else 0, 
            "Modo": int(modo_maquina), 
            "Presion": int(valores[27]) if len(valores) > 27 else 0
        }

    def intentar_conexion_bd(self):
        try:
            self.bd.connect()
            self.bd_conectada = True
            logging.info("[BD] Conexión establecida de forma exitosa.")
            self.sincronizar_respaldos_locales()
        except Exception as e:
            self.bd_conectada = False
            logging.warning(f"[BD] Servidor offline o inaccesible. Modo de respaldo local activado. Motivo: {e}")

    def gestionar_persistencia(self, data):
        if self.bd_conectada:
            try:
                self.bd.insertar_parametros(
                    str(data["Fecha"]), int(data["NumSol"]), float(data["VolArc"]), float(data["VolPri"]), 
                    int(data["Salida"]), int(data["Programa"]), float(data["Elevacion"]), int(data["Caida"]), 
                    float(data["Penetracion"]), int(data["Energia"]), int(data["Corriente"]), float(data["Tiempo"]), 
                    float(data["LonPer"]), str(data["Linea"]), str(data["ID"])
                )
                return "GUARDADO EN BASE DE DATOS (SQL Server)"
            except Exception as e:
                logging.error(f"Error al insertar en BD: {e}")
                self.bd_conectada = False
        
        df_local = pd.DataFrame([data])
        header_needed = not os.path.exists(ARCHIVO_RESPALDO)
        df_local.to_csv(ARCHIVO_RESPALDO, mode='a', index=False, header=header_needed)
        return f"ALMACENADO LOCALMENTE EN RESPALDO ({ARCHIVO_RESPALDO})"

    def sincronizar_respaldos_locales(self):
        if not os.path.exists(ARCHIVO_RESPALDO): 
            return
        
        logging.info("[SINCRONIZACIÓN] Volcando respaldos locales a SQL Server...")
        try:
            df_respaldos = pd.read_csv(ARCHIVO_RESPALDO)
            for _, row in df_respaldos.iterrows():
                self.bd.insertar_parametros(
                    str(row["Fecha"]), int(row["NumSol"]), float(row["VolArc"]), float(row["VolPri"]), 
                    int(row["Salida"]), int(row["Programa"]), float(row["Elevacion"]), int(row["Caida"]), 
                    float(row["Penetracion"]), int(row["Energia"]), int(row["Corriente"]), float(row["Tiempo"]), 
                    float(row["LonPer"]), str(row["Linea"]), str(row["ID"])
                )
            os.remove(ARCHIVO_RESPALDO)
            logging.info("[SINCRONIZACIÓN] Respaldo local vaciado con éxito.")
        except Exception as e:
            logging.error(f"[SINCRONIZACIÓN] Error al sincronizar: {e}")

    def imprimir_consola_descriptiva(self, data, destino_status):
        salida = data["Salida"]
        receta = RANGOS_RECETA.get(salida, RANGOS_RECETA[2])

        log_msg = (
            f"\n=====================================================================\n"
            f" 📦 REGISTRO PROCESADO: {data['Linea']} | Status: {destino_status}\n"
            f"=====================================================================\n"
            f" 🔍 [TRAZABILIDAD]   Serial Único : {data['Serial_Trazabilidad']}\n"
            f"                    ID Máquina   : '{data['ID']}'\n"
            f" 🕒 [TIEMPO]        Máquina      : {data['Fecha']}\n"
            f" ⚡ [ELÉCTRICOS]    Arco Piloto  : {data['VolArc']} V [Rango: {receta['VolArc']['min']}-{receta['VolArc']['max']} V]\n"
            f"                    V. Principal : {data['VolPri']} V [Rango: {receta['VolPri']['min']}-{receta['VolPri']['max']} V]\n"
            f"                    Corriente    : {data['Corriente']} A [Rango: {receta['Corriente']['min']}-{receta['Corriente']['max']} A]\n"
            f"                    Energía      : {data['Energia']} J [Rango: {receta['Energia']['min']}-{receta['Energia']['max']} J]\n"
            f" ⚙️  [MECÁNICOS]     Tiempo Arco  : {data['Tiempo']} ms | Caída: {data['Caida']} ms\n"
            f"                    Elevación    : {data['Elevacion']} mm | Perno: {data['LonPer']} mm\n"
            f"                    Penetración  : {data['Penetracion']} mm\n"
            f" 📊 [CONTADORES]    Pistola (Salida {data['Salida']}): {data['NumSol']} soldaduras\n"
            f" 🚨 [DIAGNÓSTICO]   Cód. Error   : {data['Err']} | Alarma: {data['Alarma']} | Modo: {data['Modo']}\n"
            f"=====================================================================\n"
        )
        logging.info(log_msg)

    def ejecutar_monitoreo(self):
        ser = self.inicializar_puerto()
        if not ser: 
            return
        
        self.intentar_conexion_bd()

        # DRAIN E INICIALIZACIÓN DE BÚFER
        try:
            ser.reset_input_buffer()
            ser.write(b'\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00')
            
            # Lectura impulsada por el timeout del puerto serie (sin time.sleep)
            raw_bytes = ser.read(300)
            if len(raw_bytes) >= 200:
                valores = [int(b) for b in raw_bytes]
                cnt_raw = int(valores[108]) + (int(valores[109]) * 256) if len(valores) > 109 else 0
                salida_init = int(valores[218]) if len(valores) > 218 else 2
                cnt_init = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw
                
                if salida_init in [3, 10]:
                    self.ultimo_cnt_s3 = cnt_init
                else:
                    self.ultimo_cnt_s2 = cnt_init

                logging.info("[SYNC] Estado base sincronizado. Aguardando disparos en tiempo real...")
        except Exception as e:
            logging.warning(f"[SYNC] Advertencia en sincronización inicial: {e}")

        # BUCLE PRINCIPAL DE MONITORIZACIÓN
        try:
            with ser:
                while True:
                    if not self.bd_conectada:
                        self.intentar_conexion_bd()

                    ser.reset_input_buffer()
                    ser.write(b'\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00')
                    
                    # Lectura basada en el timeout nativo del conector serie
                    raw_bytes = ser.read(300)
                    if len(raw_bytes) >= 200:
                        valores_trama = [int(b) for b in raw_bytes]
                        data = self.procesar_trama(valores_trama)
                        
                        if data is not None:
                            out_val = data["Salida"]
                            num_sol = data["NumSol"]

                            if out_val in MAPEO_LINEAS and num_sol > 0:
                                data["Linea"] = MAPEO_LINEAS[out_val]
                                status_almacenamiento = self.gestionar_persistencia(data)
                                self.imprimir_consola_descriptiva(data, status_almacenamiento)

        except KeyboardInterrupt:
            logging.info("[INFO] Monitoreo detenido manualmente por el usuario.")
        finally:
            if hasattr(self.bd, 'disconnect') and self.bd_conectada:
                self.bd.disconnect()

if __name__ == "__main__":
    procesador = ProcesadorTramasIndustriales(PUERTO_SERIAL, BAUDRATE, CADENA_CONEXION_BD)
    procesador.ejecutar_monitoreo()