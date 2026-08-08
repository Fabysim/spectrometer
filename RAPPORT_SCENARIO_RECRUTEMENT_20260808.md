# Rapport scénario E2E — Recrutement & gating

**Date d’exécution :** 2026-08-08  
**Application :** Spectromètre V2 — *Version modulaire* (`http://localhost:5263`)  
**Méthode :** inscription et parcours via l’interface (Playwright sur les formulaires SSR / Blazor InteractiveServer). **Aucune insertion métier SQL** (postes, candidatures, évaluations, analyses).  
**Exception technique documentée :** après chaque inscription réussie, `EmailConfirmed=TRUE` a été forcé en SQL sur `core."AspNetUsers"` uniquement — les e-mails Resend partent bien (`/verification-email`), mais les boîtes `@test.local` ne sont pas consultationables en local. Sans ce flag, `RequireConfirmedAccount` bloque le login.

**Artefacts :** captures dans `tools/scenario_recrutement_20260808/screenshots/` ; état brut `tools/scenario_recrutement_20260808/state.json`.

---

## 0. Arithmétique candidatures (validée avant exécution)

| | Postule A | Ne postule pas A |
|---|---|---|
| **Postule B** | C01–C11 (11) | **C14, C15** (exclusifs B) |
| **Ne postule pas B** | **C12, C13** (exclusifs A) | — |

- **A :** 13 postulants (C01–C13), 2 exclus (C14, C15)  
- **B :** 13 postulants (C01–C11 + C14 + C15), 2 exclus (C12, C13)  
- Les 2 exclus de A ≠ les 2 exclus de B : **OK**

Répartition postes :

| Poste | Candidats | Effectif |
|---|---|---|
| A1 Développeur full-stack .NET | C01–C08 | **8** |
| A2 Product Owner agile | C09–C13 | **5** |
| B1 Consultant senior RH | C01–C07, C14, C15 | **9** |
| B2 Analyste junior transformation | C08–C11 | **4** |

Comptes créés en base : **A = 8+5**, **B = 9+4** (vérifié).

---

## 1. Comptes créés (identifiants)

Mot de passe commun à tous les comptes : `ScenarioE2E2026!`

### Entreprises

| Rôle | Entreprise | Email | CompanyId | Schéma | Modules actifs |
|---|---|---|---|---|---|
| **A** (sans Recrutement) | NovaTech Solutions (PME SaaS technologique) | `scenario20260808.entreprise.a@test.local` | 3747 | `co_novatech_solutions` | **ProfilEntreprise uniquement** |
| **B** (avec Recrutement) | Cabinet Horizon Conseil (conseil RH) | `scenario20260808.entreprise.b@test.local` | 3748 | `co_cabinet_horizon_conseil` | ProfilEntreprise + **Compatibilite, Recrutement, Vivier, Entretien, Analytics** |

### Candidats

| Id | Nom | Email | ProfileId | Profil grille (intention) |
|---|---|---|---|---|
| C01 | Léa Dupont | `scenario20260808.c01@test.local` | 4979 | high_a (tech) |
| C02 | Hugo Bernard | `scenario20260808.c02@test.local` | 4980 | high_a |
| C03 | Chloé Petit | `scenario20260808.c03@test.local` | 4981 | high_a |
| C04 | Nathan Robert | `scenario20260808.c04@test.local` | 4982 | mid_a |
| C05 | Manon Richard | `scenario20260808.c05@test.local` | 4983 | mid_a |
| C06 | Lucas Durand | `scenario20260808.c06@test.local` | 4984 | mid_b |
| C07 | Emma Moreau | `scenario20260808.c07@test.local` | 4985 | mid_b |
| C08 | Louis Simon | `scenario20260808.c08@test.local` | 4986 | low_a |
| C09 | Jade Laurent | `scenario20260808.c09@test.local` | 4987 | high_b |
| C10 | Gabriel Lefebvre | `scenario20260808.c10@test.local` | 4988 | high_b |
| C11 | Inès Michel | `scenario20260808.c11@test.local` | 4989 | mid_mix |
| C12 | Arthur Garcia | `scenario20260808.c12@test.local` | 4990 | high_a — **A seulement** |
| C13 | Lina David | `scenario20260808.c13@test.local` | 4991 | mid_a — **A seulement** |
| C14 | Adam Bertrand | `scenario20260808.c14@test.local` | 4992 | high_b — **B seulement** |
| C15 | Alice Roux | `scenario20260808.c15@test.local` | 4993 | low_b — **B seulement** |

---

## 2. Profils entreprises & postes

### Profils

