from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
import pyodbc
import json
import os

app = FastAPI()

# Permitir que las páginas HTML lean la API local sin bloqueos de seguridad
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

def obtener_conexion():
    ruta_config = os.path.join(os.path.dirname(__file__), "config.json")
    with open(ruta_config, "r", encoding="utf-8") as f:
        config = json.load(f)
    return pyodbc.connect(config["cadena_conexion_bd"])

@app.get("/api/parametros")
def get_parametros():
    try:
        conn = obtener_conexion()
        cursor = conn.cursor()
        
        # Consulta a la base de datos SQL Server
        query = """
        SELECT TOP 50 
            p.fecha, m.id_maquina as celda, p.salida, p.programa, 
            p.num_sol, p.corriente, p.energia, p.tiempo, p.penetracion,
            p.vol_arc, p.vol_pri, p.elevacion, p.caida, p.lon_per,
            p.estatus_calidad, p.detalles_fallas
        FROM emhart.parametros p
        INNER JOIN emhart.maquinas m ON p.Identificador_ID = m.id
        ORDER BY p.id DESC
        """
        cursor.execute(query)
        rows = cursor.fetchall()
        
        resultado = []
        for r in rows:
            resultado.append({
                "fecha": str(r.fecha),
                "celda": r.celda,
                "salida": f"Out {r.salida}",
                "programa": r.programa,
                "numSol": r.num_sol,
                "corriente": r.corriente,
                "energia": r.energia,
                "tiempo": r.tiempo,
                "penetracion": r.penetracion,
                "volArc": r.vol_arc,
                "volPri": r.vol_pri,
                "elevacion": r.elevacion,
                "caida": r.caida,
                "lonPer": r.lon_per,
                "estatus": r.estatus_calidad,
                "detalles": r.detalles_fallas,
                "turno": "T1"
            })
        
        conn.close()
        return resultado
    except Exception as e:
        return {"error": str(e)}