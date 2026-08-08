from playwright.sync_api import sync_playwright

BASE = "http://localhost:5263"
PASS = "ScenarioE2E2026!"
OWNER = "scenario20260808.entreprise.b@test.local"

with sync_playwright() as p:
    b = p.chromium.launch(headless=True)
    page = b.new_page()
    page.goto(f"{BASE}/login?culture=fr")
    page.fill("#login-email", OWNER)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(2500)
    page.goto(f"{BASE}/entreprise/postes/2/candidats/14", wait_until="networkidle")
    page.wait_for_timeout(2000)
    text = page.locator("body").inner_text()
    print("url", page.url)
    print("has Niveau déclaré", "Niveau déclaré" in text)
    # count Moyen labels roughly
    print("Moyen count", text.count("Moyen"))
    print("dash-only declare?", 'title="' in page.content() and "NiveauDeclareIndisponible" in page.content())
    idx = text.find("Évaluation")
    print(text[idx:idx + 900] if idx >= 0 else text[:900])
    b.close()
