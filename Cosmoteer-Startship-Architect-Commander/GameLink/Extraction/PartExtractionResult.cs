using Cosmoteer.Data;

namespace Cosmoteer.GameLink.Extraction;

public enum PartExtractionStatus
{
    /// <summary>The file has no <c>Part</c> with a <c>PartCrew</c> component, e.g. a base file, strings or a non-part rules file.</summary>
    NotACrewedPart,

    /// <summary>Crew data was extracted and can be used for an override.</summary>
    Extracted,

    /// <summary>The file is a crewed part, but something required is missing or couldn't be resolved.</summary>
    Failed
}

/// <summary>
///     The outcome of reading one .rules file. <see cref="Problems" /> explains failures and also lists notes for extracted parts (e.g. no HighPriorityPrerequisites).
/// </summary>
public sealed record PartExtractionResult(string File, PartExtractionStatus Status, CrewData? CrewData, IReadOnlyList<string> Problems)
{
    public static PartExtractionResult NotACrewedPart(string file) => new(file, PartExtractionStatus.NotACrewedPart, null, []);
    public static PartExtractionResult Failed(string file, params string[] problems) => new(file, PartExtractionStatus.Failed, null, problems);
}
