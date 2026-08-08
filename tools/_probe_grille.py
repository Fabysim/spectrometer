import json
from pathlib import Path
import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
state = json.loads(Path(r"C:\Users\Fabrice\source\repos\Spectrometre\Version modulaire\tools\scenario_recrutement_20260808\state.json").read_text(encoding="utf-8"))

with psycopg.connect(DB) as conn:
    cur = conn.cursor()
    cur.execute(
        """
        SELECT table_name FROM information_schema.tables
        WHERE table_schema='core' AND (table_name ILIKE '%module%' OR table_name ILIKE '%activ%')
        ORDER BY 1
        """
    )
    print("core module-ish", cur.fetchall())

    for key in ("A", "B"):
        schema = state["ids"][f"company_{key}"]["schema"]
        cur.execute(f'SELECT * FROM "{schema}"."CompanyCompatibilityCriteria" LIMIT 5')
        cols = [d.name for d in cur.description]
        rows = cur.fetchall()
        print(f"\n{key} CompanyCompatibilityCriteria cols={cols}")
        for r in rows:
            print(r)

    cur.execute(
        """
        SELECT column_name FROM information_schema.columns
        WHERE table_schema='profil_candidat' AND table_name='CandidateProfiles'
        """
    )
    # tags may be in separate table
    cur.execute(
        """
        SELECT table_name FROM information_schema.tables
        WHERE table_schema='profil_candidat' ORDER BY 1
        """
    )
    print("\nprofil_candidat tables", [r[0] for r in cur.fetchall()])

    # sample candidate 4979
    for t in ("CandidateCompatibilityCriteria", "CandidateProfiles", "CandidateAnswers"):
        cur.execute(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema='profil_candidat' AND table_name=%s ORDER BY ordinal_position
            """,
            (t,),
        )
        cols = [r[0] for r in cur.fetchall()]
        if cols:
            print(t, cols)
            if t != "CandidateAnswers":
                cur.execute(f'SELECT * FROM profil_candidat."{t}" WHERE "Id"=4979 OR "CandidateProfileId"=4979 LIMIT 3')
                try:
                    print(cur.fetchall())
                except Exception as e:
                    conn.rollback()
                    print("err", e)
