import psycopg

DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
profiles = [4979, 4980, 4981, 4982, 4983, 4984, 4985, 4986, 4987, 4988, 4989, 4992, 4993]
with psycopg.connect(DB) as c:
    cur = c.cursor()
    cur.execute(
        'DELETE FROM core."CandidatureIndexEntries" WHERE "CompanyId"=%s AND "CandidateProfileId" = ANY(%s)',
        (3748, profiles),
    )
    print("index deleted", cur.rowcount)
    c.commit()
