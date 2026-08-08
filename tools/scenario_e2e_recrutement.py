#!/usr/bin/env python3
"""
Scénario E2E Recrutement — Spectromètre Version modulaire (2026-08-08).
Inscription via formulaire SSR normal ; EmailConfirmed via SQL (Resend non réceptionnable en local) ;
reste via Playwright sur l'UI Blazor InteractiveServer.
"""
from __future__ import annotations

import json
import re
import time
from dataclasses import dataclass, asdict, field
from datetime import datetime
from pathlib import Path

import psycopg
from playwright.sync_api import sync_playwright, Page, expect

BASE = "http://localhost:5263"
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "tools" / "scenario_recrutement_20260808"
SHOTS = OUT / "screenshots"
STATE = OUT / "state.json"
PASS = "ScenarioE2E2026!"
DB = "host=localhost port=5432 dbname=spectrometre_v2 user=postgres password=Pil@tes2025"

# --- Matrice candidatures (arithmétique validée) ---
# Exclus A uniquement : C14, C15 (postulent à B)
# Exclus B uniquement : C12, C13 (postulent à A)
# Communs : C01–C11
# A1=8 (C01–C08), A2=5 (C09–C13)
# B1=9 (C01–C07,C14,C15), B2=4 (C08–C11)

CANDIDATES = [
    # id, prenom, nom, email_suffix, tech tags index pattern, rythme, alignement note
    ("C01", "Léa", "Dupont", "c01", "high_a", 4),
    ("C02", "Hugo", "Bernard", "c02", "high_a", 4),
    ("C03", "Chloé", "Petit", "c03", "high_a", 3),
    ("C04", "Nathan", "Robert", "c04", "mid_a", 3),
    ("C05", "Manon", "Richard", "c05", "mid_a", 3),
    ("C06", "Lucas", "Durand", "c06", "mid_b", 3),
    ("C07", "Emma", "Moreau", "c07", "mid_b", 2),
    ("C08", "Louis", "Simon", "c08", "low_a", 2),
    ("C09", "Jade", "Laurent", "c09", "high_b", 3),
    ("C10", "Gabriel", "Lefebvre", "c10", "high_b", 3),
    ("C11", "Inès", "Michel", "c11", "mid_mix", 3),
    ("C12", "Arthur", "Garcia", "c12", "high_a", 4),  # A only
    ("C13", "Lina", "David", "c13", "mid_a", 3),      # A only
    ("C14", "Adam", "Bertrand", "c14", "high_b", 3),   # B only
    ("C15", "Alice", "Roux", "c15", "low_b", 1),       # B only
]

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

COMPANY_A = {
    "email": "scenario20260808.entreprise.a@test.local",
    "name": "NovaTech Solutions",
    "owner_nom": "Tremblay",
    "owner_prenom": "Sophie",
    "tags": {
        "tech": ["Informatique bureautique", "Outils numériques avancés", "Gestion de projet", "Langues étrangères"],
        "comp": ["Initiative", "Adaptabilité", "Communication", "Rigueur"],
        "cult": ["Innovation", "Autonomie", "Collaboration", "Excellence"],
        "mot": ["Apprentissage", "Progression", "Autonomie", "Responsabilités"],
        "vig": ["Rythme intense", "Consignes floues"],
    },
    "rythme": 4,
    "answers": {
        # partial free-text for realism (theme labels vary; filled via first visible textareas)
        "blurb": "PME technologique liégeoise spécialisée en produits SaaS B2B. Culture produit, sprints courts, remote-friendly, excellence technique."
    },
}

COMPANY_B = {
    "email": "scenario20260808.entreprise.b@test.local",
    "name": "Cabinet Horizon Conseil",
    "owner_nom": "Lambert",
    "owner_prenom": "Marc",
    "tags": {
        "tech": ["Gestion de projet", "Gestion d'équipe", "Rédaction / communication", "Service client"],
        "comp": ["Sens des responsabilités", "Coopération", "Communication", "Gestion des conflits"],
        "cult": ["Respect", "Transparence", "Esprit d'équipe", "Excellence"],
        "mot": ["Reconnaissance", "Utilité sociale", "Stabilité", "Responsabilités"],
        "vig": ["Pression commerciale", "Horaires variables"],
    },
    "rythme": 3,
    "answers": {
        "blurb": "Cabinet de services professionnels RH et transformation organisationnelle. Relation client, méthodologie, confidentialité, rythme soutenu."
    },
}

