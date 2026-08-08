import json
from pathlib import Path
import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
state = json.loads(
    Path(r"C:\Users\Fabrice\source\repos\Spectrometre\Version modulaire\tools\scenario_recrutement_20260808\state.json").read_text(
        encoding="utf-8"
    )
)
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
    macols = [r[0] for r in cur.fetchall()]
    print("ModuleActivations cols", macols)
    cur.execute('SELECT * FROM core."ModuleActivations"')
    all_mods = cur.fetchall()
    mcols = [d.name for d in cur.description]
    print("all modules count", len(all_mods), "cols", mcols)
    # filter by company ids appearing in any int-like col
    for key in ("A", "B"):
        cid = state["ids"][f"company_{key}"]["id"]
        matched = []
        for row in all_mods:
            if cid in row:
                matched.append(list(row))
        out[f"modules_{key}"] = {"cols": mcols, "rows": matched}
        print(key, cid, matched)

    cur.execute(
        """
        SELECT "CandidateProfileId", "TechniqueTags", "ComportementaleTags", "CulturelleTags",
               "MotivationnelleTags", "PointsVigilanceTags", "RythmeTravail"
        FROM profil_candidat."CandidateCompatibilityCriteria"
        WHERE "CandidateProfileId" BETWEEN 4979 AND 4993
        ORDER BY 1
        """
    )
    out["candidate_grilles"] = [list(r) for r in cur.fetchall()]
    print("grilles", len(out["candidate_grilles"]))
    if out["candidate_grilles"]:
        print("C01", out["candidate_grilles"][0])
        print("C15", out["candidate_grilles"][-1])

    for key in ("A", "B"):
        schema = state["ids"][f"company_{key}"]["schema"]
        cur.execute(
            f"""
            SELECT "Id","Titre",
                   ("OffreTexte" IS NOT NULL AND length("OffreTexte")>0),
                   COALESCE(length("OffreTexte"),0), "OffreGenereeParIa"
            FROM "{schema}"."Postes" ORDER BY 1
            """
        )
        out[f"offers_{key}"] = [list(r) for r in cur.fetchall()]
        cur.execute(
            f'SELECT "PosteId", COUNT(*) FROM "{schema}"."Candidatures" GROUP BY 1 ORDER BY 1'
        )
        out[f"cand_counts_{key}"] = [list(r) for r in cur.fetchall()]
        cur.execute(
            f"""
            SELECT e."CandidatureId", ROUND(AVG(e."NiveauFinal"::numeric),2), COUNT(*)
            FROM "{schema}"."EvaluationsCriteresCandidature" e
            WHERE e."NiveauFinal" IS NOT NULL
            GROUP BY 1 ORDER BY 1
            """
        )
        out[f"eval_avg_{key}"] = [list(r) for r in cur.fetchall()]
        cur.execute(
            f"""
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='CompatibilityResults'
            """,
            (schema,),
        )
        ccols = [r[0] for r in cur.fetchall()]
        out[f"compat_cols_{key}"] = ccols
        if ccols:
            cur.execute(f'SELECT * FROM "{schema}"."CompatibilityResults"')
            out[f"compat_{key}"] = {
                "cols": [d.name for d in cur.description],
                "rows": [list(r) for r in cur.fetchall()],
            }
        cur.execute(
            f"""
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='AnalysesIaPoste'
            """,
            (schema,),
        )
        acols = [r[0] for r in cur.fetchall()]
        if acols:
            cur.execute(f'SELECT * FROM "{schema}"."AnalysesIaPoste"')
            rows = []
            for r in cur.fetchall():
                rows.append(
                    [
                        (x[:350] + "…") if isinstance(x, str) and len(x) > 350 else x
                        for x in r
                    ]
                )
            out[f"analyses_{key}"] = {
                "cols": [d.name for d in cur.description],
                "rows": rows,
            }

print("offers A", out["offers_A"])
print("offers B", out["offers_B"])
print("cand A", out["cand_counts_A"], "B", out["cand_counts_B"])
print("eval B count", len(out.get("eval_avg_B", [])))
print("analyses B", len(out.get("analyses_B", {}).get("rows", [])))

Path(
    r"C:\Users\Fabrice\source\repos\Spectrometre\Version modulaire\tools\scenario_recrutement_20260808\db_evidence.json"
).write_text(json.dumps(out, indent=2, ensure_ascii=False, default=str), encoding="utf-8")