- **NovaTech (A)** : tags tech (bureautique, outils numériques, gestion de projet, langues), culture innovation/autonomie, rythme **4 (intense)**.  
- **Horizon (B)** : tags conseil (gestion projet/équipe, rédaction, service client), culture respect/transparence, rythme **3 (soutenu)**.  
- Captures : `entreprise_profil_NovaTech_So.png`, `entreprise_profil_Cabinet_Hor.png`, `entreprise_b_modules.png`.

### Postes & offres auto-générées

| Poste | Titre | Critères | Offre auto | `OffreGenereeParIa` | Longueur |
|---|---|---|---|---|---|
| A1 | Développeur full-stack .NET | manuels + **génération IA** | **Oui** | true | 1794 |
| A2 | Product Owner agile | manuels | **Oui** | true | 1645 |
| B1 | Consultant senior RH | manuels + **génération IA** | **Oui** | true | 2197 |
| B2 | Analyste junior en transformation | manuels | **Oui** | true | 1899 |

**Confirmation :** les 4 postes ont bien une `OffreTexte` persistée (aperçu A1 : « OFFRE D'EMPLOI / DÉVELOPPEUR FULL-STACK .NET… »). Les critères IA ont enrichi A1 et B1 (ex. DevOps, Expertise RH, etc.).

---

## 3. Tableau candidat × poste × candidature

Légende : ✅ = a postulé · — = n’a pas postulé

| Candidat | A1 Dev .NET | A2 PO agile | B1 Consultant RH | B2 Analyste junior |
|---|---|---|---|---|
| C01 Léa Dupont | ✅ | — | ✅ | — |
| C02 Hugo Bernard | ✅ | — | ✅ | — |
| C03 Chloé Petit | ✅ | — | ✅ | — |
| C04 Nathan Robert | ✅ | — | ✅ | — |
| C05 Manon Richard | ✅ | — | ✅ | — |
| C06 Lucas Durand | ✅ | — | ✅ | — |
| C07 Emma Moreau | ✅ | — | ✅ | — |
| C08 Louis Simon | ✅ | — | — | ✅ |
| C09 Jade Laurent | — | ✅ | — | ✅ |
| C10 Gabriel Lefebvre | — | ✅ | — | ✅ |
| C11 Inès Michel | — | ✅ | — | ✅ |
| C12 Arthur Garcia | — | ✅ | — | — |
| C13 Lina David | — | ✅ | — | — |
| C14 Adam Bertrand | — | — | ✅ | — |
| C15 Alice Roux | — | — | ✅ | — |
| **Totaux** | **8** | **5** | **9** | **4** |

---

## 4. Traitement entreprise B (module Recrutement)

Pour **chacun des 13** candidats B :

1. **Grille d’évaluation** (`/entreprise/postes/{posteId}/candidats/{candidatureId}`) — niveaux finaux volontairement variés (base 1→4 selon candidat).  
2. **1ʳᵉ entrevue** (`/entretien/{profileId}`) — grille générée par axes + bibliothèque de questions visible. Réponses bibliothèque saisies pour **au moins 5** candidats (C01–C05).  
3. **Guide 2ᵉ entrevue** (`/entreprise/postes/{posteId}/guide-entrevue`) — complété **par poste** (B1 et B2), pas une URL par candidat (comportement actuel de l’app).  
4. **Analyse IA** — générée pour les 13 candidatures (13 lignes `AnalysesIaPoste`).

**Non fait pour A** (volontaire) — sert de contrôle gating.

### Tableau B — score compatibilité × niveau grille × recommandation IA

Scores lus sur `/entreprise/postes/{id}/selection` **après** correction des grilles candidat (voir anomalies). Niveaux = moyenne des `NiveauFinal` (échelle 0–4).

#### Poste B1 — Consultant senior RH

| Candidat | ProfileId | Compat. (sélection) | Niveau moyen grille | Ton du rapport IA (extrait) |
|---|---|---|---|---|
| C14 Adam Bertrand | 4992 | **100%** | 3,7/4 | Profil fort / aligné conseil RH |
| C06 Lucas Durand | 4984 | 49% | 2/4 | Adéquation partielle |
| C07 Emma Moreau | 4985 | 44% | 2/4 | Adéquation partielle |
| C05 Manon Richard | 4983 | 39% | 2/4 | Écarts à creuser |
| C04 Nathan Robert | 4982 | 39% | 3/4 | Mix évaluation haute / compat. moyenne |
| C03 Chloé Petit | 4981 | 32% | 3/4 | Idem |
| C02 Hugo Bernard | 4980 | 27% | 3,7/4 | IA très favorable sur **grille critères** malgré compat. tags basse (profil tech) |
| C01 Léa Dupont | 4979 | 27% | 3,7/4 | « Profil global très prometteur… excellente adéquation » (basé sur niveaux critères, pas sur tags) |
| C15 Alice Roux | 4993 | **10%** | 1/4 | Lacunes majeures, recommandations de creuser / risque |

#### Poste B2 — Analyste junior

| Candidat | ProfileId | Compat. | Niveau moyen | Ton IA |
|---|---|---|---|---|
| C10 Gabriel Lefebvre | 4988 | **100%** | 3/4 | Fort alignement |
| C09 Jade Laurent | 4987 | **100%** | 3,7/4 | Fort alignement |
| C11 Inès Michel | 4989 | 49% | 2/4 | Moyen |
| C08 Louis Simon | 4986 | **28%** | 1/4 | Écarts synthèse / outils ; prudence |

**Cohérence IA vs « meilleur candidat » :**  
- Sur **B1**, le meilleur score de **compatibilité tags** est **C14 (100%)**, exclusif B et profil `high_b`. L’IA sur C01 (27% tags mais 3,7/4 grille) reste très enthousiaste car elle s’appuie surtout sur les **niveaux d’évaluation des critères de poste**. Les deux signaux divergent — à documenter comme comportement actuel, pas comme panne.  
- Sur **B2**, C09/C10 (100%) sont clairement devant C08 (28%) ; l’IA sur C08 souligne bien les écarts (synthèse écrite, Pack Office).

Captures deep-dive : `b_deep_detail_C01*.png`, `b_deep_detail_C08.png`, `b_deep_detail_C15.png`, entretiens associés, `b_selection_B1_apres_grilles.png`, `b_selection_B2_apres_grilles.png`.

---

## 5. Vérifications entreprise A (SANS module Recrutement)

| Contrôle | Résultat | Preuve |
|---|---|---|
| Vivier liste les 13 candidatures | **Oui** (profils #4979–#4991) | `a_vivier*.png` |
| Scores de compatibilité dans le vivier | **Non calculés** (« Score non calculé ») | Voir anomalies — module **Compatibilite** absent du socle A |
| Liste candidatures : liens 1ʳᵉ/2ᵉ entrevue | **Verrouillés** — badge « Nécessite le module Recrutement » (pas de lien mort) | `a_candidats_A1.png`, texte state |
| Détail candidature : carte « Préparer l'entretien » | **Verrouillée** + CTA « Activer le module » | `a_detail_A1.png` |
| Détail : section Analyse IA | **Verrouillée** (même message) | idem |
| Grille critères (Niveau final) | **Toujours éditable** (socle) | visible sur détail A |
| `/entreprise/postes/{id}/selection` | **Accessible** sans Recrutement ; ranking affiché (compat. « — » faute de module Compatibilite) | `a_selection_A1.png` |
| Accès direct `/entretien/{profileId}` (C01) | **Bloqué** — message verrouillage module, **pas** la grille | `a_entretien_locked.png` |

---

## 6. Vérifications entreprise B (AVEC Recrutement)

| Contrôle | Résultat |
|---|---|
| Vivier + accès entrevues depuis liste | **Oui** — liens « Première entrevue » / « Deuxième entrevue » actifs ; grilles ✅ |
| KPI sélection — classement varié | **Oui** — B1 de 100% → 10% ; B2 de 100% → 28% |
| Deep-dive 3 contrastés (C14/C01 fort eval, C08 moyen/faible, C15 faible) | Grille + entretiens + rapport IA présents |
| Fuite cross-tenant | **Non observée** : C12/C13 absents du vivier/listes B ; C14/C15 absents du vivier A (présence par ProfileId côté A uniquement pour postulants A) |

---

## 7. Comportement du gating Recrutement observé

| Surface | Entreprise A (socle) | Entreprise B (+ Recrutement) | Attendu ? |
|---|---|---|---|
| Modules DB | ProfilEntreprise | + Compatibilite, Recrutement, Vivier, Entretien, Analytics | Oui (bundle activation) |
| Vivier — liste candidatures | Visible | Visible | Oui |
| Vivier — score % | « Score non calculé » | Affiche un % (voir anomalie cache 50%) | Partiel — A sans Compatibilite |
| Liste candidatures — entrevues | Badge cadenas / texte module requis | Liens actifs | **Oui** |
| Détail — Préparer entretien | Carte `--locked` | Carte active | **Oui** |
| Détail — Analyse IA | Section verrouillée | Génération / PDF / régénérer | **Oui** |
| Détail — Grille Niveau final | Éditable | Éditable | **Oui** (socle) |
| Page Sélection | Accessible | Accessible + scores + niveaux | **Oui** |
| `/entretien/{id}` direct | Message verrouillé | Grille + bibliothèque | **Oui** |
| Guide 2ᵉ entrevue | Lien présent mais flux recrutement verrouillé côté actions | Accessible, éditable | **Oui** |

**Verdict gating :** le découpage livré précédemment se comporte correctement en conditions réelles : le socle conserve grille + sélection ; Recrutement débloque entretiens + analyse IA ; pas de liens morts.

---

## 8. Anomalies constatées

1. **Scores vivier entreprise A = « Score non calculé »**  
   Cause probable : activation Recrutement provisionne aussi **Compatibilite** (et Vivier) ; le socle A n’a que ProfilEntreprise. La page `/vivier` reste utilisable mais le calcul de score ne tourne pas. **Écart vs attente métier** (« vivier avec score » pour A).

2. **Vivier B reste à 50% pour tous** après correction des grilles, alors que **Sélection** et **détail candidature** affichent des scores différenciés (100%…10%). Suspicion de **cache / chemin d’affichage vivier** non invalidé (ou snapshot neutre). Les KPI sélection sont eux corrects.

3. **Première passe d’automatisation** : les tags Grille H candidat n’avaient pas été persistés (clics trop tôt / hors étape grille). **Corrigé via UI** dans un second passage ; les scores sélection reflètent maintenant la diversité voulue.

4. **EmailConfirmed** : forçage SQL nécessaire pour login local (documenté en tête). Inscription UI + envoi Resend OK.

5. **Rapport IA vs score tags** : pour un candidat très bien noté sur la grille critères mais mal aligné en tags (ex. C01 sur B1), l’IA reste très positive. Cohérent avec l’implémentation actuelle (poids fort des niveaux finaux), mais peut surprendre si l’on attend une synthèse dominée par la compatibilité structurée.

6. **Guide 2ᵉ entrevue** : une page **par poste**, pas par candidat — le scénario « pour chaque candidat » a été interprété comme « pour chaque poste ayant des candidatures B ».

**Hors ces points, le parcours bout-en-bout (inscription → postes → candidatures → évaluation → entretiens → IA → gating A/B → isolation tenant) fonctionne.**

---

## 9. Isolation cross-tenant

| Vérification | Résultat |
|---|---|
| C12/C13 (exclusifs A) absents des listes B | **OK** (`cross_tenant_C12_in_B = false`) |
| C14/C15 (exclusifs B) absents du vivier A | **OK** |
| Postes / candidatures dans schémas séparés `co_novatech_solutions` vs `co_cabinet_horizon_conseil` | **OK** |

---

## 10. Captures d’écran (principales)

| Fichier | Contenu |
|---|---|
| `a_vivier_apres_grilles.png` | Vivier A — 13 candidats, scores non calculés |
| `a_candidats_A1.png` | Liste A1 — badges module Recrutement |
| `a_detail_A1.png` | Détail verrouillé entretien/IA, grille OK |
| `a_selection_A1.png` | Sélection accessible sans Recrutement |
| `a_entretien_locked.png` | Garde `/entretien/{id}` |
| `b_vivier_apres_grilles.png` | Vivier B — 13 candidats |
| `b_candidats_B1.png` / `B2` | Liens entrevues actifs |
| `b_selection_B*_apres_grilles.png` | Classements scores variés |
| `b_deep_detail_C01|C08|C15*.png` | Contrastés + rapports IA |
| `b_deep_entretien_C*.png` | Grilles 1ʳᵉ entrevue + bibliothèque |
| `guide_entrevue_B1|B2.png` | Guides 2ᵉ entrevue |
| `poste_*_profil.png` | Profils postes / critères |

---

## 11. Conclusion

Le scénario de volume (2 entreprises, 4 postes, 15 candidats, matrice d’exclusion croisée, traitement complet B, contrôle gating A) a été exécuté **via l’UI** avec données réelles.  

**Gating Recrutement : conforme** (verrous UI + garde `/entretien`, sélection/grille socle conservés).  
**Isolation tenant : conforme.**  
**KPI / IA / entretiens côté B : opérationnels**, avec classement de sélection clairement différencié après correction des grilles candidat.  

Les anomalies listées en §8 (scores vivier A, cache 50% vivier B, divergence IA vs tags) sont les principaux points de suivi produit — aucun n’a empêché la clôture du scénario.
