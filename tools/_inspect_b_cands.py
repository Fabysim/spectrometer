"""Inspect B candidatures: which lack NiveauDeclare."""
import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
SCHEMA = "co_cabinet_horizon_conseil"
PROFILES = {
    4979: "C01", 4980: "C02", 4981: "C03", 4982: "C04", 4983: "C05",
    4984: "C06", 4985: "C07", 4986: "C08", 4987: "C09", 4988: "C10",
    4989: "C11", 4992: "C14", 4993: "C15",
}

with psycopg.connect(DB) as conn:
    cur = conn.cursor()
    cur.execute(
        f'''
        SELECT c."Id", c."PosteId", c."CandidateProfileId", p."Titre",
               (SELECT COUNT(*) FROM "{SCHEMA}"."EvaluationsCriteresCandidature" e
                WHERE e."CandidatureId"=c."Id" AND e."NiveauDeclare" IS NOT NULL) AS with_declare,
               (SELECT COUNT(*) FROM "{SCHEMA}"."EvaluationsCriteresCandidature" e
                WHERE e."CandidatureId"=c."Id") AS eval_rows
        FROM "{SCHEMA}"."Candidatures" c
        JOIN "{SCHEMA}"."Postes" p ON p."Id"=c."PosteId"
        WHERE c."CandidateProfileId" = ANY(%s)
        ORDER BY c."PosteId", c."CandidateProfileId"
        ''',
        (list(PROFILES.keys()),),
    )
    rows = cur.fetchall()
    print(f"candidatures B scenario: {len(rows)}")
    for r in rows:
        cid, poste, pid, titre, wd, er = r
        label = PROFILES.get(pid, "?")
        need = "REDO" if wd == 0 else "OK"
        print(f"  {need} {label} cand={cid} poste={poste} ({titre}) declare_rows={wd}/{er}")

    cur.execute(f'SELECT "Id","Titre" FROM "{SCHEMA}"."Postes" ORDER BY 1')
    print("postes", cur.fetchall())
    cur.execute(f'SELECT "PosteId", COUNT(*) FROM "{SCHEMA}"."CriteresEvaluation" GROUP BY 1')
    print("criteres", cur.fetchall())