POSTES = {
    "A1": {
        "titre": "Développeur full-stack .NET",
        "dept": "Produit",
        "desc": "Concevoir et livrer des fonctionnalités Blazor/.NET pour notre plateforme SaaS multi-tenant.",
        "taches": "Développer des features, revues de code, CI/CD, pair programming, support production.",
        "salaire": "48000-58000 EUR",
        "avantages": "Télétravail 3j/semaine, budget formation, Mutuelle+, matériel fourni",
        "company": "A",
        "criteres_manuels": [
            ("Technique", "C# / .NET", 4),
            ("Technique", "Blazor / WebAssembly", 3),
            ("Soft skills", "Autonomie et ownership", 4),
        ],
        "criteres_suggestions": True,
    },
    "A2": {
        "titre": "Product Owner agile",
        "dept": "Produit",
        "desc": "Porter le backlog produit, prioriser la valeur et synchroniser métier / ingénierie.",
        "taches": "Backlog, ateliers, roadmap trimestrielle, acceptance criteria, métriques d'adoption.",
        "salaire": "52000-62000 EUR",
        "avantages": "Télétravail hybride, tickets restaurant, conférences produit",
        "company": "A",
        "criteres_manuels": [
            ("Métier", "Gestion de backlog", 4),
            ("Métier", "Facilitation d'ateliers", 3),
            ("Technique", "Compréhension architecture SaaS", 2),
        ],
        "criteres_suggestions": False,
    },
    "B1": {
        "titre": "Consultant senior RH",
        "dept": "Conseil",
        "desc": "Accompagner des clients sur recrutement, culture et diagnostics organisationnels.",
        "taches": "Diagnostics, ateliers, rapports, coaching managers, réponses RFPs.",
        "salaire": "55000-70000 EUR",
        "avantages": "Voiture de fonction, télétravail 2j, intéressement",
        "company": "B",
        "criteres_manuels": [
            ("Métier", "Conseil RH / organisation", 4),
            ("Relation client", "Présentation et influence", 4),
            ("Méthode", "Conduite de projet", 3),
        ],
        "criteres_suggestions": True,
    },
    "B2": {
        "titre": "Analyste junior en transformation",
        "dept": "Conseil",
        "desc": "Soutenir les missions de conseil : collecte de données, synthèses, support ateliers.",
        "taches": "Interviews, analyses, slides, documentation process, support senior.",
        "salaire": "34000-40000 EUR",
        "avantages": "Mentorat, formation continue, tickets resto",
        "company": "B",
        "criteres_manuels": [
            ("Analyse", "Synthèse écrite", 3),
            ("Relation", "Écoute active", 3),
            ("Outils", "Pack Office avancé", 2),
        ],
        "criteres_suggestions": False,
    },
}

# Applications: candidate_id -> list of poste keys
APPLICATIONS = {
    "C01": ["A1", "B1"],
    "C02": ["A1", "B1"],
    "C03": ["A1", "B1"],
    "C04": ["A1", "B1"],
    "C05": ["A1", "B1"],
    "C06": ["A1", "B1"],
    "C07": ["A1", "B1"],
    "C08": ["A1", "B2"],
    "C09": ["A2", "B2"],
    "C10": ["A2", "B2"],
    "C11": ["A2", "B2"],
    "C12": ["A2"],
    "C13": ["A2"],
    "C14": ["B1"],
    "C15": ["B1"],
}

# NiveauFinal patterns for B applicants (0-4)
EVAL_LEVELS = {
    "C01": 4, "C02": 4, "C03": 3, "C04": 3, "C05": 2,
    "C06": 2, "C07": 2, "C08": 1, "C09": 4, "C10": 3,
    "C11": 2, "C14": 4, "C15": 1,
}


def log(msg: str) -> None:
    print(f"[{datetime.now().strftime('%H:%M:%S')}] {msg}", flush=True)


def db():
    return psycopg.connect(DB)


def confirm_email(email: str) -> None:
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute(
                'UPDATE core."AspNetUsers" SET "EmailConfirmed"=TRUE WHERE lower("Email")=lower(%s)',
                (email,),
            )
            conn.commit()
            if cur.rowcount == 0:
                raise RuntimeError(f"User not found for confirm: {email}")


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


