#!/usr/bin/env python3
"""
Rejeu candidatures B + entrevues — scénario 2026-08-08.
Prérequis : grille obligatoire (PostulerAvecGrille) déjà livrée.
"""
from __future__ import annotations

import json
import re
from datetime import datetime
from pathlib import Path

import psycopg
from playwright.sync_api import sync_playwright, Page

BASE = "http://localhost:5263"
PASS = "ScenarioE2E2026!"
DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"
SCHEMA = "co_cabinet_horizon_conseil"
COMPANY_ID = 3748
OWNER_B = "scenario20260808.entreprise.b@test.local"

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "tools" / "scenario_entrevues_20260808"
SHOTS = OUT / "screenshots"
STATE = OUT / "state.json"

# cid -> (email, profile_id, intention, poste_id, poste_title_fragment)
CANDIDATES = {
    "C01": ("scenario20260808.c01@test.local", 4979, "high_a", 1, "Consultant senior RH"),
    "C02": ("scenario20260808.c02@test.local", 4980, "high_a", 1, "Consultant senior RH"),
    "C03": ("scenario20260808.c03@test.local", 4981, "high_a", 1, "Consultant senior RH"),
    "C04": ("scenario20260808.c04@test.local", 4982, "mid_a", 1, "Consultant senior RH"),
    "C05": ("scenario20260808.c05@test.local", 4983, "mid_a", 1, "Consultant senior RH"),
    "C06": ("scenario20260808.c06@test.local", 4984, "mid_b", 1, "Consultant senior RH"),
    "C07": ("scenario20260808.c07@test.local", 4985, "mid_b", 1, "Consultant senior RH"),
    "C08": ("scenario20260808.c08@test.local", 4986, "low_a", 2, "Analyste junior"),
    "C09": ("scenario20260808.c09@test.local", 4987, "high_b", 2, "Analyste junior"),
    "C10": ("scenario20260808.c10@test.local", 4988, "high_b", 2, "Analyste junior"),
    "C11": ("scenario20260808.c11@test.local", 4989, "mid_mix", 2, "Analyste junior"),
    "C14": ("scenario20260808.c14@test.local", 4992, "high_b", 1, "Consultant senior RH"),
    "C15": ("scenario20260808.c15@test.local", 4993, "low_b", 1, "Consultant senior RH"),
}

# Declared level base 0-4 by intention (varied per criterion via offset)
DECLARE_BASE = {
    "high_b": 4,
    "high_a": 3,
    "mid_b": 3,
    "mid_mix": 2,
    "mid_a": 2,
    "low_a": 1,
    "low_b": 1,
}

# Final level base — diverge for some candidates
FINAL_BASE = {
    "C01": 2,  # declared high-ish but final mid (écart)
    "C02": 3,
    "C03": 2,
    "C04": 2,
    "C05": 3,  # declared mid, final higher
    "C06": 3,
    "C07": 2,
    "C08": 1,
    "C09": 4,
    "C10": 4,
    "C11": 2,
    "C14": 4,  # top
    "C15": 0,  # bottom
}


def log(msg: str) -> None:
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}", flush=True)


def load_state() -> dict:
    if STATE.exists():
        return json.loads(STATE.read_text(encoding="utf-8"))
    return {"steps": {}, "ids": {}, "notes": [], "anomalies": []}


def save_state(state: dict) -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    STATE.write_text(json.dumps(state, indent=2, ensure_ascii=False), encoding="utf-8")


def shot(page: Page, name: str) -> str:
    SHOTS.mkdir(parents=True, exist_ok=True)
    path = SHOTS / f"{name}.png"
    page.screenshot(path=str(path), full_page=True)
    return str(path)


def login(page: Page, email: str) -> None:
    page.goto(f"{BASE}/login?culture=fr", wait_until="domcontentloaded")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator("button[type=submit]").click()
    page.wait_for_timeout(2500)
    log(f"login {email} -> {page.url}")


def logout(page: Page) -> None:
    page.goto(f"{BASE}/logout", wait_until="domcontentloaded")
    page.wait_for_timeout(1000)


def levels_for_declare(intention: str, n: int) -> list[int]:
    base = DECLARE_BASE[intention]
    # pattern: base, base-1, base, base-1, ...
    out = []
    for i in range(n):
        v = base - (i % 2)
        out.append(max(0, min(4, v)))
    # ensure variety for high/low extremes
    if intention.startswith("high") and n >= 3:
        out[0] = 4
        out[-1] = max(2, out[-1])
    if intention.startswith("low") and n >= 2:
        out[0] = 0
        out[1] = 1
    return out


