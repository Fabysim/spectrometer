import psycopg
conn = psycopg.connect("host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025")
cur = conn.cursor()
for t in ("UserCompanyLinks", "Companies", "AspNetUsers"):
    cur.execute(
        "SELECT column_name FROM information_schema.columns WHERE table_schema='core' AND table_name=%s ORDER BY ordinal_position",
        (t,),
    )
    print(t, [r[0] for r in cur.fetchall()])
cur.execute(
    "SELECT column_name FROM information_schema.columns WHERE table_schema='profil_candidat' AND table_name='CandidateProfiles' ORDER BY ordinal_position"
)
print("CandidateProfiles", [r[0] for r in cur.fetchall()])
# sample postes columns in a schema
cur.execute(
    """
    SELECT column_name FROM information_schema.columns
    WHERE table_schema='co_atelier_numerique_dubois' AND table_name='Postes'
    ORDER BY ordinal_position
    """
)
print("Postes", [r[0] for r in cur.fetchall()])
cur.execute(
    """
    SELECT column_name FROM information_schema.columns
    WHERE table_schema='co_atelier_numerique_dubois' AND table_name='Candidatures'
    ORDER BY ordinal_position
    """
)
print("Candidatures", [r[0] for r in cur.fetchall()])
conn.close()
