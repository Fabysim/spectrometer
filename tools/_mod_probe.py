import json
from pathlib import Path
import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
state = json.loads(Path(r"C:\Users\Fabrice\source\repos\Spectrometre\Version modulaire\tools\scenario_recrutement_20260808\state.json").read_text(encoding="utf-8"))
out = {}

with psycopg.connect(DB) as conn:
    cur = conn.cursor()
    cur.execute(
        """
        SELECT column_name FROM information_schema.columns
        WHERE table_schema='core' AND table_name='ModuleActivations'
        ORDER BY ordinal_position
        """
    )
    print("ModuleActivations cols", [r[0] for r in cur.fetchall()])
    cur.execute('SELECT * FROM core."ModuleActivations" LIMIT 3')
    print("sample", [d.name for d in cur.description], cur.fetchall())

    for key in ("A", "B"):
        cid = state["ids"][f"company_{key}"]["id"]
        cur.execute('SELECT * FROM core."ModuleActivations" WHERE "SubjectId"=%s OR "CompanyId"=%s OR "TenantId"=%s', (cid, cid, cid))