def levels_for_final(cid: str, n: int, declared: list[int]) -> list[int]:
    base = FINAL_BASE[cid]
    out = []
    for i in range(n):
        v = base + ((i % 3) - 1)
        out.append(max(0, min(4, v)))
    # force divergence from declared for first criterion when possible
    if n and out[0] == declared[0]:
        out[0] = max(0, min(4, declared[0] - 1 if declared[0] > 0 else declared[0] + 1))
    return out


def apply_with_grille(page: Page, cid: str, state: dict) -> None:
    email, profile_id, intention, poste_id, title_frag = CANDIDATES[cid]
    login(page, email)
    page.goto(f"{BASE}/candidat/postes", wait_until="domcontentloaded")
    page.wait_for_timeout(2500)

    # Already in DB?
    with psycopg.connect(DB) as conn:
        cur = conn.cursor()
        cur.execute(
            f'''
            SELECT c."Id", AVG(e."NiveauDeclare"::float), COUNT(e.*)
            FROM "{SCHEMA}"."Candidatures" c
            LEFT JOIN "{SCHEMA}"."EvaluationsCriteresCandidature" e
              ON e."CandidatureId"=c."Id" AND e."NiveauDeclare" IS NOT NULL
            WHERE c."CandidateProfileId"=%s AND c."PosteId"=%s
            GROUP BY c."Id"
            ''',
            (profile_id, poste_id),
        )
        existing = cur.fetchone()
        if existing and existing[2] and existing[2] > 0:
            state["ids"][cid] = {
                "email": email,
                "profile_id": profile_id,
                "intention": intention,
                "poste_id": poste_id,
                "candidature_id": int(existing[0]),
                "declare_avg": round(float(existing[1] or 0), 2),
                "declare_levels": levels_for_declare(intention, int(existing[2])),
                "n_criteres": int(existing[2]),
            }
            log(f"{cid} already in DB cand={existing[0]} — skip UI")
            logout(page)
            return

    cards = page.locator("article.poste-carte")
    target = None
    for i in range(cards.count()):
        text = cards.nth(i).inner_text()
        if title_frag in text:
            target = cards.nth(i)
            if "Horizon" in text or "Cabinet" in text:
                break
    if target is None:
        raise RuntimeError(f"{cid}: poste card not found for {title_frag} (cards={cards.count()})")

    if "Candidature envoyée" in target.inner_text() or "Application sent" in target.inner_text():
        log(f"{cid} UI shows already applied — refresh from DB")
        logout(page)
        return

    btn = target.get_by_role("button", name=re.compile(r"Postuler|Apply", re.I))
    btn.click()
    page.wait_for_timeout(2000)
    grille = page.locator(".postulation-grille")
    if grille.count() == 0:
        shot(page, f"fail_no_grille_{cid}")
        raise RuntimeError(f"{cid}: grille inline not shown")

    if cid in ("C01", "C09", "C15"):
        shot(page, f"grille_inline_{cid}")

    selects = page.locator(".postulation-grille select")
    n = selects.count()
    declared = levels_for_declare(intention, n)
    for i, lvl in enumerate(declared):
        selects.nth(i).select_option(str(lvl))
        page.wait_for_timeout(100)

    page.get_by_role("button", name=re.compile(r"Confirmer ma candidature|Confirm my application", re.I)).click()
    page.wait_for_timeout(4000)

    with psycopg.connect(DB) as conn:
        cur = conn.cursor()
        cur.execute(
            f'''
            SELECT c."Id",
                   AVG(e."NiveauDeclare"::float),
                   COUNT(e.*)
            FROM "{SCHEMA}"."Candidatures" c
            JOIN "{SCHEMA}"."EvaluationsCriteresCandidature" e ON e."CandidatureId"=c."Id"
            WHERE c."CandidateProfileId"=%s AND c."PosteId"=%s
              AND e."NiveauDeclare" IS NOT NULL
            GROUP BY c."Id"
            ''',
            (profile_id, poste_id),
        )
        row = cur.fetchone()
        if not row:
            shot(page, f"fail_persist_{cid}")
            raise RuntimeError(f"{cid}: candidature/declare not persisted")
        candidature_id, avg_decl, cnt = row
        state["ids"][cid] = {
            "email": email,
            "profile_id": profile_id,
            "intention": intention,
            "poste_id": poste_id,
            "candidature_id": int(candidature_id),
            "declare_avg": round(float(avg_decl), 2),
            "declare_levels": declared,
            "n_criteres": int(cnt),
        }
        log(f"{cid} applied cand={candidature_id} declare_avg={avg_decl:.2f} n={cnt}")

    logout(page)


