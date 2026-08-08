import psycopg

conn = psycopg.connect(
    "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
)
cur = conn.cursor()
cur.execute('SELECT COUNT(*) FROM core."AspNetUsers"')
print("users", cur.fetchone()[0])
cur.execute(
    'SELECT "Email", "EmailConfirmed", "FirstName", "LastName" FROM core."AspNetUsers" ORDER BY "Id" DESC LIMIT 15'
)
for row in cur.fetchall():
    print(row)
cur.execute('SELECT "Id", "Name", "SchemaName" FROM core."Companies" ORDER BY "Id" DESC LIMIT 15')
print("--- companies ---")
for row in cur.fetchall():
    print(row)
cur.execute('SELECT COUNT(*) FROM profil_candidat."CandidateQuestions"')
print("candidate_questions", cur.fetchone()[0])
# company questions live per-tenant; sample one
cur.execute(
    """
    SELECT table_schema FROM information_schema.tables
    WHERE table_name='CompanyQuestions' AND table_schema LIKE 'co_%'
    LIMIT 1
    """
)
schema = cur.fetchone()[0]
cur.execute(f'SELECT COUNT(*) FROM "{schema}"."CompanyQuestions"')
print("company_questions_sample", schema, cur.fetchone()[0])
conn.close()
