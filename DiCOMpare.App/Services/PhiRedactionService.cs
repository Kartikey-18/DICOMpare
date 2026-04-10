using FellowOakDicom;

namespace DiCOMpare.Services;

public static class PhiRedactionService
{
    // DICOM tags that contain Protected Health Information per HIPAA Safe Harbor
    private static readonly HashSet<string> PhiTagIds = new(StringComparer.OrdinalIgnoreCase)
    {
        DicomTag.PatientName.ToString(),
        DicomTag.PatientID.ToString(),
        DicomTag.PatientBirthDate.ToString(),
        DicomTag.PatientSex.ToString(),
        DicomTag.PatientAge.ToString(),
        DicomTag.PatientWeight.ToString(),
        DicomTag.PatientSize.ToString(),
        DicomTag.PatientAddress.ToString(),
        DicomTag.PatientTelephoneNumbers.ToString(),
        DicomTag.OtherPatientIDsRETIRED.ToString(),
        DicomTag.AccessionNumber.ToString(),
        DicomTag.ReferringPhysicianName.ToString(),
        DicomTag.PerformingPhysicianName.ToString(),
        DicomTag.OperatorsName.ToString(),
        DicomTag.InstitutionName.ToString(),
        DicomTag.InstitutionAddress.ToString(),
        DicomTag.InstitutionalDepartmentName.ToString(),
        DicomTag.StationName.ToString(),
        DicomTag.StudyID.ToString(),
        DicomTag.DeviceSerialNumber.ToString(),
    };

    public static bool IsPhiTag(string tagId) => PhiTagIds.Contains(tagId);

    public static string Redact(string tagId, string value)
    {
        if (!IsPhiTag(tagId) || string.IsNullOrEmpty(value))
            return value;

        return "[REDACTED]";
    }
}