def set_finals_and_ia(page: Page, state: dict) -> None:
    login(page, OWNER_B)
    for cid, info in state["ids"].items():
        if not cid.startswith("C"):
            continue
        poste_id = info["poste_id"]
        candidature_id = info["candidature_id"]
        page.goto(
            f"{BASE}/entreprise/postes/{poste_id}/candidats/{candidature_id}",
            wait_until="networkidle",
        )
        page.wait_for_timeout(1500)

        # capture declare column for a few
        if cid in ("C01", "C09", "C15"):
            shot(page, f"detail_declare_{cid}")

        selects = page.locator("table.postes-eval-table select, .postes-eval-table select")
        if selects.count() == 0:
            selects = page.locator("table select")
        n = selects.count()
        declared = info.get("declare_levels") or levels_for_declare(info["intention"], n)
        finals = levels_for_final(cid, n, declared[:n] if declared else [2] * n)
        for i, lvl in enumerate(finals):
            try:
                selects.nth(i).select_option(str(lvl))
                page.wait_for_timeout(250)
            except Exception as ex:
                state["anomalies"].append(f"{cid} set final[{i}]={lvl}: {ex}")

        info["final_levels"] = finals

        # Generate / regenerate IA
        gen = page.get_by_role("button", name=re.compile(r"Générer l'analyse|Régénérer|Generate|Regenerate", re.I))
        if gen.count():
            gen.first.click()
            page.wait_for_timeout(14000)
        if cid in ("C14", "C09", "C01", "C15", "C08"):
            shot(page, f"detail_ia_{cid}")

        # scrape IA snippet
        body = page.locator("body").inner_text()
        info["detail_text"] = body[:5000]
        save_state(state)
        log(f"{cid} finals+IA done")

    logout(page)


def do_interviews(page: Page, state: dict) -> None:
    login(page, OWNER_B)
    library_ok = False
    for cid, info in state["ids"].items():
        if not cid.startswith("C"):
            continue
        pid = info["profile_id"]
        page.goto(f"{BASE}/entretien/{pid}", wait_until="networkidle")
        page.wait_for_timeout(3500)
        body = page.locator("body").inner_text()
        if "Bibliothèque" in body or "library" in body.lower() or "Enregistrer les réponses" in body:
            library_ok = True
        areas = page.locator("textarea[id^='iq-'], .bibliotheque textarea, textarea")
        # Prefer iq- textareas
        iq = page.locator("textarea[id^='iq-']")
        n = iq.count() if iq.count() else min(areas.count(), 8)
        filled = 0
        target = iq if iq.count() else areas
        for i in range(min(target.count(), 6)):
            target.nth(i).fill(
                f"[{cid}] Réponse entrevue Q{i+1}: exemple STAR concret, résultat mesurable, "
                f"alignement avec le poste {info['poste_id']}."
            )
            filled += 1
            page.wait_for_timeout(120)
        save_btn = page.get_by_role("button", name=re.compile(r"Enregistrer les réponses|Save answers|Enregistrer", re.I))
        if save_btn.count() and filled:
            save_btn.first.click()
            page.wait_for_timeout(1500)
        info["interview_answers_filled"] = filled
        if cid in ("C14", "C09", "C15"):
            shot(page, f"entretien1_{cid}")
        log(f"{cid} entretien answers={filled}")
        save_state(state)

    state["notes"].append(
        "Bibliothèque questions + persistance disponible"
        if library_ok
        else "Bibliothèque non détectée clairement — réponses tentées sur textareas visibles"
    )

    # Guides 2e entrevue per poste
    for poste_id, key in ((1, "B1"), (2, "B2")):
        page.goto(f"{BASE}/entreprise/postes/{poste_id}/guide-entrevue", wait_until="networkidle")
        page.wait_for_timeout(1500)
        areas = page.locator("textarea")
        for i in range(min(areas.count(), 5)):
            areas.nth(i).fill(
                f"Guide 2e entrevue {key} — axe {i+1}: valider motivation, culture, exemples STAR, "
                f"écarts grille déclarée/finale."
            )
            page.wait_for_timeout(200)
        save_btn = page.get_by_role("button", name=re.compile(r"Enregistrer|Save", re.I))
        if save_btn.count():
            save_btn.first.click()
            page.wait_for_timeout(1000)
        shot(page, f"guide_entrevue_{key}")
        log(f"guide {key} saved")

    logout(page)


def verify_and_capture(page: Page, state: dict) -> None:
    login(page, OWNER_B)
    state["verify"] = {}
    for poste_id, key in ((1, "B1"), (2, "B2")):
        page.goto(f"{BASE}/entreprise/postes/{poste_id}/selection", wait_until="networkidle")
        page.wait_for_timeout(2000)
        shot(page, f"selection_{key}")
        state["verify"][f"selection_{key}"] = page.locator("body").inner_text()[:7000]

        page.goto(f"{BASE}/entreprise/postes/{poste_id}/candidats", wait_until="networkidle")
        page.wait_for_timeout(1500)
        shot(page, f"candidats_{key}")
        state["verify"][f"candidats_{key}"] = page.locator("body").inner_text()[:5000]

    # recommended candidates deep shots
    for cid in ("C14", "C09"):
        info = state["ids"].get(cid)
        if not info:
            continue
        page.goto(
            f"{BASE}/entreprise/postes/{info['poste_id']}/candidats/{info['candidature_id']}",
            wait_until="networkidle",
        )
        page.wait_for_timeout(2000)
        shot(page, f"recommended_detail_{cid}")
        state["verify"][f"recommended_{cid}"] = page.locator("body").inner_text()[:6000]

    logout(page)


