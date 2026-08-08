# Rapport scénario E2E — Rejeu grille obligatoire + entrevues (Entreprise B)

**Date d’exécution :** 2026-08-09  
**Fichier :** `RAPPORT_SCENARIO_ENTREVUES_20260809.md`  
**Scénario amont :** [`RAPPORT_SCENARIO_RECRUTEMENT_20260808.md`](./RAPPORT_SCENARIO_RECRUTEMENT_20260808.md) (non modifié)  
**Application :** Spectromètre V2 — *Version modulaire* (`http://localhost:5263`)  
**Périmètre :** **Entreprise B uniquement** (Cabinet Horizon Conseil, CompanyId **3748**, schéma `co_cabinet_horizon_conseil`). Entreprise A et dossier `mvp/` non touchés.  
**Artefacts :** `tools/scenario_entrevues_20260808/` (`state.json`, `run2.log`, `regen_ia.log`, `screenshots/`).

---

## 0. Prérequis — grille obligatoire

| Contrôle | Résultat |
|---|---|
| Code `PostulerAvecGrilleAsync` / `PostulationGrilleInline` / `NiveauDeclare` | Présent |
| Tests ciblés `PostulerAvecGrille` | **3/3** verts |
| Suite complète (après correctif score IA) | **118/118** |
| Host local | répond 200 |
| `mvp/` | intact |

**Verdict :** prérequis **OK** — poursuite du scénario.

---

## 1. Méthode & comptes réutilisés

- **Pas de recréation** d’entreprises, postes ni candidats. Mot de passe : `ScenarioE2E2026!`
- **Entreprise B :** `scenario20260808.entreprise.b@test.local` — B1 `#1` Consultant senior RH (16 critères), B2 `#2` Analyste junior en transformation (3 critères)
- **13 candidats B :** C01–C07, C14, C15 → B1 ; C08–C11 → B2

### État des candidatures au démarrage de ce run

Les 13 candidatures B issues du rejeu grille (ids **15–27**) avaient déjà **`NiveauDeclare` renseigné pour 100 % des critères** (0 NULL). Conformément à la consigne (« si … SANS NiveauDeclare … retire et refais »), **aucun retrait / re-postulation** n’a été nécessaire — les candidatures existantes ont été **réutilisées**.

| Candidat | Cand. id | Poste | Déclaré moy. | Final moy. | Intention |
|---|---|---|---|---|---|
| C01 Léa Dupont | 15 | B1 | 2,56 | 1,94 | high_a |
| C02 Hugo Bernard | 16 | B1 | 2,56 | 2,94 | high_a |
| C03 Chloé Petit | 17 | B1 | 2,56 | 1,94 | high_a |
| C04 Nathan Robert | 18 | B1 | 1,50 | 1,94 | mid_a |
| C05 Manon Richard | 19 | B1 | 1,50 | 2,88 | mid_a |
| C06 Lucas Durand | 20 | B1 | 2,50 | 2,94 | mid_b |
| C07 Emma Moreau | 21 | B1 | 2,50 | 1,94 | mid_b |
| C14 Adam Bertrand | 26 | B1 | 3,50 | 3,63 | high_b |
| C15 Alice Roux | 27 | B1 | 0,50 | 0,38 | low_b |
| C08 Louis Simon | 22 | B2 | 0,67 | 1,33 | low_a |
| C09 Jade Laurent | 23 | B2 | 3,67 | 3,67 | high_b |
| C10 Gabriel Lefebvre | 24 | B2 | 3,67 | 3,67 | high_b |
| C11 Inès Michel | 25 | B2 | 1,67 | 2,00 | mid_mix |

Les niveaux déclarés restent cohérents avec les intentions (`high_b` > `mid_*` > `low_*`). Les finals divergent volontairement du déclaré pour plusieurs profils (ex. C01 ↓, C05 ↑, C14 top, C15 fond).

---

## 2. Traitement entreprise

