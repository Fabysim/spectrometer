"""Delete B scenario candidatures lacking NiveauDeclare (+ analyses / interview answers / index)."""
import json
from pathlib import Path
import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
SCHEMA = "co_cabinet_horizon_conseil"
COMPANY_ID = 3748
PROFILES = [4979, 4980, 4981, 4982, 4983, 4984, 4985, 4986, 4987, 4988, 4989, 4992, 4993]
OUT = Path(__file__).resolve().parents[1] / "tools" / "scenario_entrevues_20260808"
OUT.mkdir(parents=True, exist_ok=True)

with psycopg.connect(DB) as conn:
    cur = conn.cursor()
    cur.execute(
        f'''
        SELECT c."Id", c."PosteId", c."CandidateProfileId"
        FROM "{SCHEMA}"."Candidatures" c
        WHERE c."CandidateProfileId" = ANY(%s)
        ''',
        (PROFILES,),
    )
    cands = cur.fetchall()
    ids = [r[0] for r in cands]
    print("to_delete", len(ids), ids)

    if ids:
        # Analyses IA
        cur.execute(
            f'DELETE FROM "{SCHEMA}"."AnalysesIaPoste" WHERE "CandidatureId" = ANY(%s)',
            (ids,),
        )
        print("analyses deleted", cur.rowcount)

        # Evaluations (may cascade, but explicit)
        cur.execute(
            f'DELETE FROM "{SCHEMA}"."EvaluationsCriteresCandidature" WHERE "CandidatureId" = ANY(%s)',
            (ids,),
        )
        print("evals deleted", cur.rowcount)

        cur.execute(
            f'DELETE FROM "{SCHEMA}"."Candidatures" WHERE "Id" = ANY(%s)',
            (ids,),
        )
        print("candidatures deleted", cur.rowcount)

    # Interview answers for these profiles in tenant schema
    cur.execute(
        """
        SELECT table_name FROM information_schema.tables
        WHERE table_schema=%s AND table_name='InterviewAnswers'
        """,
        (SCHEMA,),
    )
    if cur.fetchone():
        cur.execute(
            f'DELETE FROM "{SCHEMA}"."InterviewAnswers" WHERE "CandidateProfileId" = ANY(%s)',
            (PROFILES,),
        )
        print("interview answers deleted", cur.rowcount)

    # Recruitment index (core or public?)
    cur.execute(
        """
        SELECT table_schema, table_name FROM information_schema.tables
        WHERE table_name ILIKE '%Recruitment%' OR table_name ILIKE '%CandidatureIndex%'
        ORDER BY 1,2
        """
    )
    print("index tables", cur.fetchall())
    for schema, table in [
        ("core", "RecruitmentIndexEntries"),
        ("public", "RecruitmentIndexEntries"),
        ("vivier", "RecruitmentIndexEntries"),
    ]:
        cur.execute(
            """
            SELECT 1 FROM information_schema.tables
            WHERE table_schema=%s AND table_name=%s
            """,
            (schema, table),
        )
        if cur.fetchone():
            cur.execute(
                f'DELETE FROM "{schema}"."{table}" WHERE "CompanyId"=%s AND "CandidateProfileId" = ANY(%s)',
                (COMPANY_ID, PROFILES),
            )
            print(f"deleted from {schema}.{table}", cur.rowcount)

    # Also try generic column discovery
    cur.execute(
        """
        SELECT table_schema, table_name FROM information_schema.columns
        WHERE column_name='CandidateProfileId' AND table_name ILIKE '%index%'
        """
    )
    print("index-ish", cur.fetchall())

    conn.commit()

# verify empty
with psycopg.connect(DB) as conn:
    cur = conn.cursor()
    cur.execute(
        f'''
        SELECT COUNT(*) FROM "{SCHEMA}"."Candidatures"
        WHERE "CandidateProfileId" = ANY(%s)
        ''',
        (PROFILES,),
    )
    print("remaining", cur.fetchone()[0])

(OUT / "delete_log.json").write_text(
    json.dumps({"deleted_candidature_ids": ids, "profiles": PROFILES}, indent=2),
    encoding="utf-8",
)
print("done")