def register_via_http(page: Page, *, nom, prenom, email, profil, nom_entreprise=None) -> None:
    """Inscription SSR via formulaire réel (pas SQL)."""
    page.goto(f"{BASE}/inscription?culture=fr", wait_until="domcontentloaded")
    page.fill("#inscription-nom", nom)
    page.fill("#inscription-prenom", prenom)
    page.fill("#inscription-email", email)
    page.fill("#motDePasse", PASS)
    page.fill("#confirmationMotDePasse", PASS)
    page.evaluate("typeof spectrometreInscriptionGoToStep==='function' && spectrometreInscriptionGoToStep(2)")
    page.wait_for_timeout(300)
    page.locator(f'input[name="_model.Profil"][value="{profil}"]').check()
    if profil == "Entreprise":
        page.wait_for_timeout(200)
        page.fill("#inscription-entreprise", nom_entreprise or "")
    page.locator('button[type="submit"]').click()
    page.wait_for_timeout(2500)
    # User may exist even if Resend fails
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "Id" FROM core."AspNetUsers" WHERE lower("Email")=lower(%s)', (email,))
            row = cur.fetchone()
    if not row:
        body = page.content()
        raise RuntimeError(f"Inscription failed for {email}. URL={page.url} snippet={body[:500]}")
    confirm_email(email)
    log(f"Registered+confirmed {email} ({profil})")


def login(page: Page, email: str) -> None:
    page.goto(f"{BASE}/login?culture=fr", wait_until="domcontentloaded")
    page.fill("#login-email", email)
    page.fill("#login-password", PASS)
    page.locator('button[type="submit"]').click()
    page.wait_for_url(re.compile(r".*/dashboard.*|/entreprise/.*|/candidat/.*"), timeout=30000)
    log(f"Logged in {email} -> {page.url}")


def logout(page: Page) -> None:
    page.goto(f"{BASE}/logout", wait_until="domcontentloaded")
    page.wait_for_timeout(1200)


def fill_company_grille(page: Page, company: dict) -> None:
    page.goto(f"{BASE}/entreprise/profil", wait_until="networkidle")
    page.wait_for_timeout(1500)
    # Fill some free-text on first theme
    textareas = page.locator("textarea")
    n = min(textareas.count(), 4)
    for i in range(n):
        textareas.nth(i).fill(company["answers"]["blurb"] + f" (réponse thème #{i+1})")
        page.wait_for_timeout(400)
    # Switch to Grille K tab
    tab = page.get_by_role("button", name=re.compile(r"Grille|compatibilité|Compatibility", re.I))
    if tab.count():
        tab.first.click()
    else:
        # last nav button often Grille
        page.locator("nav.entreprise-tabs button").last.click()
    page.wait_for_timeout(800)

    def check_tags(tags: list[str]):
        for t in tags:
            loc = page.locator("label.tag-checkbox", has_text=t)
            if loc.count() == 0:
                continue
            box = loc.first.locator("input[type=checkbox]")
            if not box.is_checked():
                loc.first.click()
                page.wait_for_timeout(250)

    tags = company["tags"]
    check_tags(tags["tech"])
    check_tags(tags["comp"])
    check_tags(tags["cult"])
    check_tags(tags["mot"])
    check_tags(tags["vig"])
    # rythme select
    sel = page.locator("select.entreprise-select")
    if sel.count():
        sel.first.select_option(str(company["rythme"]))
        page.wait_for_timeout(400)
    shot(page, f"entreprise_profil_{company['name'][:12].replace(' ', '_')}")


def create_poste(page: Page, key: str, poste: dict) -> int:
    page.goto(f"{BASE}/entreprise/postes", wait_until="networkidle")
    page.wait_for_timeout(1000)
    page.get_by_role("button", name=re.compile(r"Nouveau poste|New job", re.I)).click()
    page.wait_for_timeout(500)
    page.fill("#poste-titre", poste["titre"])
    if page.locator("#poste-dept").count():
        page.fill("#poste-dept", poste["dept"])
    page.fill("#poste-desc", poste["desc"])
    page.fill("#poste-taches", poste["taches"])
    page.fill("#poste-salaire", poste["salaire"])
    page.fill("#poste-avantages", poste["avantages"])
    page.get_by_role("button", name=re.compile(r"Créer|Create|Enregistrer|Save", re.I)).last.click()
    page.wait_for_timeout(2000)
    # Find poste id from link
    link = page.locator(f'a[href*="/entreprise/postes/"][href*="/profil"]', has_text=re.compile(re.escape(poste["titre"][:20])))
    if link.count() == 0:
        link = page.locator('a[href*="/entreprise/postes/"][href*="/profil"]').first
    href = link.get_attribute("href") or ""
    m = re.search(r"/entreprise/postes/(\d+)/profil", href)
    if not m:
        # try row click
        page.get_by_text(poste["titre"]).first.click()
        page.wait_for_timeout(1000)
        m = re.search(r"/entreprise/postes/(\d+)", page.url)
    if not m:
        raise RuntimeError(f"Cannot resolve poste id for {key}: {page.url}")
    poste_id = int(m.group(1))
    log(f"Created poste {key} id={poste_id}")
    return poste_id


