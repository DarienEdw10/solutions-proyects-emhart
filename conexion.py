import pyodbc
 
class ConectorSQLServer:
 
    def __init__(self, connection_string, table_name="parametros"):
        self.connection_string = connection_string
        self.table_name = table_name
        self.conn = None
        self.cursor = None
 
    def connect(self):
        try:
            self.conn = pyodbc.connect(self.connection_string)
            self.cursor = self.conn.cursor()
            print("Conexion establecida.")
        except Exception as e:
            print(f"Error al establecer conexión con la base de datos: {e}")
 
    def insertar_parametros(self, Fecha, NumSol, VolArc, VolPri, Salida, Programa, Elevacion,
                            Caida, Penetracion, Energia, Corriente, Tiempo, LonPer, Linea, Identificador_ID):
        query = f"""
            INSERT INTO {self.table_name} (
                Fecha, NumSol, VolArc, VolPri, Salida, Programa,
                Elevacion, Caida, Penetracion, Energia, Corriente,
                Tiempo, LonPer, Linea, Identificador_ID
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
        """
        try:
            if self.cursor is None:
                print("Error: No hay una conexión activa a la base de datos.")
                return
            
            self.cursor.execute(query, (
                Fecha, NumSol, VolArc, VolPri, Salida, Programa,
                Elevacion, Caida, Penetracion, Energia, Corriente,
                Tiempo, LonPer, Linea, Identificador_ID
            ))
            self.conn.commit()
            print(f"[{Linea}] Registro N° {NumSol} guardado en SQL Server.")
        except Exception as e:
            print(f"Error al insertar datos en la tabla {self.table_name}: {e}")
 
    def disconnect(self):
        try:
            if self.cursor:
                self.cursor.close()
            if self.conn:
                self.conn.close()
            print("Conexión cerrada de forma segura.")
        except Exception as e:
            print(f"Error al cerrar la conexión: {e}")