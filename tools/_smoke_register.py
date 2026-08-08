from playwright.sync_api import sync_playwright
import psycopg

BASE = "http://localhost:5263"
email = "scenario20260808.smoke@test.local"
PASS = "ScenarioE2E2026!"
DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"

with sync_playwright() as p:
    b = p.chromium.launch(headless=True)
    page = b.new_page()
    page.goto(f"{BASE}/inscription?culture=fr")
    page.fill("#inscription-nom", "Smoke")
    page.fill("#inscription-prenom", "Test")
    page.fill("#inscription-email", email)
    page.fill("#motDePasse", PASS)
    page.fill("#confirmationMotDePasse", PASS)
    page.evaluate("spectrometreInscriptionGoToStep(2)")
    page.wait_for_timeout(400)
    page.locator('input[name="_model.Profil"][value="Entreprise"]').check()
    page.wait_for_timeout(300)
    page.fill("#inscription-entreprise", "Smoke Co Scenario")
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(5000)
    print("after register url", page.url)
    content = page.content().lower()
    print("verification?", "vérification" in content or "verification" in content)
    print("error alert?", "alert-danger" in content)
    with psycopg.connect(DB) as c:
        cur = c.cursor()
        cur.execute(
            'SELECT "Email","EmailConfirmed" FROM core."AspNetUsers" WHERE lower("Email")=lower(%s)',
            (email,),
        )
        print("user", cur.fetchone())
        cur.execute(
            'UPDATE core."AspNetUsers" SET "EmailConfirmed"=TRUE WHERE lower("Email")=lower(%s)',
            (email,),
        )
        c.commit()
    page.goto(f"{BASE}/login?culture=fr")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(4000)
    print("after login", page.url)
    page.goto(f"{BASE}/entreprise/profil")
    page.wait_for_timeout(2500)
    print("profil", page.url, "title", page.title())
    print("tabs", page.locator("nav.entreprise-tabs button").count())
    b.close()