def fill_poste_profil(page: Page, poste_id: int, poste: dict) -> None:
    page.goto(f"{BASE}/entreprise/postes/{poste_id}/profil", wait_until="networkidle")
    page.wait_for_timeout(1200)
    # Manual criteria
    manual_btn = page.get_by_role("button", name=re.compile(r"manuellement|manually|Ajouter un critère", re.I))
    if manual_btn.count():
        manual_btn.first.click()
        page.wait_for_timeout(400)
    for cat, lib, niv in poste["criteres_manuels"]:
        if page.locator("#critere-categorie").count() == 0:
            page.get_by_role("button", name=re.compile(r"manuellement|Ajouter", re.I)).first.click()
            page.wait_for_timeout(300)
        page.fill("#critere-categorie", cat)
        page.fill("#critere-libelle", lib)
        page.select_option("#critere-niveau", str(niv))
        page.get_by_role("button", name=re.compile(r"^Ajouter$|^Add$|^Enregistrer$|^Save$", re.I)).first.click()
        page.wait_for_timeout(1500)  # allow offre regen
    if poste.get("criteres_suggestions"):
        sug = page.get_by_role("button", name=re.compile(r"Suggestion|IA|générer", re.I))
        if sug.count():
            # try catalogue suggestions first
            for i in range(sug.count()):
                txt = sug.nth(i).inner_text()
                if "IA" in txt or "générer" in txt.lower() or "Generate" in txt:
                    sug.nth(i).click()
                    page.wait_for_timeout(8000)
                    break
            else:
                sug.first.click()
                page.wait_for_timeout(2000)
                # add selection if checkboxes
                boxes = page.locator("input[type=checkbox]")
                for i in range(min(3, boxes.count())):
                    if not boxes.nth(i).is_checked():
                        boxes.nth(i).check()
                add = page.get_by_role("button", name=re.compile(r"Ajouter la sélection|Add selection", re.I))
                if add.count():
                    add.first.click()
                    page.wait_for_timeout(2000)
    # capture offre if visible
    shot(page, f"poste_{poste_id}_profil")
    # check offre text in DB later


def fill_candidate_grille(page: Page, cid: str, profile_key: str, rythme: int) -> None:
    page.goto(f"{BASE}/candidat/questionnaire", wait_until="networkidle")
    page.wait_for_timeout(1200)
    # Answer a few questions quickly then jump to grille if possible
    for i in range(3):
        ta = page.locator("textarea.candidat-textarea")
        if ta.count() == 0:
            break
        ta.first.fill(f"[{cid}] Réponse scénario E2E question {i+1} — profil {profile_key}. Expérience concrète et motivations alignées.")
        nxt = page.get_by_role("button", name=re.compile(r"Suivant|Next", re.I))
        if nxt.count():
            nxt.first.click()
            page.wait_for_timeout(600)
    # Navigate progression to grille
    grille_item = page.locator(".progression-item", has_text=re.compile(r"Grille|compatibilité|Compatibility", re.I))
    if grille_item.count():
        grille_item.first.click()
        page.wait_for_timeout(800)
    else:
        # keep clicking next until grille
        for _ in range(30):
            if page.locator("label.tag-checkbox").count() > 0:
                break
            nxt = page.get_by_role("button", name=re.compile(r"Suivant|Next", re.I))
            if not nxt.count():
                break
            ta = page.locator("textarea.candidat-textarea")
            if ta.count():
                ta.first.fill(f"[{cid}] réponse auto {_}")
            nxt.first.click()
            page.wait_for_timeout(400)

    tags = PROFILES[profile_key]
    for group in ("tech", "comp", "cult", "mot", "vig"):
        for t in tags[group]:
            loc = page.locator("label.tag-checkbox", has_text=t)
            if loc.count() and not loc.first.locator("input").is_checked():
                loc.first.click()
                page.wait_for_timeout(200)
    sel = page.locator("select").filter(has=page.locator("option")).first
    # more specific: rythme select
    rythme_sel = page.locator("select.candidat-select, select.entreprise-select, .grille-champ select")
    if rythme_sel.count():
        rythme_sel.first.select_option(str(rythme))
    else:
        # any select with option value=rythme
        for i in range(page.locator("select").count()):
            s = page.locator("select").nth(i)
            opts = s.locator(f'option[value="{rythme}"]')
            if opts.count():
                s.select_option(str(rythme))
                break
    page.wait_for_timeout(500)
    shot(page, f"candidat_{cid}_grille")