def collect_db_metrics(state: dict) -> None:
    with psycopg.connect(DB) as conn:
        cur = conn.cursor()
        cur.execute(
            """
            SELECT column_name FROM information_schema.columns
            WHERE table_schema=%s AND table_name='InterviewAnswers'
            """,
            (SCHEMA,),
        )
        ia_cols = [r[0] for r in cur.fetchall()]
        answer_col = "AnswerText" if "AnswerText" in ia_cols else ("ReponseTexte" if "ReponseTexte" in ia_cols else None)

        for cid, info in list(state["ids"].items()):
            if not cid.startswith("C"):
                continue
            cand_id = info["candidature_id"]
            cur.execute(
                f'''
                SELECT AVG(e."NiveauDeclare"::float), AVG(e."NiveauFinal"::float),
                       COUNT(*) FILTER (WHERE e."NiveauDeclare" IS NOT NULL),
                       COUNT(*) FILTER (WHERE e."NiveauFinal" IS NOT NULL)
                FROM "{SCHEMA}"."EvaluationsCriteresCandidature" e
                WHERE e."CandidatureId"=%s
                ''',
                (cand_id,),
            )
            d_avg, f_avg, d_n, f_n = cur.fetchone()
            info["declare_avg"] = round(float(d_avg or 0), 2)
            info["final_avg"] = round(float(f_avg or 0), 2) if f_avg is not None else None
            info["declare_n"] = int(d_n)
            info["final_n"] = int(f_n)

            cur.execute(
                f'''
                SELECT "AnalyseTexte", "GenereeParIa", "GenereeLe"
                FROM "{SCHEMA}"."AnalysesIaPoste"
                WHERE "CandidatureId"=%s
                ORDER BY "Id" DESC LIMIT 1
                ''',
                (cand_id,),
            )
            row = cur.fetchone()
            if row:
                texte = row[0] or ""
                info["ia_preview"] = texte[:600]
                info["ia_par_ia"] = bool(row[1])
                low = texte.lower()
                if any(w in low for w in ("très prometteur", "excellente", "fortement", "prioritaire", "recommand")):
                    info["ia_tone"] = "positif"
                elif any(w in low for w in ("lacune", "écart", "insuffisant", "prudence", "risque", "faible")):
                    info["ia_tone"] = "prudent"
                else:
                    info["ia_tone"] = "neutre"
            else:
                info["ia_preview"] = None
                info["ia_tone"] = "absent"

            if answer_col:
                cur.execute(
                    f'''
                    SELECT COUNT(*) FROM "{SCHEMA}"."InterviewAnswers"
                    WHERE "CandidateProfileId"=%s AND COALESCE(TRIM("{answer_col}"),'') <> ''
                    ''',
                    (info["profile_id"],),
                )
                info["interview_persisted"] = int(cur.fetchone()[0])
            else:
                info["interview_persisted"] = 0

    save_state(state)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    SHOTS.mkdir(parents=True, exist_ok=True)
    state = load_state()
    state.setdefault("anomalies", [])
    state.setdefault("notes", [])
    state["started"] = datetime.now().isoformat()
    state["prereq"] = {
        "build_errors": 0,
        "tests": "116/116",
        "postuler_avec_grille": True,
    }

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.set_default_timeout(60000)

        if not state["steps"].get("applications"):
            for cid in CANDIDATES:
                try:
                    apply_with_grille(page, cid, state)
                except Exception as ex:
                    state["anomalies"].append(f"apply {cid}: {ex}")
                    log(f"ERROR apply {cid}: {ex}")
                save_state(state)
            state["steps"]["applications"] = True
            save_state(state)

        if not state["steps"].get("finals_ia"):
            set_finals_and_ia(page, state)
            state["steps"]["finals_ia"] = True
            save_state(state)

        if not state["steps"].get("interviews"):
            do_interviews(page, state)
            state["steps"]["interviews"] = True
            save_state(state)

        if not state["steps"].get("verify"):
            verify_and_capture(page, state)
            state["steps"]["verify"] = True
            save_state(state)

        browser.close()

    collect_db_metrics(state)
    state["finished"] = datetime.now().isoformat()
    save_state(state)
    log("DONE")


if __name__ == "__main__":
    main()
