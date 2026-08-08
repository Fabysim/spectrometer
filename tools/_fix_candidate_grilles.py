"""Re-fill candidate Grille H properly (tags were not persisted during bulk run)."""
from __future__ import annotations

import json
import re
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
STATE = json.loads((ROOT / "tools/scenario_recrutement_20260808/state.json").read_text(encoding="utf-8"))
BASE = "http://localhost:5263"
PASS = STATE["password"]

# Import profile defs from main script by duplicating minimal map
PROFILES = {
    "high_a": {
        "tech": ["Informatique bureautique", "Outils numériques avancés", "Gestion de projet", "Langues étrangères"],
        "comp": ["Initiative", "Adaptabilité", "Communication", "Rigueur"],
        "cult": ["Innovation", "Autonomie", "Collaboration", "Excellence"],
        "mot": ["Apprentissage", "Progression", "Autonomie", "Responsabilités"],
        "vig": ["Rythme intense"],
    },
    "mid_a": {
        "tech": ["Informatique bureautique", "Gestion de projet", "Rédaction / communication"],
        "comp": ["Ponctualité", "Communication", "Coopération"],
        "cult": ["Collaboration", "Respect", "Autonomie"],
        "mot": ["Apprentissage", "Stabilité", "Salaire"],
        "vig": ["Consignes floues"],
    },
    "low_a": {
        "tech": ["Comptabilité", "Vente / négociation"],
        "comp": ["Ponctualité", "Sens des responsabilités"],
        "cult": ["Stabilité", "Respect"],
        "mot": ["Salaire", "Stabilité"],
        "vig": ["Bruit", "Isolement"],
    },
    "high_b": {
        "tech": ["Gestion de projet", "Gestion d'équipe", "Rédaction / communication", "Service client"],
        "comp": ["Sens des responsabilités", "Coopération", "Communication", "Gestion des conflits"],
        "cult": ["Respect", "Transparence", "Esprit d'équipe", "Excellence"],
        "mot": ["Reconnaissance", "Utilité sociale", "Stabilité", "Responsabilités"],
        "vig": ["Pression commerciale"],
    },
    "mid_b": {
        "tech": ["Gestion de projet", "Service client", "Langues étrangères"],
        "comp": ["Communication", "Adaptabilité", "Coopération"],
        "cult": ["Respect", "Collaboration", "Esprit d'équipe"],
        "mot": ["Reconnaissance", "Progression", "Apprentissage"],
        "vig": ["Horaires variables"],
    },
    "low_b": {
        "tech": ["Mécanique / électricité", "Informatique bureautique"],
        "comp": ["Initiative", "Adaptabilité"],
        "cult": ["Innovation", "Autonomie"],
        "mot": ["Apprentissage", "Autonomie"],
        "vig": ["Rythme intense", "Mobilité fréquente"],
    },
    "mid_mix": {
        "tech": ["Gestion de projet", "Outils numériques avancés", "Service client"],
        "comp": ["Communication", "Rigueur", "Coopération"],
        "cult": ["Collaboration", "Excellence", "Transparence"],
        "mot": ["Progression", "Responsabilités", "Apprentissage"],
        "vig": ["Manque de reconnaissance"],
    },
}

RYTHME = {
    "C01": 4, "C02": 4, "C03": 3, "C04": 3, "C05": 3,
    "C06": 3, "C07": 2, "C08": 2, "C09": 3, "C10": 3,
    "C11": 3, "C12": 4, "C13": 3, "C14": 3, "C15": 1,
}


def login(page, email):
    page.goto(f"{BASE}/login?culture=fr", wait_until="domcontentloaded")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(2500)


def goto_grille(page):
    page.goto(f"{BASE}/candidat/questionnaire", wait_until="networkidle")
    page.wait_for_timeout(1000)
    # Click progression item for grille
    item = page.locator(".progression-item", has_text=re.compile(r"Grille|compatib", re.I))
    if item.count():
        item.first.click()
        page.wait_for_timeout(800)
    # If still on questions, advance
    for _ in range(40):
        if page.locator("label.tag-checkbox").count() > 0:
            return True
        nxt = page.get_by_role("button", name=re.compile(r"Suivant|Next", re.I))
        if not nxt.count():
            break
        ta = page.locator("textarea.candidat-textarea")
        if ta.count():
            ta.first.fill("Réponse scénario E2E — complétion grille.")
        nxt.first.click()
        page.wait_for_timeout(350)
    return page.locator("label.tag-checkbox").count() > 0


def check_tag(page, text: str):
    loc = page.locator("label.tag-checkbox", has_text=text)
    if loc.count() == 0:
        print(f"  missing tag UI: {text}")
        return
    box = loc.first.locator("input[type=checkbox]")
    if box.is_checked():
        return
    # force change event for Blazor
    loc.first.click()
    page.wait_for_timeout(200)
    if not box.is_checked():
        box.check(force=True)
        page.wait_for_timeout(200)


def fill(page, cid: str):
    info = STATE["ids"][cid]
    email = info["email"]
    profile_key = info["profile_key"]
    tags = PROFILES[profile_key]
    rythme = RYTHME[cid]
    print(f"== {cid} {email} {profile_key} rythme={rythme}")
    login(page, email)
    ok = goto_grille(page)
    print("  grille visible", ok, "tags", page.locator("label.tag-checkbox").count())
    if not ok:
        page.screenshot(path=str(ROOT / f"tools/scenario_recrutement_20260808/screenshots/fix_grille_{cid}.png"))
        page.goto(f"{BASE}/logout")
        return False
    for group in ("tech", "comp", "cult", "mot", "vig"):
        for t in tags[group]:
            check_tag(page, t)
    # rythme
    selects = page.locator("select")
    set_ok = False
    for i in range(selects.count()):
        s = selects.nth(i)
        if s.locator(f'option[value="{rythme}"]').count():
            s.select_option(str(rythme))
            page.wait_for_timeout(400)
            # blur to ensure save
            s.evaluate("el => el.dispatchEvent(new Event('change', {bubbles:true}))")
            set_ok = True
            break
    print("  rythme set", set_ok)
    page.wait_for_timeout(800)
    page.screenshot(path=str(ROOT / f"tools/scenario_recrutement_20260808/screenshots/candidat_{cid}_grille_fixed.png"), full_page=True)
    page.goto(f"{BASE}/logout")
    page.wait_for_timeout(800)
    return True


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.set_default_timeout(45000)
        for cid in [f"C{i:02d}" for i in range(1, 16)]:
            fill(page, cid)
        browser.close()


if __name__ == "__main__":
    main()
