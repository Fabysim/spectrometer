namespace Spectrometre.Core.Modules;

/// <summary>
/// Trace, pour un SUJET donné (une entreprise, un candidat, et à terme un profil indépendant — voir
/// <see cref="ModuleActivationSubjectType"/>), quels modules sont activés (table de registre, schéma
/// <c>core</c>). Remplace l'ancien couplage exclusif à <c>CompanyId</c> : la clé logique est désormais
/// (<see cref="SubjectType"/>, <see cref="SubjectId"/>, <see cref="ModuleCode"/>).
/// </summary>
/// <remarks>
/// <see cref="SubjectId"/> est une simple référence par identifiant (jamais une contrainte de clé
/// étrangère) — pour <see cref="ModuleActivationSubjectType.Company"/> c'est <c>Company.Id</c> (Core) ;
/// pour <see cref="ModuleActivationSubjectType.Candidate"/> c'est <c>CandidateProfileId</c> (ProfilCandidat,
/// un module — le noyau ne peut pas y référencer un type, même principe que <c>CandidatureIndexEntry</c>).
/// </remarks>
public sealed class ModuleActivation
{
    public int Id { get; set; }
    public ModuleActivationSubjectType SubjectType { get; set; }
    public int SubjectId { get; set; }
    public required string ModuleCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset ActivatedAt { get; set; } = DateTimeOffset.UtcNow;
}
