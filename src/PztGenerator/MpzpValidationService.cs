namespace PztGenerator;

public static class MpzpValidationService
{
    public static List<ValidationMessage> Validate(
        MpzpRequirements requirements,
        double siteArea,
        double buildingFootprint,
        double bioArea,
        double grossFloorArea,
        double buildingCoveragePercent,
        double bioPercent,
        double intensity)
    {
        var messages = new List<ValidationMessage>();

        if (siteArea <= 0)
        {
            messages.Add(new ValidationMessage("Brak powierzchni dzialki. Dodaj obszar z typem `Granica terenu / dzialki`.", ValidationSeverity.Error));
            return messages;
        }

        if (!requirements.HasAnyValue)
        {
            messages.Add(new ValidationMessage("Nie wpisano wymagan MPZP. Uzyj przycisku `MPZP`.", ValidationSeverity.Info));
            return messages;
        }

        if (buildingFootprint == 0)
        {
            messages.Add(new ValidationMessage("Brak obszarow zabudowy. Uzyj `Przypisz typ` dla obrysow budynkow.", ValidationSeverity.Warning));
        }

        ValidateMinBio(requirements, siteArea, bioArea, bioPercent, messages);
        ValidateMinBuildingCoverage(requirements, siteArea, buildingFootprint, buildingCoveragePercent, messages);
        ValidateMaxBuildingCoverage(requirements, siteArea, buildingFootprint, buildingCoveragePercent, messages);
        ValidateMinIntensity(requirements, siteArea, grossFloorArea, intensity, messages);
        ValidateMaxIntensity(requirements, siteArea, grossFloorArea, intensity, messages);

        if (messages.Count == 0)
        {
            messages.Add(new ValidationMessage("Warunki MPZP sa spelnione.", ValidationSeverity.Success));
        }

        return messages;
    }

    private static void ValidateMinBio(MpzpRequirements requirements, double siteArea, double bioArea, double bioPercent, List<ValidationMessage> messages)
    {
        if (requirements.MinBioPercent <= 0)
        {
            return;
        }

        double requiredBioArea = siteArea * requirements.MinBioPercent / 100;

        if (bioPercent < requirements.MinBioPercent)
        {
            messages.Add(new ValidationMessage($"PBC: {bioArea:N2} m2 / {siteArea:N2} m2 = {bioPercent:N2}%, wymagane min. {requirements.MinBioPercent:N2}% ({requiredBioArea:N2} m2) -> warunek niespelniony, brakuje {requiredBioArea - bioArea:N2} m2.", ValidationSeverity.Error));
            return;
        }

        messages.Add(new ValidationMessage($"PBC: {bioArea:N2} m2 / {siteArea:N2} m2 = {bioPercent:N2}%, wymagane min. {requirements.MinBioPercent:N2}% ({requiredBioArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
    }

    private static void ValidateMinBuildingCoverage(MpzpRequirements requirements, double siteArea, double buildingFootprint, double buildingCoveragePercent, List<ValidationMessage> messages)
    {
        if (requirements.MinBuildingCoveragePercent <= 0)
        {
            return;
        }

        double requiredBuildingArea = siteArea * requirements.MinBuildingCoveragePercent / 100;

        if (buildingCoveragePercent < requirements.MinBuildingCoveragePercent)
        {
            messages.Add(new ValidationMessage($"Pow. zabudowy min.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, wymagane min. {requirements.MinBuildingCoveragePercent:N2}% ({requiredBuildingArea:N2} m2) -> warunek niespelniony, brakuje {requiredBuildingArea - buildingFootprint:N2} m2.", ValidationSeverity.Error));
            return;
        }

        messages.Add(new ValidationMessage($"Pow. zabudowy min.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, wymagane min. {requirements.MinBuildingCoveragePercent:N2}% ({requiredBuildingArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
    }

    private static void ValidateMaxBuildingCoverage(MpzpRequirements requirements, double siteArea, double buildingFootprint, double buildingCoveragePercent, List<ValidationMessage> messages)
    {
        if (requirements.MaxBuildingCoveragePercent <= 0)
        {
            return;
        }

        double allowedBuildingArea = siteArea * requirements.MaxBuildingCoveragePercent / 100;

        if (buildingCoveragePercent > requirements.MaxBuildingCoveragePercent)
        {
            messages.Add(new ValidationMessage($"Pow. zabudowy max.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, dopuszczalne max. {requirements.MaxBuildingCoveragePercent:N2}% ({allowedBuildingArea:N2} m2) -> warunek niespelniony, za duzo o {buildingFootprint - allowedBuildingArea:N2} m2.", ValidationSeverity.Error));
            return;
        }

        messages.Add(new ValidationMessage($"Pow. zabudowy max.: {buildingFootprint:N2} m2 / {siteArea:N2} m2 = {buildingCoveragePercent:N2}%, dopuszczalne max. {requirements.MaxBuildingCoveragePercent:N2}% ({allowedBuildingArea:N2} m2) -> warunek spelniony.", ValidationSeverity.Success));
    }

    private static void ValidateMinIntensity(MpzpRequirements requirements, double siteArea, double grossFloorArea, double intensity, List<ValidationMessage> messages)
    {
        if (requirements.MinIntensity <= 0)
        {
            return;
        }

        double requiredGrossFloorArea = siteArea * requirements.MinIntensity;

        if (intensity < requirements.MinIntensity)
        {
            messages.Add(new ValidationMessage($"Intensywnosc min.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, wymagane min. {requirements.MinIntensity:N2} ({requiredGrossFloorArea:N2} m2 pow. calk.) -> warunek niespelniony, brakuje {requiredGrossFloorArea - grossFloorArea:N2} m2.", ValidationSeverity.Error));
            return;
        }

        messages.Add(new ValidationMessage($"Intensywnosc min.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, wymagane min. {requirements.MinIntensity:N2} ({requiredGrossFloorArea:N2} m2 pow. calk.) -> warunek spelniony.", ValidationSeverity.Success));
    }

    private static void ValidateMaxIntensity(MpzpRequirements requirements, double siteArea, double grossFloorArea, double intensity, List<ValidationMessage> messages)
    {
        if (requirements.MaxIntensity <= 0)
        {
            return;
        }

        double allowedGrossFloorArea = siteArea * requirements.MaxIntensity;

        if (intensity > requirements.MaxIntensity)
        {
            messages.Add(new ValidationMessage($"Intensywnosc max.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, dopuszczalne max. {requirements.MaxIntensity:N2} ({allowedGrossFloorArea:N2} m2 pow. calk.) -> warunek niespelniony, za duzo o {grossFloorArea - allowedGrossFloorArea:N2} m2.", ValidationSeverity.Error));
            return;
        }

        messages.Add(new ValidationMessage($"Intensywnosc max.: {grossFloorArea:N2} m2 pow. calk. / {siteArea:N2} m2 = {intensity:N2}, dopuszczalne max. {requirements.MaxIntensity:N2} ({allowedGrossFloorArea:N2} m2 pow. calk.) -> warunek spelniony.", ValidationSeverity.Success));
    }
}
