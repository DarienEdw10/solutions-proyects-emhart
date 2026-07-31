import os
import time
import serial
import pandas as pd
from datetime import datetime, timedelta
from conector import ConectorSQLServer

# =====================================================================
# CONFIGURACIONES Y CONSTANTES INDUSTRIALES
# =====================================================================
PUERTO_SERIAL = 'COM6'
BAUDRATE = 57600
ARCHIVO_RESPALDO = 'respaldo_tucker.csv'

CADENA_CONEXION_BD = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "SERVER=DESKTOP-D946QNA\\SQLEXPRESS;"
    "DATABASE=emhartt;"
    "Trusted_Connection=yes;"
)

MAPEO_LINEAS = {
    1: "Out 1", 
    2: "Wheel House Out 2", 
    3: "Wheel House Out 3", 
    4: "Out 4", 
    5: "Out 5"
}

RANGOS_RECETA = {
    "VolArc": {"min": 16, "max": 33},
    "VolPri": {"min": 240, "max": 265},
    "Energia": {"min": 1200, "max": 1500},
    "Corriente": {"min": 1270, "max": 1330},
    "Tiempo": {"min": 37, "max": 46}
}

class ProcesadorTramasIndustriales:
    def __init__(self, puerto, baudrate, conexion_bd):
        self.puerto = puerto
        self.baudrate = baudrate
        self.conexion_bd = conexion_bd
        self.historico_procesados = set() 
        self.bd = ConectorSQLServer(connection_string=self.conexion_bd, table_name="emhartt.dbo.parametros")
        self.bd_conectada = False
        
        # Variables de seguimiento dinámico
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
                timeout=0.5
            )
            print(f"[OK] Puerto {self.puerto} enlazado correctamente.")
            return ser
        except Exception as e:
            print(f"[ERROR] Interfaz física inaccesible en {self.puerto}: {e}")
            return None

    def procesar_trama(self, valores):
        # 1. MODO DE DISPARO ACTIVO (Modo 1 = Disparo Real)
        modo_maquina = int(valores[105]) if len(valores) > 105 else 2
        if modo_maquina != 1:
            return None

        # 2. FECHA MÁQUINA (Alemania -> México -7 hrs)
        try:
            fecha_alemania = datetime(2000 + int(valores[93]), int(valores[94]), int(valores[95]), int(valores[92]), int(valores[91]), int(valores[90]))
            fecha_maquina = (fecha_alemania - timedelta(hours=7)).strftime("%Y-%m-%d %H:%M:%S")
        except:
            fecha_maquina = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

        # 3. CANALIZACIÓN Y LECTURA DEDICADA POR PISTOLA
        salida_fisica = int(valores[218]) if len(valores) > 218 else 2

        if salida_fisica in [3, 10]:
            salida_val = 3
            # Lectura Salida 3 -> Bytes [108] y [109]
            byte_bajo = int(valores[108]) if len(valores) > 108 else 0
            byte_alto = int(valores[109]) if len(valores) > 109 else 0
            cnt_raw = byte_bajo + (byte_alto * 256)
            if cnt_raw <= 0: return None
            
            cnt_norm = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw
            
            # Anti-duplicado en la misma ventana de tiempo
            if fecha_maquina == self.ultima_fecha_s3 and cnt_norm == self.ultimo_cnt_s3:
                return None

            # Validación de avance incremental dinámico
            if self.ultimo_cnt_s3 > 0:
                diferencia = cnt_norm - self.ultimo_cnt_s3
                if diferencia <= 0 or diferencia > 20:
                    return None

            self.ultimo_cnt_s3 = cnt_norm
            self.ultima_fecha_s3 = fecha_maquina
            cnt_trama = cnt_norm

        else:
            salida_val = 2
            # Lectura Salida 2 -> Bytes nativos del PLC [145] y [146]
            byte_bajo = int(valores[145]) if len(valores) > 145 else 0
            byte_alto = int(valores[146]) if len(valores) > 146 else 0
            
            # Fallback a 108-109 si viniera en cero
            if byte_bajo == 0 and byte_alto == 0:
                byte_bajo = int(valores[108]) if len(valores) > 108 else 0
                byte_alto = int(valores[109]) if len(valores) > 109 else 0

            cnt_raw = byte_bajo + (byte_alto * 256)
            if cnt_raw <= 0: return None

            cnt_norm = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw

            # Anti-duplicado en la misma ventana de tiempo
            if fecha_maquina == self.ultima_fecha_s2 and cnt_norm == self.ultimo_cnt_s2:
                return None

            if self.ultimo_cnt_s2 > 0:
                diferencia = cnt_norm - self.ultimo_cnt_s2
                if diferencia <= 0 or diferencia > 20:
                    return None

            self.ultimo_cnt_s2 = cnt_norm
            self.ultima_fecha_s2 = fecha_maquina
            cnt_trama = cnt_norm

        sol_real = cnt_trama
        prog_val = int(valores[205]) if len(valores) > 205 else 1

        # 4. EXTRAER ID DE MÁQUINA
        try:
            id_raw = [valores[idx] for idx in range(221, 205, -1) if idx < len(valores)]
            id_completo = "".join([chr(b) for b in id_raw if 32 <= b <= 126]).strip()
        except:
            id_completo = "TCK-SYS"

        # 5. PARÁMETROS TÉCNICOS ESCALADOS
        vol_pri_raw = int(valores[48]) + int(valores[49]) * 256 if len(valores) > 49 else 0
        vol_pri = round(vol_pri_raw / 10.0, 1)

        vol_arc_raw = int(valores[44]) + int(valores[45]) * 256 if len(valores) > 45 else 0
        vol_arc = round(vol_arc_raw / 10.0, 1)

        corriente = int(valores[102]) + int(valores[103]) * 256 if len(valores) > 103 else 0
        energia = int(valores[64]) + int(valores[65]) * 256 if len(valores) > 65 else 0

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
            print("[BD] Conexión establecida de forma exitosa.")
            self.sincronizar_respaldos_locales()
        except Exception as e:
            self.bd_conectada = False
            print(f"[BD] Servidor offline o inaccesible. Modo de respaldo local activado. Motivo: {e}")

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
                print(f"⚠️ Error al insertar en BD: {e}")
                self.bd_conectada = False
        
        df_local = pd.DataFrame([data])
        header_needed = not os.path.exists(ARCHIVO_RESPALDO)
        df_local.to_csv(ARCHIVO_RESPALDO, mode='a', index=False, header=header_needed)
        return f"⚠️ ALMACENADO LOCALMENTE EN RESPALDO ({ARCHIVO_RESPALDO})"

    def sincronizar_respaldos_locales(self):
        if not os.path.exists(ARCHIVO_RESPALDO): return
        
        print(f"[SINCRONIZACIÓN] Volcando respaldos locales a la BD...")
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
            print("[SINCRONIZACIÓN] Respaldo vaciado con éxito.")
        except Exception as e:
            print(f"[SINCRONIZACIÓN] Error al sincronizar: {e}")

    def imprimir_consola_descriptiva(self, data, destino_status):
        print("\n=====================================================================")
        print(f" 📦 REGISTRO PROCESADO: {data['Linea']} | Status: {destino_status}")
        print("=====================================================================")
        print(f" 🔍 [TRAZABILIDAD]  Serial Único : {data['Serial_Trazabilidad']}")
        print(f"                    ID Máquina   : '{data['ID']}'")
        print(f" 🕒 [TIEMPO]        Máquina      : {data['Fecha']}")
        print(f" ⚡ [ELÉCTRICOS]    Arco Piloto  : {data['VolArc']} V [Rango: {RANGOS_RECETA['VolArc']['min']}-{RANGOS_RECETA['VolArc']['max']} V]")
        print(f"                    V. Principal : {data['VolPri']} V [Rango: {RANGOS_RECETA['VolPri']['min']}-{RANGOS_RECETA['VolPri']['max']} V]")
        print(f"                    Corriente    : {data['Corriente']} A [Rango: {RANGOS_RECETA['Corriente']['min']}-{RANGOS_RECETA['Corriente']['max']} A]")
        print(f"                    Energía      : {data['Energia']} J [Rango: {RANGOS_RECETA['Energia']['min']}-{RANGOS_RECETA['Energia']['max']} J]")
        print(f" ⚙️  [MECÁNICOS]     Tiempo Arco  : {data['Tiempo']} ms | Caída: {data['Caida']} ms")
        print(f"                    Elevación    : {data['Elevacion']} mm | Perno: {data['LonPer']} mm")
        print(f"                    Penetración  : {data['Penetracion']} mm")
        print(f" 📊 [CONTADORES]    Pistola (Salida {data['Salida']}): {data['NumSol']} soldaduras")
        print(f" 🚨 [DIAGNÓSTICO]   Cód. Error   : {data['Err']} | Alarma: {data['Alarma']} | Modo: {data['Modo']}")
        print("=====================================================================\n")

    def ejecutar_monitoreo(self):
        ser = self.inicializar_puerto()
        if not ser: return
        
        self.intentar_conexion_bd()

        # DRAIN INICIAL
        try:
            ser.reset_input_buffer()
            ser.write(b'\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00')
            time.sleep(0.3)
            if ser.inWaiting() >= 200:
                raw_bytes = ser.read(ser.inWaiting())
                valores = [int(b) for b in raw_bytes]
                if len(valores) >= 200:
                    salida_init = int(valores[218]) if len(valores) > 218 else 2
                    cnt_raw = int(valores[108]) + (int(valores[109]) * 256) if len(valores) > 109 else 0
                    cnt_norm = cnt_raw % 32768 if cnt_raw >= 32768 else cnt_raw
                    
                    if salida_init in [3, 10]:
                        self.ultimo_cnt_s3 = cnt_norm
                    else:
                        self.ultimo_cnt_s2 = cnt_norm

                    print(f"[SYNC] Sistema enlazado en tiempo real. Aguardando disparos...")
        except Exception as e:
            print(f"[SYNC] Advertencia en sincronización inicial: {e}")

        try:
            with ser:
                while True:
                    if not self.bd_conectada:
                        self.intentar_conexion_bd()

                    ser.reset_input_buffer()
                    ser.write(b'\x00\x80\x0c\x00\x20\x00\x42\x00\x00\x00\xee\x00')
                    time.sleep(0.3)
                    
                    bytes_esperando = ser.inWaiting()
                    if bytes_esperando >= 200:
                        raw_bytes = ser.read(bytes_esperando)
                        valores_trama = [int(b) for b in raw_bytes]
                        
                        if len(valores_trama) >= 200:
                            data = self.procesar_trama(valores_trama)
                            
                            if data is not None:
                                out_val = data["Salida"]
                                num_sol = data["NumSol"]

                                if out_val in MAPEO_LINEAS and num_sol > 0:
                                    llave_soldadura = (out_val, num_sol)
                                    
                                    if llave_soldadura not in self.historico_procesados:
                                        self.historico_procesados.add(llave_soldadura)
                                        data["Linea"] = MAPEO_LINEAS[out_val]
                                        
                                        status_almacenamiento = self.gestionar_persistencia(data)
                                        self.imprimir_consola_descriptiva(data, status_almacenamiento)
                    
                    time.sleep(0.5)
                                
        except KeyboardInterrupt:
            print("\n[INFO] Monitoreo detenido manualmente.")
        finally:
            if hasattr(self.bd, 'disconnect') and self.bd_conectada: 
                self.bd.disconnect()

if __name__ == "__main__":
    procesador = ProcesadorTramasIndustriales(PUERTO_SERIAL, BAUDRATE, CADENA_CONEXION_BD)
    procesador.ejecutar_monitoreo()