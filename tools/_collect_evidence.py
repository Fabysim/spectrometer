"""Collect DB evidence for RAPPORT_SCENARIO_RECRUTEMENT_20260808.md"""
import json
from pathlib import Path
import psycopg

ROOT = Path(__file__).resolve().parents[1]
STATE = ROOT / "tools" / "scenario_recrutement_20260808" / "state.json"
OUT = ROOT / "tools" / "scenario_recrutement_20260808" / "db_evidence.json"
DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"

state = json.loads(STATE.read_text(encoding="utf-8"))
evidence = {"modules": {}, "candidatures": {}, "scores": {}, "analyses": {}, "offers": {}, "evaluations": {}}

with psycopg.connect(DB) as conn:
    cur = conn.cursor()

    for key in ("A", "B"):
        cid = state["ids"][f"company_{key}"]["id"]
        schema = state["ids"][f"company_{key}"]["schema"]
        cur.execute(
            '''
            SELECT m."ModuleCode", m."IsActive"
            FROM core."CompanyModules" m
            WHERE m."CompanyId"=%s
            ORDER BY 1
            ''',
            (cid,),
        )
        # table name may differ
        try:
            evidence["modules"][key] = cur.fetchall()
        except Exception as e:
            conn.rollback()
            cur.execute(
                """
                SELECT table_name FROM information_schema.tables
                WHERE table_schema='core' AND table_name ILIKE '%module%'
                """
            )
            tables = [r[0] for r in cur.fetchall()]
            evidence["modules"][key] = {"error": str(e), "tables": tables}
            for t in tables:
                cur.execute(
                    f'SELECT * FROM core."{t}" WHERE "CompanyId"=%s LIMIT 20',
                    (cid,),
                )
                cols = [d.name for d in cur.description]
                evidence["modules"][f"{key}_{t}"] = {"cols": cols, "rows": [list(r) for r in cur.fetchall()]}

        # candidatures with candidate names
        cur.execute(
            f'''
            SELECT c."Id", c."PosteId", c."CandidateProfileId", c."Statut", c."EstPreselectionne",
                   p."Titre"
            FROM "{schema}"."Candidatures" c
            JOIN "{schema}"."Postes" p ON p."Id"=c."PosteId"
            ORDER BY c."PosteId", c."Id"
            '''
        )
        cands = cur.fetchall()
        evidence["candidatures"][key] = []
        for row in cands:
            cand_id, poste_id, profile_id, statut, prep, titre = row
            cur.execute(
                '''
                SELECT u."Email", u."FirstName", u."LastName"
                FROM profil_candidat."CandidateProfiles" cp
                JOIN core."AspNetUsers" u ON u."Id"=cp."UserId"
                WHERE cp."Id"=%s
                ''',
                (profile_id,),
            )
            u = cur.fetchone()
            evidence["candidatures"][key].append(
                {
                    "candidature_id": cand_id,
                    "poste_id": poste_id,
                    "poste_titre": titre,
                    "profile_id": profile_id,
                    "email": u[0] if u else None,
                    "name": f"{u[1]} {u[2]}" if u else None,
                    "statut": statut,
                    "preselect": prep,
                }
            )

        # CompatibilityResults columns + rows
        cur.execute(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='CompatibilityResults'
            ORDER BY ordinal_position
            """,
            (schema,),
        )
        cols = [r[0] for r in cur.fetchall()]
        evidence["scores"][f"{key}_cols"] = cols
        if cols:
            cur.execute(f'SELECT * FROM "{schema}"."CompatibilityResults"')
            evidence["scores"][key] = {
                "cols": [d.name for d in cur.description],
                "rows": [list(r) for r in cur.fetchall()],
            }

        # Analyses IA
        cur.execute(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='AnalysesIaPoste'
            ORDER BY ordinal_position
            """,
            (schema,),
        )
        acols = [r[0] for r in cur.fetchall()]
        evidence["analyses"][f"{key}_cols"] = acols
        if acols:
            cur.execute(f'SELECT * FROM "{schema}"."AnalysesIaPoste"')
            evidence["analyses"][key] = {
                "cols": [d.name for d in cur.description],
                "rows": [
                    [ (str(x)[:500] if isinstance(x, str) and len(x) > 500 else x) for x in r ]
                    for r in cur.fetchall()
                ],
            }

        # Evaluations
        cur.execute(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='EvaluationsCriteresCandidature'
            ORDER BY ordinal_position
            """,
            (schema,),
        )
        ecols = [r[0] for r in cur.fetchall()]
        if ecols:
            cur.execute(f'SELECT * FROM "{schema}"."EvaluationsCriteresCandidature"')
            evidence["evaluations"][key] = {
                "cols": [d.name for d in cur.description],
                "rows": [list(r) for r in cur.fetchall()],
            }

        # Offers
        cur.execute(
            f'''
            SELECT "Id", "Titre",
                   CASE WHEN "OffreTexte" IS NULL OR "OffreTexte"='' THEN FALSE ELSE TRUE END AS has_offre,
                   length(COALESCE("OffreTexte",'')),
                   "OffreGenereeParIa"
            FROM "{schema}"."Postes"
            ORDER BY "Id"
            '''
        )
        evidence["offers"][key] = [list(r) for r in cur.fetchall()]

        # Criteres count
        cur.execute(f'SELECT "PosteId", COUNT(*) FROM "{schema}"."CriteresEvaluation" GROUP BY 1 ORDER BY 1')
        evidence.setdefault("criteres", {})[key] = [list(r) for r in cur.fetchall()]

OUT.write_text(json.dumps(evidence, indent=2, ensure_ascii=False, default=str), encoding="utf-8")
print("wrote", OUT)
print(json.dumps({k: (len(v) if isinstance(v, (list, dict)) else v) for k, v in evidence.items()}, indent=2))
