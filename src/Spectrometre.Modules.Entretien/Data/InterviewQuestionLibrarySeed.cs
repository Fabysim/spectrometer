using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Data;

/// <summary>
/// Seed idempotent du catalogue public — contenu porté mot pour mot depuis
/// <c>mvp SeedDatabase.SeedInterviewQuestionLibraryAsync</c>.
/// </summary>
public static class InterviewQuestionLibrarySeed
{
    public static async Task EnsureSeededAsync(
        EntretienCatalogDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (await db.InterviewQuestionCategories.AnyAsync(cancellationToken))
            return;

        var cat1 = new InterviewQuestionCategory { Name = "Motivation et projet professionnel", SortOrder = 1 };
        var cat2 = new InterviewQuestionCategory { Name = "Compétences et expérience", SortOrder = 2 };
        var cat3 = new InterviewQuestionCategory { Name = "Comportement et collaboration", SortOrder = 3 };
        db.InterviewQuestionCategories.AddRange(cat1, cat2, cat3);
        await db.SaveChangesAsync(cancellationToken);

        var s1a = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat1.Id,
            Name = "Adhésion au poste et à l'entreprise",
            SortOrder = 1
        };
        var s1b = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat1.Id,
            Name = "Projection et attentes",
            SortOrder = 2
        };
        var s2a = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat2.Id,
            Name = "Savoir-faire et savoirs",
            SortOrder = 1
        };
        var s2b = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat2.Id,
            Name = "Situations et réalisations",
            SortOrder = 2
        };
        var s3a = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat3.Id,
            Name = "Travail en équipe",
            SortOrder = 1
        };
        var s3b = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat3.Id,
            Name = "Communication",
            SortOrder = 2
        };
        var s3c = new InterviewQuestionSubCategory
        {
            InterviewQuestionCategoryId = cat3.Id,
            Name = "Résilience et organisation",
            SortOrder = 3
        };
        db.InterviewQuestionSubCategories.AddRange(s1a, s1b, s2a, s2b, s3a, s3b, s3c);
        await db.SaveChangesAsync(cancellationToken);

        var questions = new List<InterviewQuestion>();
        void AddQ(InterviewQuestionSubCategory sub, int order, string text, string? expected = null) =>
            questions.Add(new InterviewQuestion
            {
                InterviewQuestionSubCategoryId = sub.Id,
                Text = text,
                ExpectedElements = expected,
                SortOrder = order
            });

        AddQ(s1a, 1, "Qu'est-ce qui vous attire dans ce poste et dans notre secteur d'activité ?",
            "Motivation sincère, recherche documentaire sur l'entreprise.");
        AddQ(s1a, 2, "Où vous voyez-vous dans deux à trois ans si nous travaillons ensemble ?",
            "Projection réaliste, alignement avec le poste.");
        AddQ(s1a, 3, "Quels critères sont importants pour vous dans votre prochain emploi ?",
            "Conditions de travail, apprentissage, équipe, impact.");

        AddQ(s1b, 1, "Qu'attendez-vous concrètement de votre manager au quotidien ?",
            "Clarté, feedback, autonomie.");
        AddQ(s1b, 2, "Comment définissez-vous une « bonne journée de travail » ?",
            "Indicateurs personnels de satisfaction.");
        AddQ(s1b, 3, "Y a-t-il des aspects du poste sur lesquels vous souhaitez être formé en priorité ?",
            "Besoins de montée en compétences.");

        AddQ(s2a, 1, "Décrivez les compétences clés que vous mobilisez le plus souvent dans votre métier.",
            "Liste structurée, vocabulaire métier.");
        AddQ(s2a, 2, "Quels outils ou méthodes maîtrisez-vous pour [domaine du poste] ?",
            "Outils concrets, niveau d'aisance.");
        AddQ(s2a, 3, "Comment restez-vous à jour dans votre domaine ?",
            "Veille, formations, réseau.");
        AddQ(s2a, 4, "Quelle est la tâche la plus complexe que vous avez menée avec succès ?",
            "Périmètre, difficulté, résultat.");
        AddQ(s2a, 5, "Comment priorisez-vous lorsque plusieurs demandes arrivent en même temps ?",
            "Critères d'urgence / importance.");
        AddQ(s2a, 6, "Donnez un exemple où vous avez dû apprendre rapidement quelque chose de nouveau.",
            "Méthode d'apprentissage, délai.");
        AddQ(s2a, 7, "Comment assurez-vous la qualité de votre travail avant de le livrer ?",
            "Contrôles, relecture, tests.");
        AddQ(s2a, 8, "Quelles normes ou réglementations impactent votre métier et comment les appliquez-vous ?",
            "Conscience des contraintes légales / qualité.");

        AddQ(s2b, 1, "Parlez-moi d'une réussite dont vous êtes particulièrement fier(e).",
            "Contexte, action, résultat mesurable (STAR).");
        AddQ(s2b, 2, "Décrivez une situation difficile avec un collègue ou un client et comment vous l'avez gérée.",
            "Écoute, négociation, escalade si besoin.");
        AddQ(s2b, 3, "Un exemple où vous n'avez pas atteint l'objectif : qu'avez-vous retenu ?",
            "Honnêteté, apprentissage.");
        AddQ(s2b, 4, "Comment avez-vous contribué à améliorer un processus ou un indicateur ?",
            "Initiative, mesure d'impact.");
        AddQ(s2b, 5, "Racontez un conflit d'intérêts ou de priorités entre deux parties : votre rôle.",
            "Médiation, transparence.");
        AddQ(s2b, 6, "Exemple de travail sous forte contrainte de délai.",
            "Organisation, arbitrages.");
        AddQ(s2b, 7, "Situation où vous avez dû convaincre sans autorité hiérarchique.",
            "Argumentation, preuves.");
        AddQ(s2b, 8, "Comment mesurez-vous la réussite de vos actions dans votre poste actuel ou passé ?",
            "KPI, retours clients, équipe.");

        AddQ(s3a, 1, "Quel rôle préférez-vous jouer dans une équipe projet ?",
            "Coordinateur, expert, facilitateur…");
        AddQ(s3a, 2, "Comment réagissez-vous lorsqu'un membre de l'équipe ne tient pas ses engagements ?",
            "Dialogue direct, soutien, escalade.");
        AddQ(s3a, 3, "Donnez un exemple de collaboration interservices réussie.",
            "Objectif commun, communication.");
        AddQ(s3a, 4, "Comment accueillez-vous les critiques sur votre travail ?",
            "Ouverture, plan d'amélioration.");

        AddQ(s3b, 1, "Comment adaptez-vous votre message à un interlocuteur non technique ?",
            "Pédagogie, exemples simples.");
        AddQ(s3b, 2, "Quelle est votre manière de synthétiser une information complexe à l'oral ?",
            "Structure, clarté.");
        AddQ(s3b, 3, "Comment gérez-vous les malentendus par écrit (e-mail, messagerie) ?",
            "Clarification, ton professionnel.");

        AddQ(s3c, 1, "Comment organisez-vous votre semaine lorsque la charge est élevée ?",
            "Outils, routines, limites.");
        AddQ(s3c, 2, "Décrivez comment vous gérez le stress dans un contexte incertain.",
            "Strategies personnelles, exemple.");

        db.InterviewQuestions.AddRange(questions);
        await db.SaveChangesAsync(cancellationToken);
    }
}