def apply_to_poste(page: Page, company_id: int, poste_id: int) -> None:
    page.goto(f"{BASE}/candidat/postes/{company_id}/{poste_id}", wait_until="networkidle")
    page.wait_for_timeout(1000)
    btn = page.get_by_role("button", name=re.compile(r"Postuler|Apply", re.I))
    if btn.count() and btn.first.is_enabled():
        btn.first.click()
        page.wait_for_timeout(2000)
    shot(page, f"postuler_{company_id}_{poste_id}")


def activate_recrutement(page: Page) -> None:
    page.goto(f"{BASE}/entreprise/modules", wait_until="networkidle")
    page.wait_for_timeout(1000)
    card = page.locator("article.modules-card", has_text=re.compile(r"Recrutement", re.I))
    btn = card.get_by_role("button", name=re.compile(r"Activer|Activate", re.I))
    if btn.count():
        btn.first.click()
        page.wait_for_timeout(3000)
    shot(page, "entreprise_b_modules")


def company_ids_for_email(email: str) -> tuple[int, str]:
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute(
                '''
                SELECT c."Id", c."SchemaName"
                FROM core."Companies" c
                JOIN core."UserCompanyLinks" l ON l."CompanyId"=c."Id"
                JOIN core."AspNetUsers" u ON u."Id"=l."UserId"
                WHERE lower(u."Email")=lower(%s)
                ORDER BY c."Id" DESC LIMIT 1
                ''',
                (email,),
            )
            row = cur.fetchone()
            if not row:
                raise RuntimeError(f"No company for {email}")
            return int(row[0]), row[1]