| Étape | Statut |
|---|---|
| Niveaux finaux (détail candidature) | Déjà saisis (écarts F−D non nuls sur plusieurs profils) |
| 1ʳᵉ entrevue `/entretien/{id}` | Bibliothèque **disponible** — **6 réponses × 13 = 78** persistées |
| Guide 2ᵉ entrevue B1 & B2 | Complétés (1 page / poste) |
| Analyse IA | **Régénérée pour les 13** après le correctif « score tags dans le prompt » (`GenererAnalyseIaAsync` + `ICompatibiliteScoreService`) |

**Entreprise A :** inchangée (13 candidatures, 0 analyse IA).

---

## 3. Preuves UI (captures)

| Capture | Contenu |
|---|---|
| `screenshots/grille_inline_B1_ouverte.png` / `grille_inline_B1_component.png` | Nouveau flux : grille inline, confirmation grisée tant que « Choisir… » |
| `screenshots/detail_declare_C01.png` / `detail_declare_final_C01.png` | Colonne **Niveau déclaré** remplie (plus de « — ») + finals / écart |
| `screenshots/selection_B1.png` / `selection_B2.png` / `selection_*_apres_regen_ia.png` | Classement sélection |
| `screenshots/ia_regen_C14.png` / `recommended_apres_regen_C14.png` | Rapport IA C14 (priorité B1) |
| `screenshots/ia_regen_C09.png` / `C10` | Rapports IA candidats B2 ex-æquo |
| `screenshots/entretien1_C14.png` / `guide_entrevue_B1.png` / `B2` | Entretiens / guides |

---

## 4. Synthèse par poste (tous signaux côte à côte)

Échelle niveaux : moyenne 0–4. **Écart F−D** = final − déclaré. **Compat.** = score tags (page Sélection). **IA** = ton après régénération (prompt enrichi tags + axes).

### 4.1 Poste B1 — Consultant senior RH

| Candidat | Intention | Déclaré | Final | Écart F−D | Compat. | IA (extrait / ton) |
|---|---|---|---|---|---|---|
| **C14 Adam Bertrand** #4992 | high_b | **3,50** | **3,63** | +0,13 | **100%** | **Profil très solide** ; petits écarts Fort vs Très fort ; section « Cohérence tags/grille » : divergence 100 % tags vs écarts techniques mineurs — **priorité claire** |
| C06 Lucas Durand #4984 | mid_b | 2,50 | 2,94 | +0,44 | 49% | Atouts projet/B2B ; divergence tags/grille signalée |
| C07 Emma Moreau #4985 | mid_b | 2,50 | 1,94 | −0,56 | 44% | Points forts + écarts critiques ; divergence signalée |
| C05 Manon Richard #4983 | mid_a | 1,50 | 2,88 | **+1,38** | 39% | Grille finale haute vs tags 39 % — **« contradiction flagrante »** explicitement écrite |
| C04 Nathan Robert #4982 | mid_a | 1,50 | 1,94 | +0,44 | 39% | Mix atouts / écarts ; divergence signalée |
| C03 Chloé Petit #4981 | high_a | 2,56 | 1,94 | −0,63 | 32% | Lacunes métier ; divergence signalée |
| C02 Hugo Bernard #4980 | high_a | 2,56 | 2,94 | +0,38 | 27% | Grille plutôt haute / tags bas — divergence signalée |
| C01 Léa Dupont #4979 | high_a | 2,56 | 1,94 | −0,63 | **27%** | Lacunes cœur de métier ; **score tags 27 %** vs org. 75 % / motiv. 0 % — divergence explicite |
| C15 Alice Roux #4993 | low_b | **0,50** | **0,38** | −0,13 | **10%** | Inadéquation profonde ; **tags 10 % convergent** avec grille basse |

**Priorité IA B1 :** **C14 Adam Bertrand**.  
Tags 100 %, final le plus haut, IA « profil très solide ». La seule nuance : l’IA signale désormais une **divergence mineure** (tags parfaits vs 2–3 critères Fort au lieu de Très fort) — elle ne lisse plus silencieusement.

