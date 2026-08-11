#CODIGO DE BD CON LECTURA JSON.  #CODIGO DE CONECCION A LA BD EHM YA FUNCIONANDO#  #CONEXION EN LA BD EHM
import pyodbc


class ConectorSQLServer:

    def __init__(self, connection_string, table_name="emhart.parametros"):
        self.connection_string = connection_string
        self.table_name = table_name
        self.conn = None
        self.cursor = None

    def connect(self):
        try:
            self.conn = pyodbc.connect(self.connection_string)
            self.cursor = self.conn.cursor()
            print("Conexión establecida con éxito a SQL Server.")
        except Exception as e:
            print(f"Error al establecer conexión con la base de datos: {e}")

    def obtener_id_maquina(self, codigo_celda):
        """Obtiene el Id primario (INT) de una celda/máquina a partir de su código (ej. 'CELDA-01')."""
        query = "SELECT Id FROM emhart.maquinas WHERE IdMaquina = ? AND Activo = 1;"
        try:
            if self.cursor is None:
                self.connect()

            self.cursor.execute(query, (codigo_celda,))
            row = self.cursor.fetchone()
            if row:
                return row[0]  # Retorna el Id entero
            else:
                print(
                    f"Advertencia: No se encontró la máquina '{codigo_celda}' activa en emhart.maquinas."
                )
                return None
        except Exception as e:
            print(f"Error al obtener ID de la máquina '{codigo_celda}': {e}")
            return None

    def obtener_tolerancias_activas(self, codigo_celda):
        """Consulta las tolerancias activas asociadas al ID entero de la máquina enviando su código."""
        query = """
            SELECT rt.salida, rt.parametro, rt.min_val, rt.max_val
            FROM emhart.referencia_tolerancia rt
            INNER JOIN emhart.maquinas m ON rt.id_maquina = m.Id
            WHERE m.IdMaquina = ? AND rt.estado = 1;
        """
        try:
            if self.cursor is None:
                self.connect()

            self.cursor.execute(query, (codigo_celda,))
            rows = self.cursor.fetchall()

            # Formatear tolerancias en diccionario para fácil validación
            tolerancias = {}
            for row in rows:
                salida, parametro, min_val, max_val = row
                if salida not in tolerancias:
                    tolerancias[salida] = {}
                tolerancias[salida][parametro] = (min_val, max_val)

            return tolerancias
        except Exception as e:
            print(f"Error al cargar tolerancias para '{codigo_celda}': {e}")
            return {}

    def insertar_parametros(
        self,
        Fecha,
        NumSol,
        VolArc,
        VolPri,
        Salida,
        Programa,
        Elevacion,
        Caida,
        Penetracion,
        Energia,
        Corriente,
        Tiempo,
        LonPer,
        Linea,
        Identificador_ID,  # Puede ser el Id (INT) o la cadena ('CELDA-01')
        estatus_calidad="OK",
        detalles_fallas=None,
    ):
        # Si nos pasan la cadena 'CELDA-01', resolvemos su ID entero primero
        if isinstance(Identificador_ID, str):
            id_num = self.obtener_id_maquina(Identificador_ID)
            if id_num is None:
                print(
                    f"Error: No se pudo insertar el registro N° {NumSol}. La celda '{Identificador_ID}' no existe."
                )
                return
            Identificador_ID = id_num

        query = f"""
            INSERT INTO {self.table_name} (
                Fecha, NumSol, VolArc, VolPri, Salida, Programa, 
                Elevacion, Caida, Penetracion, Energia, Corriente, 
                Tiempo, LonPer, Linea, Identificador_ID,
                estatus_calidad, detalles_fallas
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        """
        try:
            if self.cursor is None:
                print("Error: No hay una conexión activa a la base de datos.")
                return

            self.cursor.execute(
                query,
                (
                    Fecha,
                    NumSol,
                    VolArc,
                    VolPri,
                    Salida,
                    Programa,
                    Elevacion,
                    Caida,
                    Penetracion,
                    Energia,
                    Corriente,
                    Tiempo,
                    LonPer,
                    Linea,
                    Identificador_ID,  # Insertado estrictamente como INT
                    estatus_calidad,
                    detalles_fallas,
                ),
            )
            self.conn.commit()
            print(
                f"[{Linea}] Registro N° {NumSol} [{estatus_calidad}] guardado en SQL Server (ID Máquina: {Identificador_ID})."
            )
        except Exception as e:
            print(
                f"Error al insertar datos en la tabla {self.table_name}: {e}"
            )

    def disconnect(self):
        try:
            if self.cursor:
                self.cursor.close()
            if self.conn:
                self.conn.close()
            print("Conexión cerrada de forma segura.")
        except Exception as e:
            print(f"Error al cerrar la conexión: {e}")