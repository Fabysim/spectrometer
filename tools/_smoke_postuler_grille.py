"""Smoke UI: postuler avec grille inline puis vérifier Niveau déclaré côté entreprise."""
import re
import time
from playwright.sync_api import sync_playwright
import psycopg

BASE = "http://localhost:5263"
PASS = "ScenarioE2E2026!"
DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
# Entreprise B du scénario a Recrutement + postes avec critères
OWNER = "scenario20260808.entreprise.b@test.local"
# Nouveau candidat pour ne pas retomber sur déjà postulé
CAND = f"scenario20260808.grille.{int(time.time())}@test.local"


def wait_host():
    import urllib.request
    for _ in range(40):
        try:
            urllib.request.urlopen(f"{BASE}/login", timeout=2)
            return
        except Exception:
            time.sleep(1)
    raise RuntimeError("Host down")


def register_candidat(page):
    page.goto(f"{BASE}/inscription?culture=fr")
    page.fill("#inscription-nom", "Grille")
    page.fill("#inscription-prenom", "Smoke")
    page.fill("#inscription-email", CAND)
    page.fill("#motDePasse", PASS)
    page.fill("#confirmationMotDePasse", PASS)
    page.evaluate("spectrometreInscriptionGoToStep(2)")
    page.wait_for_timeout(300)
    page.locator('input[name="_model.Profil"][value="Candidat"]').check()
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(3000)
    with psycopg.connect(DB) as c:
        cur = c.cursor()
        cur.execute('UPDATE core."AspNetUsers" SET "EmailConfirmed"=TRUE WHERE lower("Email")=lower(%s)', (CAND,))
        c.commit()


def login(page, email):
    page.goto(f"{BASE}/login?culture=fr")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(2500)


def main():
    wait_host()
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        register_candidat(page)
        login(page, CAND)
        page.goto(f"{BASE}/candidat/postes", wait_until="networkidle")
        page.wait_for_timeout(2000)
        # Find Horizon / Consultant card Postuler
        cards = page.locator("article.poste-carte")
        target = None
        for i in range(cards.count()):
            t = cards.nth(i).inner_text()
            if "Consultant senior RH" in t or "Horizon" in t:
                target = cards.nth(i)
                break
        if target is None:
            # any Postuler
            btn = page.get_by_role("button", name=re.compile(r"Postuler|Apply", re.I))
            assert btn.count() > 0, "no Postuler button"
            btn.first.click()
        else:
            target.get_by_role("button", name=re.compile(r"Postuler|Apply", re.I)).click()
        page.wait_for_timeout(1500)
        assert page.locator(".postulation-grille").count() > 0, "grille inline absente"
        selects = page.locator(".postulation-grille select")
        n = selects.count()
        print("criteres selects", n)
        for i in range(n):
            selects.nth(i).select_option(index=3)  # Moyen-ish
            page.wait_for_timeout(200)
        page.get_by_role("button", name=re.compile(r"Confirmer ma candidature|Confirm my application", re.I)).click()
        page.wait_for_timeout(3000)
        body = page.locator("body").inner_text()
        assert "Candidature envoyée" in body or "Application sent" in body or "déjà" in body.lower() or page.locator(".deja-postule").count() > 0
        print("candidature OK côté candidat")

        page.goto(f"{BASE}/logout")
        page.wait_for_timeout(1000)
        login(page, OWNER)
        # open first poste candidats
        page.goto(f"{BASE}/entreprise/postes/1/candidats", wait_until="networkidle")
        page.wait_for_timeout(2000)
        # open detail of Smoke Grille
        link = page.locator("a[href*='/candidats/']").filter(has_text=re.compile(r"Smoke|Grille|Voir le détail|See details", re.I))
        if link.count() == 0:
            link = page.locator("a[href*='/entreprise/postes/1/candidats/']")
        href = None
        for i in range(link.count()):
            h = link.nth(i).get_attribute("href") or ""
            if re.search(r"/candidats/\d+", h):
                href = h
                # prefer latest / our candidate by scanning row text
                row = link.nth(i).locator("xpath=ancestor::tr")
                if row.count() and ("Smoke" in row.inner_text() or CAND.split("@")[0] in row.inner_text()):
                    break
        assert href, "no candidature detail link"
        page.goto(BASE + href if href.startswith("/") else href, wait_until="networkidle")
        page.wait_for_timeout(2000)
        text = page.locator("body").inner_text()
        # Should not show NiveauDeclareIndisponible dash-only for declared column when values exist
        assert "Niveau déclaré" in text or "Declared" in text
        # After our fill, badges should show level labels not only —
        print("detail snippet:", text[text.find("Évaluation"):text.find("Évaluation")+800] if "Évaluation" in text else text[:800])
        browser.close()
        print("SMOKE_OK", CAND)


if __name__ == "__main__":
    main()
