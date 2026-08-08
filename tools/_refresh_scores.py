"""Refresh B vivier/selection + A vivier after grille fix; capture scores."""
import json
import re
from pathlib import Path
from playwright.sync_api import sync_playwright

ROOT = Path(__file__).resolve().parents[1]
STATE = json.loads((ROOT / "tools/scenario_recrutement_20260808/state.json").read_text(encoding="utf-8"))
SHOTS = ROOT / "tools/scenario_recrutement_20260808/screenshots"
BASE = "http://localhost:5263"
PASS = STATE["password"]


def login(page, email):
    page.goto(f"{BASE}/login?culture=fr")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(2500)


def main():
    updates = {}
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.set_default_timeout(45000)

        # Company A vivier
        login(page, STATE["ids"]["company_A"]["email"])
        page.goto(f"{BASE}/vivier", wait_until="networkidle")
        page.wait_for_timeout(2500)
        page.screenshot(path=str(SHOTS / "a_vivier_apres_grilles.png"), full_page=True)
        updates["a_vivier"] = page.locator("body").inner_text()[:5000]
        page.goto(f"{BASE}/logout")
        page.wait_for_timeout(800)

        # Company B
        login(page, STATE["ids"]["company_B"]["email"])
        page.goto(f"{BASE}/vivier", wait_until="networkidle")
        page.wait_for_timeout(2500)
        page.screenshot(path=str(SHOTS / "b_vivier_apres_grilles.png"), full_page=True)
        updates["b_vivier"] = page.locator("body").inner_text()[:6000]

        for key in ("B1", "B2"):
            pid = STATE["ids"][f"poste_{key}"]
            page.goto(f"{BASE}/entreprise/postes/{pid}/selection", wait_until="networkidle")
            page.wait_for_timeout(2000)
            page.screenshot(path=str(SHOTS / f"b_selection_{key}_apres_grilles.png"), full_page=True)
            updates[f"b_selection_{key}"] = page.locator("body").inner_text()[:6000]

        # deep C01 detail again for score
        info = STATE["ids"]["b_processing"]["C01"]
        page.goto(
            f"{BASE}/entreprise/postes/{info['poste_id']}/candidats/{info['candidature_id']}",
            wait_until="networkidle",
        )
        page.wait_for_timeout(2000)
        page.screenshot(path=str(SHOTS / "b_deep_detail_C01_apres_grilles.png"), full_page=True)
        updates["b_deep_C01"] = page.locator("body").inner_text()[:3500]

        page.goto(f"{BASE}/logout")
        browser.close()

    path = ROOT / "tools/scenario_recrutement_20260808/verify_refresh.json"
    path.write_text(json.dumps(updates, indent=2, ensure_ascii=False), encoding="utf-8")
    print("wrote", path)
    for k, v in updates.items():
        # extract score-like snippets
        scores = re.findall(r"(\d+%\s*|Score non calculé|Compatibilité\s*\n?\s*[—\d%]+)", v)
        print(k, "score hits", scores[:20])


if __name__ == "__main__":
    main()