**Cohérence / contradictions B1 :**
- **C14 / C15 :** signaux globalement alignés (haut / bas).
- **C01 / C02 / C05 :** contradictions tags↔grille **explicitées** dans le rapport IA (correctif livré le même jour) — comportement attendu, plus une anomalie produit.
- **C05** reste le cas d’école : déclaré 1,5 → final 2,88 (+1,38) avec compat. 39 % ; l’IA parle maintenant de « contradiction flagrante » au lieu d’ignorer les tags.

### 4.2 Poste B2 — Analyste junior en transformation

| Candidat | Intention | Déclaré | Final | Écart F−D | Compat. | IA (extrait / ton) |
|---|---|---|---|---|---|---|
| **C09 Jade Laurent** #4987 | high_b | **3,67** | **3,67** | 0 | **100%** | **Très prometteur** ; aucun écart défavorable ; **cohérence tags 100 % / grille** |
| **C10 Gabriel Lefebvre** #4988 | high_b | **3,67** | **3,67** | 0 | **100%** | Profil remarquable ; aucun écart ; **signaux parfaitement alignés** |
| C11 Inès Michel #4989 | mid_mix | 1,67 | 2,00 | +0,33 | 49% | Pack Office fort ; écart synthèse ; divergence partielle |
| C08 Louis Simon #4986 | low_a | 0,67 | 1,33 | +0,67 | 28% | Pack Office OK ; lacunes synthèse/écoute ; divergence signalée |

*(Hors scénario : profil #5059 / cand. 14 — 50 %, niveau « — » ; voir §6.)*

**Priorité IA B2 :** **ex-æquo C09 et C10** (Sélection place C10 en tête, textes IA équivalents « très prometteur / remarquable » + tags 100 %). **Recommandation retenue : C10**, puis C09 immédiat.

**Cohérence B2 :** pour C09/C10, tags + grille + IA convergent — l’IA le dit explicitement (« parfaite cohérence »). C08/C11 restent en retrait sur les trois axes.

---

## 5. Recommandations finales (lecture décideur)

| Poste | Priorité | Pourquoi | Vigilance |
|---|---|---|---|
| **B1** | **C14** | 100 % tags + final 3,6 + IA très solide | Petits écarts Fort→Très fort (atelier, recrutement) — désormais nommés |
| **B2** | **C10** (puis C09) | 100 % + 3,7/4 + IA sans écart défavorable | Quasi-égalité C09 ; point de vigilance « pression commerciale » à creuser |

---

## 6. Écarts / anomalies

1. **Candidature parasite B2 #5059** (`scenario20260808.grille.*@test.local`, cand. 14) — hors C01–C15, finals non saisis, pas d’IA. Visible à 50 % dans Sélection. Non traitée (hors périmètre).
2. **Captures `full_page` `/candidat/postes`** — hauteur ~185k px (liste mondiale de postes de test). Preuves grille reprises via `/candidat/postes/3748/1`.
3. **Correctif IA score tags (même journée)** — les analyses du premier passage (avant ~00:46) ignoraient les tags ; **toutes régénérées** ensuite. Le présent rapport s’appuie uniquement sur les textes post-correctif.
4. **Absence d’anomalie sur le cœur du scénario** — 13/13 `NiveauDeclare` OK ; entretiens 78 réponses ; guides B1/B2 OK ; A et `mvp/` non touchés.

---

## 7. Isolation & non-régression

| Vérification | Résultat |
|---|---|
| Entreprise A | **OK** (13 candidatures, 0 analyses) |
| `mvp/` | **OK** |
| Tenant B isolé | Schéma `co_cabinet_horizon_conseil` |

---

## 8. Conclusion

Le rejeu B avec grille obligatoire est **clos** : déclarés cohérents avec les intentions, finals différenciés, entretiens complets, guides OK, et analyses IA désormais **pondérées tags + grille** (divergences explicitement signalées, notamment C01/C05).

**Priorités :** B1 → **C14** ; B2 → **C10** (C09 quasi-ex-æquo).