def candidat_profile_id(email: str) -> int:
    with db() as conn:
        with conn.cursor() as cur:
            cur.execute(
                '''
                SELECT p."Id" FROM profil_candidat."CandidateProfiles" p
                JOIN core."AspNetUsers" u ON u."Id"=p."UserId"
                WHERE lower(u."Email")=lower(%s)
                ''',
                (email,),
            )
            row = cur.fetchone()
            if not row:
                # try SubjectId link
                cur.execute(
                    '''
                    SELECT table_schema FROM information_schema.columns
                    WHERE table_name='CandidateProfiles' AND column_name='UserId'
                    '''
                )
                raise RuntimeError(f"No candidate profile for {email}")
            return int(row[0])


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    SHOTS.mkdir(parents=True, exist_ok=True)
    state = load_state()
    state.setdefault("anomalies", [])
    state.setdefault("notes", [])
    state["password"] = PASS
    state["started"] = datetime.now().isoformat()

    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(locale="fr-FR", viewport={"width": 1440, "height": 900})
        page = context.new_page()
        page.set_default_timeout(45000)

        # --- Companies ---
        if not state["steps"].get("companies"):
            for key, co in (("A", COMPANY_A), ("B", COMPANY_B)):
                email = co["email"]
                with db() as conn:
                    with conn.cursor() as cur:
                        cur.execute('SELECT 1 FROM core."AspNetUsers" WHERE lower("Email")=lower(%s)', (email,))
                        exists = cur.fetchone() is not None
                if not exists:
                    register_via_http(
                        page,
                        nom=co["owner_nom"],
                        prenom=co["owner_prenom"],
                        email=email,
                        profil="Entreprise",
                        nom_entreprise=co["name"],
                    )
                else:
                    confirm_email(email)
                    log(f"Company {key} already exists")
                login(page, email)
                cid, schema = company_ids_for_email(email)
                state["ids"][f"company_{key}"] = {"id": cid, "schema": schema, "email": email, "name": co["name"]}
                fill_company_grille(page, co)
                if key == "B":
                    activate_recrutement(page)
                logout(page)
            state["steps"]["companies"] = True
            save_state(state)

        # --- Postes ---
        if not state["steps"].get("postes"):
            for key, poste in POSTES.items():
                co_key = poste["company"]
                email = COMPANY_A["email"] if co_key == "A" else COMPANY_B["email"]
                login(page, email)
                poste_id = create_poste(page, key, poste)
                fill_poste_profil(page, poste_id, poste)
                state["ids"][f"poste_{key}"] = poste_id
                # offre check
                schema = state["ids"][f"company_{co_key}"]["schema"]
                with db() as conn:
                    with conn.cursor() as cur:
                        cur.execute(
                            f'SELECT "OffreTexte", "OffreGenereeParIa" FROM "{schema}"."Postes" WHERE "Id"=%s',
                            (poste_id,),
                        )
                        row = cur.fetchone()
                        state["ids"][f"poste_{key}_offre"] = {
                            "has_text": bool(row and row[0]),
                            "len": len(row[0]) if row and row[0] else 0,
                            "par_ia": bool(row[1]) if row else False,
                            "preview": (row[0][:200] if row and row[0] else None),
                        }
                logout(page)
                save_state(state)
            state["steps"]["postes"] = True
            save_state(state)

        # --- Candidates ---
        if not state["steps"].get("candidates"):
            for cid, prenom, nom, suffix, profile_key, rythme in CANDIDATES:
                email = f"scenario20260808.{suffix}@test.local"
                with db() as conn:
                    with conn.cursor() as cur:
                        cur.execute('SELECT 1 FROM core."AspNetUsers" WHERE lower("Email")=lower(%s)', (email,))
                        exists = cur.fetchone() is not None
                if not exists:
                    register_via_http(page, nom=nom, prenom=prenom, email=email, profil="Candidat")
                else:
                    confirm_email(email)
                login(page, email)
                fill_candidate_grille(page, cid, profile_key, rythme)
                # resolve profile id
                with db() as conn:
                    with conn.cursor() as cur:
                        cur.execute(
                            '''
                            SELECT p."Id" FROM profil_candidat."CandidateProfiles" p
                            JOIN core."AspNetUsers" u ON u."Id"=p."UserId"
                            WHERE lower(u."Email")=lower(%s)
                            ''',
                            (email,),
                        )
                        row = cur.fetchone()
                        if not row:
                            # alternate column names
                            cur.execute(
                                '''
                                SELECT column_name FROM information_schema.columns
                                WHERE table_schema='profil_candidat' AND table_name='CandidateProfiles'
                                '''
                            )
                            cols = [r[0] for r in cur.fetchall()]
                            state["anomalies"].append(f"CandidateProfiles cols={cols} for {email}")
                            raise RuntimeError(f"No profile for {email}, cols={cols}")
                        pid = int(row[0])
                state["ids"][cid] = {"email": email, "profile_id": pid, "profile_key": profile_key}
                # apply
                for poste_key in APPLICATIONS[cid]:
                    co_key = POSTES[poste_key]["company"]
                    company_id = state["ids"][f"company_{co_key}"]["id"]
                    poste_id = state["ids"][f"poste_{poste_key}"]
                    apply_to_poste(page, company_id, poste_id)
                logout(page)
                save_state(state)
            state["steps"]["candidates"] = True
            save_state(state)

        # --- Process B candidatures ---
        if not state["steps"].get("process_b"):
            login(page, COMPANY_B["email"])
            state["ids"]["b_processing"] = {}
            for cid in APPLICATIONS:
                postes = [p for p in APPLICATIONS[cid] if p.startswith("B")]
                if not postes:
                    continue
                poste_key = postes[0]
                poste_id = state["ids"][f"poste_{poste_key}"]
                schema = state["ids"]["company_B"]["schema"]
                profile_id = state["ids"][cid]["profile_id"]
                with db() as conn:
                    with conn.cursor() as cur:
                        cur.execute(
                            f'''
                            SELECT "Id" FROM "{schema}"."Candidatures"
                            WHERE "PosteId"=%s AND "CandidateProfileId"=%s
                            ''',
                            (poste_id, profile_id),
                        )
                        row = cur.fetchone()
                        if not row:
                            state["anomalies"].append(f"Missing candidature {cid} on {poste_key}")
                            continue
                        candidature_id = int(row[0])

                # detail page - set niveaux
                page.goto(
                    f"{BASE}/entreprise/postes/{poste_id}/candidats/{candidature_id}",
                    wait_until="networkidle",
                )
                page.wait_for_timeout(1500)
                level = EVAL_LEVELS.get(cid, 2)
                selects = page.locator("select").filter(has=page.locator(f'option[value="{level}"]'))
                # Niveau final selects in table
                table_selects = page.locator("table select, .postes-criteres-table select, select")
                count = table_selects.count()
                filled = 0
                for i in range(count):
                    s = table_selects.nth(i)
                    # vary levels slightly per criterion
                    lvl = max(0, min(4, level + ((i % 3) - 1)))
                    try:
                        if s.locator(f'option[value="{lvl}"]').count():
                            s.select_option(str(lvl))
                            filled += 1
                            page.wait_for_timeout(300)
                    except Exception:
                        pass
                log(f"Eval {cid}: set {filled} niveaux (base={level})")

                # Generate IA analysis
                gen = page.get_by_role("button", name=re.compile(r"Générer l'analyse|Generate|Régénérer", re.I))
                if gen.count():
                    gen.first.click()
                    page.wait_for_timeout(12000)
                shot(page, f"b_detail_{cid}")

                # First interview
                page.goto(f"{BASE}/entretien/{profile_id}", wait_until="networkidle")
                page.wait_for_timeout(4000)
                shot(page, f"b_entretien1_{cid}")
                # Fill library answers for first 5 B candidates
                b_order = [c for c in APPLICATIONS if any(p.startswith("B") for p in APPLICATIONS[c])]
                if cid in b_order[:5]:
                    areas = page.locator("textarea[id^='iq-'], textarea.interview-answer, .bibliotheque textarea")
                    n = min(areas.count(), 3)
                    for i in range(n):
                        areas.nth(i).fill(f"Réponse entrevue scénario {cid} Q{i+1}: exemple concret, résultat mesurable.")
                    save_btn = page.get_by_role("button", name=re.compile(r"Enregistrer les réponses|Save answers|Enregistrer", re.I))
                    if save_btn.count():
                        save_btn.first.click()
                        page.wait_for_timeout(1500)

                # Guide 2e entrevue (per poste — visit once per poste later)
                state["ids"]["b_processing"][cid] = {
                    "poste_key": poste_key,
                    "poste_id": poste_id,
                    "candidature_id": candidature_id,
                    "profile_id": profile_id,
                    "eval_base": level,
                }
                save_state(state)

            # Guide entrevue once per B poste
            for poste_key in ("B1", "B2"):
                pid = state["ids"][f"poste_{poste_key}"]
                page.goto(f"{BASE}/entreprise/postes/{pid}/guide-entrevue", wait_until="networkidle")
                page.wait_for_timeout(1500)
                areas = page.locator("textarea")
                for i in range(min(areas.count(), 4)):
                    areas.nth(i).fill(
                        f"Guide 2e entrevue {poste_key} — axe {i+1}: explorer motivation, exemples STAR, culture fit."
                    )
                    page.wait_for_timeout(300)
                save_btn = page.get_by_role("button", name=re.compile(r"Enregistrer|Save", re.I))
                if save_btn.count():
                    save_btn.first.click()
                    page.wait_for_timeout(1000)
                shot(page, f"guide_entrevue_{poste_key}")

            logout(page)
            state["steps"]["process_b"] = True
            save_state(state)

        # --- Verifications A ---
        if not state["steps"].get("verify_a"):
            login(page, COMPANY_A["email"])
            page.goto(f"{BASE}/vivier", wait_until="networkidle")
            page.wait_for_timeout(2000)
            shot(page, "a_vivier")
            state["verify"] = state.get("verify", {})
            state["verify"]["a_vivier_text"] = page.locator("body").inner_text()[:4000]

            for poste_key in ("A1", "A2"):
                pid = state["ids"][f"poste_{poste_key}"]
                page.goto(f"{BASE}/entreprise/postes/{pid}/candidats", wait_until="networkidle")
                page.wait_for_timeout(1500)
                shot(page, f"a_candidats_{poste_key}")
                state["verify"][f"a_candidats_{poste_key}"] = page.locator("body").inner_text()[:5000]
                # open first candidature detail
                link = page.locator(f'a[href*="/entreprise/postes/{pid}/candidats/"]')
                if link.count():
                    href = link.first.get_attribute("href")
                    page.goto(f"{BASE}{href}", wait_until="networkidle")
                    page.wait_for_timeout(1500)
                    shot(page, f"a_detail_{poste_key}")
                    state["verify"][f"a_detail_{poste_key}"] = page.locator("body").inner_text()[:5000]
                page.goto(f"{BASE}/entreprise/postes/{pid}/selection", wait_until="networkidle")
                page.wait_for_timeout(1500)
                shot(page, f"a_selection_{poste_key}")
                state["verify"][f"a_selection_{poste_key}"] = page.locator("body").inner_text()[:5000]

            # direct entretien lock
            # pick C01 profile who applied to A
            pid = state["ids"]["C01"]["profile_id"]
            page.goto(f"{BASE}/entretien/{pid}", wait_until="networkidle")
            page.wait_for_timeout(2000)
            shot(page, "a_entretien_locked")
            state["verify"]["a_entretien_locked"] = page.locator("body").inner_text()[:3000]
            logout(page)
            state["steps"]["verify_a"] = True
            save_state(state)

        # --- Verifications B ---
        if not state["steps"].get("verify_b"):
            login(page, COMPANY_B["email"])
            page.goto(f"{BASE}/vivier", wait_until="networkidle")
            page.wait_for_timeout(2000)
            shot(page, "b_vivier")
            state["verify"]["b_vivier_text"] = page.locator("body").inner_text()[:4000]

            for poste_key in ("B1", "B2"):
                pid = state["ids"][f"poste_{poste_key}"]
                page.goto(f"{BASE}/entreprise/postes/{pid}/candidats", wait_until="networkidle")
                page.wait_for_timeout(1500)
                shot(page, f"b_candidats_{poste_key}")
                state["verify"][f"b_candidats_{poste_key}"] = page.locator("body").inner_text()[:5000]
                page.goto(f"{BASE}/entreprise/postes/{pid}/selection", wait_until="networkidle")
                page.wait_for_timeout(1500)
                shot(page, f"b_selection_{poste_key}")
                state["verify"][f"b_selection_{poste_key}"] = page.locator("body").inner_text()[:6000]

            # deep dive 3 candidates: C01 high, C08 mid/low, C15 low on B
            for cid in ("C01", "C08", "C15"):
                info = state["ids"]["b_processing"].get(cid)
                if not info:
                    continue
                page.goto(
                    f"{BASE}/entreprise/postes/{info['poste_id']}/candidats/{info['candidature_id']}",
                    wait_until="networkidle",
                )
                page.wait_for_timeout(1500)
                shot(page, f"b_deep_detail_{cid}")
                state["verify"][f"b_deep_detail_{cid}"] = page.locator("body").inner_text()[:6000]
                page.goto(f"{BASE}/entretien/{info['profile_id']}", wait_until="networkidle")
                page.wait_for_timeout(2500)
                shot(page, f"b_deep_entretien_{cid}")
                state["verify"][f"b_deep_entretien_{cid}"] = page.locator("body").inner_text()[:4000]

            # Cross-tenant: exclusive C12 should not appear in B vivier/candidats
            body = state["verify"]["b_vivier_text"] + state["verify"].get("b_candidats_B1", "") + state["verify"].get("b_candidats_B2", "")
            state["verify"]["cross_tenant_C12_in_B"] = ("Arthur" in body and "Garcia" in body)
            logout(page)

            login(page, COMPANY_A["email"])
            page.goto(f"{BASE}/vivier", wait_until="networkidle")
            page.wait_for_timeout(1500)
            body_a = page.locator("body").inner_text()
            state["verify"]["cross_tenant_C14_in_A"] = ("Adam" in body_a and "Bertrand" in body_a)
            state["verify"]["a_has_C12"] = ("Arthur" in body_a or "Garcia" in body_a)
            logout(page)
            state["steps"]["verify_b"] = True
            save_state(state)

        # Collect scores from DB for report
        scores = {}
        for co_key in ("A", "B"):
            schema = state["ids"][f"company_{co_key}"]["schema"]
            with db() as conn:
                with conn.cursor() as cur:
                    cur.execute(
                        f'''
                        SELECT c."Id", c."PosteId", c."CandidateProfileId",
                               r."ScoreGlobal"
                        FROM "{schema}"."Candidatures" c
                        LEFT JOIN "{schema}"."CompatibilityResults" r
                          ON r."CandidatureId"=c."Id" OR (r."CandidateProfileId"=c."CandidateProfileId" AND r."PosteId"=c."PosteId")
                        '''
                    )
                    # schema may differ — probe columns
        # safer probe
        for co_key in ("A", "B"):
            schema = state["ids"][f"company_{co_key}"]["schema"]
            with db() as conn:
                with conn.cursor() as cur:
                    cur.execute(
                        f'''
                        SELECT column_name FROM information_schema.columns
                        WHERE table_schema=%s AND table_name='CompatibilityResults'
                        ''',
                        (schema,),
                    )
                    cols = [r[0] for r in cur.fetchall()]
                    cur.execute(
                        f'SELECT * FROM "{schema}"."Candidatures"'
                    )
                    cand_cols = [d.name for d in cur.description]
                    rows = cur.fetchall()
                    scores[co_key] = {"candidature_cols": cand_cols, "compat_cols": cols, "rows": [list(r) for r in rows]}

        state["scores_raw"] = scores
        state["finished"] = datetime.now().isoformat()
        state["steps"]["done"] = True
        save_state(state)
        browser.close()
        log("DONE — state saved")


if __name__ == "__main__":
    main()
