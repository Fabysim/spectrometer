using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.ProfilCandidat.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Formulaire de CV structuré (sections 1 à 8 du document source « Formulaire de curriculum vitæ pour
/// postulant.pdf »), extension du module ProfilCandidat existant — même schéma fixe scopé par
/// <c>CandidateProfileId</c>, même discipline xmin que <see cref="CandidateCompatibilityCriteria"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class CvCandidatTests(ServiceFixture fixture)
{
    private ICandidateProfileService Service => fixture.Services.GetRequiredService<ICandidateProfileService>();

    [Fact]
    public async Task GetCvAsync_CandidatSansCvRempli_RetourneToutesLesSectionsVidesSansException()
    {
        var userId = $"cv-test-vide-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        var cv = await Service.GetCvAsync(candidateProfileId);

        Assert.Null(cv.Coordonnees);
        Assert.Empty(cv.Formations);
        Assert.Null(cv.CompetencesEtudes);
        Assert.Empty(cv.Experiences);
        Assert.Null(cv.CaracteristiquesPersonnelles);
        Assert.Null(cv.Loisirs);
        Assert.Empty(cv.References);
        Assert.Null(cv.Declaration);
    }

    [Fact]
    public async Task SaveCoordonneesAsync_PuisGet_RetourneLesChampsEtIgnoreLIdFourniParLAppelant()
    {
        var userId = $"cv-test-coordonnees-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        await Service.SaveCoordonneesAsync(candidateProfileId, new CvCoordonnees
        {
            Id = 999999, // doit être ignoré
            CandidateProfileId = 888888, // doit être écrasé côté serveur
            Nom = "Dupont",
            Prenoms = "Jean",
            DateNaissance = new DateOnly(1990, 5, 12),
            LieuNaissance = "Lyon",
            Nationalite = "Française",
            AdresseComplete = "12 rue des Lilas",
            Telephone = "0601020304",
            Email = "jean.dupont@example.com",
            ProfilOuPosteRecherche = "Développeur backend",
        });

        var cv = await Service.GetCvAsync(candidateProfileId);
        Assert.NotNull(cv.Coordonnees);
        Assert.Equal(candidateProfileId, cv.Coordonnees!.CandidateProfileId);
        Assert.NotEqual(999999, cv.Coordonnees.Id);
        Assert.Equal("Dupont", cv.Coordonnees.Nom);
        Assert.Equal("Jean", cv.Coordonnees.Prenoms);
        Assert.Equal(new DateOnly(1990, 5, 12), cv.Coordonnees.DateNaissance);
        Assert.Equal("jean.dupont@example.com", cv.Coordonnees.Email);

        // Un second enregistrement met à jour la même ligne (upsert), jamais une nouvelle.
        await Service.SaveCoordonneesAsync(candidateProfileId, new CvCoordonnees { Nom = "Durand" });
        var cvMisAJour = await Service.GetCvAsync(candidateProfileId);
        Assert.Equal(cv.Coordonnees.Id, cvMisAJour.Coordonnees!.Id);
        Assert.Equal("Durand", cvMisAJour.Coordonnees.Nom);
    }

    [Fact]
    public async Task SaveFormationAsync_AjoutModificationSuppression_FonctionnentCorrectement()
    {
        var userId = $"cv-test-formations-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        // Ajout de deux lignes.
        var id1 = await Service.SaveFormationAsync(candidateProfileId, id: null, new CvFormation
        {
            Periode = "De 2015 à 2018", Etablissement = "Université A", DiplomeCertificatOuNiveau = "Licence", DomaineEtudes = "Informatique",
        });
        var id2 = await Service.SaveFormationAsync(candidateProfileId, id: null, new CvFormation
        {
            Periode = "De 2018 à 2020", Etablissement = "Université B", DiplomeCertificatOuNiveau = "Master", DomaineEtudes = "Génie logiciel",
        });

        var apresAjout = (await Service.GetCvAsync(candidateProfileId)).Formations;
        Assert.Equal(2, apresAjout.Count);
        Assert.Equal(id1, apresAjout[0].Id); // ordre d'affichage préservé (DisplayOrder croissant)
        Assert.Equal(id2, apresAjout[1].Id);

        // Modification de la première ligne (même Id, pas de duplication).
        await Service.SaveFormationAsync(candidateProfileId, id: id1, new CvFormation { Etablissement = "Université A modifiée" });
        var apresModification = (await Service.GetCvAsync(candidateProfileId)).Formations;
        Assert.Equal(2, apresModification.Count);
        Assert.Equal("Université A modifiée", apresModification.Single(f => f.Id == id1).Etablissement);

        // Suppression de la seconde ligne.
        await Service.DeleteFormationAsync(candidateProfileId, id2);
        var apresSuppression = (await Service.GetCvAsync(candidateProfileId)).Formations;
        Assert.Equal(id1, Assert.Single(apresSuppression).Id);

        // Suppression idempotente (ligne déjà supprimée) : ne lève pas.
        await Service.DeleteFormationAsync(candidateProfileId, id2);
    }

    [Fact]
    public async Task SaveExperienceAsync_AjoutModificationSuppression_FonctionnentCorrectement()
    {
        var userId = $"cv-test-experiences-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        var id = await Service.SaveExperienceAsync(candidateProfileId, id: null, new CvExperience
        {
            Periode = "De 2020 à 2022", EntrepriseOrganisationOuStage = "Acme Corp", FonctionOuActiviteExercee = "Stagiaire", CompetencesDeveloppees = "Travail d'équipe",
        });

        var apresAjout = Assert.Single((await Service.GetCvAsync(candidateProfileId)).Experiences);
        Assert.Equal("Acme Corp", apresAjout.EntrepriseOrganisationOuStage);

        await Service.SaveExperienceAsync(candidateProfileId, id: id, new CvExperience { FonctionOuActiviteExercee = "Développeur junior" });
        var apresModification = Assert.Single((await Service.GetCvAsync(candidateProfileId)).Experiences);
        Assert.Equal(id, apresModification.Id);
        Assert.Equal("Développeur junior", apresModification.FonctionOuActiviteExercee);

        await Service.DeleteExperienceAsync(candidateProfileId, id);
        Assert.Empty((await Service.GetCvAsync(candidateProfileId)).Experiences);
    }

    [Fact]
    public async Task SaveReferenceAsync_AjoutModificationSuppression_FonctionnentCorrectement()
    {
        var userId = $"cv-test-references-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        var id = await Service.SaveReferenceAsync(candidateProfileId, id: null, new CvReference
        {
            NomPrenom = "Marie Curie", Fonction = "Directrice", EntrepriseOrganisation = "Institut du Radium",
            TelephoneOuEmail = "marie@example.com", LienAvecPostulant = "Ancienne responsable",
        });

        var apresAjout = Assert.Single((await Service.GetCvAsync(candidateProfileId)).References);
        Assert.Equal("Marie Curie", apresAjout.NomPrenom);

        await Service.SaveReferenceAsync(candidateProfileId, id: id, new CvReference { LienAvecPostulant = "Ancienne collègue" });
        var apresModification = Assert.Single((await Service.GetCvAsync(candidateProfileId)).References);
        Assert.Equal("Ancienne collègue", apresModification.LienAvecPostulant);

        await Service.DeleteReferenceAsync(candidateProfileId, id);
        Assert.Empty((await Service.GetCvAsync(candidateProfileId)).References);
    }

    [Fact]
    public async Task SectionsSingletonsRestantes_CompetencesEtudesCaracteristiquesLoisirsDeclaration_SauvegardeEtLectureCorrectes()
    {
        var userId = $"cv-test-singletons-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);

        await Service.SaveCompetencesEtudesAsync(candidateProfileId, new CvCompetencesEtudes
        {
            SpecialitePrincipale = "Génie logiciel",
            CompetencesTechniques = "C#, SQL",
            ConnaissancesTheoriques = "Algorithmique",
            LanguesMaitrisees = "Français, Anglais",
            OutilsLogicielsMethodes = "Git, Scrum",
        });

        await Service.SaveCaracteristiquesPersonnellesAsync(candidateProfileId, new CvCaracteristiquesPersonnelles
        {
            QualitesPersonnelles = "Ponctualité",
            AptitudesProfessionnelles = "Autonomie",
            AttitudesRelationnelles = "Esprit d'équipe",
            CapaciteSousPression = "Bonne",
            DisponibiliteMobilite = "Immédiate",
        });

        await Service.SaveLoisirsAsync(candidateProfileId, new CvLoisirs
        {
            LoisirsPreferes = "Lecture",
            ActivitesSportivesCulturelles = "Natation",
            EngagementsAssociatifs = "Bénévolat associatif",
            AutresCentresInteret = "Photographie",
        });

        await Service.SaveDeclarationAsync(candidateProfileId, new CvDeclaration
        {
            CertificationExactitude = true,
            ConsentementConsultation = true,
            Date = new DateOnly(2026, 8, 6),
            NomSignataire = "Jean Dupont",
        });

        var cv = await Service.GetCvAsync(candidateProfileId);

        Assert.Equal("Génie logiciel", cv.CompetencesEtudes?.SpecialitePrincipale);
        Assert.Equal("Ponctualité", cv.CaracteristiquesPersonnelles?.QualitesPersonnelles);
        Assert.Equal("Lecture", cv.Loisirs?.LoisirsPreferes);
        Assert.NotNull(cv.Declaration);
        Assert.True(cv.Declaration!.CertificationExactitude);
        Assert.True(cv.Declaration.ConsentementConsultation);
        Assert.Equal("Jean Dupont", cv.Declaration.NomSignataire);
    }

    [Fact]
    public async Task FormationsEtReferences_SontScopeesAuCandidat_UnAutreCandidatNeLesVoitPas()
    {
        var userId = $"cv-test-scope-{Guid.NewGuid()}";
        var autreUserId = $"cv-test-scope-autre-{Guid.NewGuid()}";
        var candidateProfileId = await Service.GetOrCreateProfileIdAsync(userId);
        var autreCandidateProfileId = await Service.GetOrCreateProfileIdAsync(autreUserId);

        await Service.SaveFormationAsync(candidateProfileId, id: null, new CvFormation { Etablissement = "École du candidat" });
        await Service.SaveReferenceAsync(candidateProfileId, id: null, new CvReference { NomPrenom = "Référence du candidat" });

        var cvAutreCandidat = await Service.GetCvAsync(autreCandidateProfileId);
        Assert.Empty(cvAutreCandidat.Formations);
        Assert.Empty(cvAutreCandidat.References);

        var cvCandidat = await Service.GetCvAsync(candidateProfileId);
        Assert.Single(cvCandidat.Formations);
        Assert.Single(cvCandidat.References);
    }
}
