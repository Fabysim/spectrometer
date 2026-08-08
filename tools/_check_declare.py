import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
with psycopg.connect(DB) as c:
    cur = c.cursor()
    cur.execute(
        """
        SELECT u."Email", cp."Id"
        FROM core."AspNetUsers" u
        JOIN profil_candidat."CandidateProfiles" cp ON cp."UserId"=u."Id"
        WHERE u."Email" ILIKE 'scenario20260808.grille.%'
        ORDER BY cp."Id" DESC LIMIT 1
        """
    )
    print("user", cur.fetchone())
    cur.execute(
        'SELECT "Id","PosteId","CandidateProfileId" FROM co_cabinet_horizon_conseil."Candidatures" ORDER BY "Id" DESC LIMIT 5'
    )
    print("cands", cur.fetchall())
    cur.execute(
        """
        SELECT "CandidatureId","CritereId","NiveauDeclare","NiveauFinal"
        FROM co_cabinet_horizon_conseil."EvaluationsCriteresCandidature"
        ORDER BY "Id" DESC LIMIT 12
        """
    )
    print("evals", cur.fetchall())
