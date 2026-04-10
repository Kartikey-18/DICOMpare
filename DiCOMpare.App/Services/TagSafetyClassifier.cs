using FellowOakDicom;
using DiCOMpare.Models;

namespace DiCOMpare.Services;

public static class TagSafetyClassifier
{
    // Tags that are safe to modify - they only affect routing/identification, not image interpretation
    private static readonly HashSet<DicomTag> SafeTags = new()
    {
        // Patient identification
        DicomTag.PatientName,
        DicomTag.PatientID,
        DicomTag.OtherPatientIDsRETIRED,
        DicomTag.PatientBirthDate,
        DicomTag.PatientSex,
        DicomTag.PatientAge,
        DicomTag.PatientWeight,
        DicomTag.PatientSize,

        // Study identification / routing
        DicomTag.AccessionNumber,
        DicomTag.StudyID,
        DicomTag.StudyDescription,
        DicomTag.StudyDate,
        DicomTag.StudyTime,
        DicomTag.StudyInstanceUID,
        DicomTag.SeriesDescription,
        DicomTag.SeriesNumber,
        DicomTag.SeriesInstanceUID,
        DicomTag.SeriesDate,
        DicomTag.SeriesTime,

        // Institutional
        DicomTag.InstitutionName,
        DicomTag.InstitutionAddress,
        DicomTag.InstitutionalDepartmentName,
        DicomTag.ReferringPhysicianName,
        DicomTag.PerformingPhysicianName,
        DicomTag.OperatorsName,
        DicomTag.StationName,
        DicomTag.ManufacturerModelName,
        DicomTag.Manufacturer,
        DicomTag.DeviceSerialNumber,
        DicomTag.SoftwareVersions,

        // Instance identification
        DicomTag.InstanceNumber,
        DicomTag.InstanceCreationDate,
        DicomTag.InstanceCreationTime,
        DicomTag.ContentDate,
        DicomTag.ContentTime,
        DicomTag.AcquisitionDate,
        DicomTag.AcquisitionTime,
        DicomTag.AcquisitionNumber,
    };

    // Tags that affect how software interprets images - NEVER safe to change
    private static readonly HashSet<DicomTag> UnsafeTags = new()
    {
        // SOP Class determines how the entire object is interpreted
        DicomTag.SOPClassUID,
        DicomTag.Modality,
        DicomTag.TransferSyntaxUID,

        // Pixel data and encoding
        DicomTag.PixelData,
        DicomTag.Rows,
        DicomTag.Columns,
        DicomTag.BitsAllocated,
        DicomTag.BitsStored,
        DicomTag.HighBit,
        DicomTag.PixelRepresentation,
        DicomTag.SamplesPerPixel,
        DicomTag.PhotometricInterpretation,
        DicomTag.PlanarConfiguration,
        DicomTag.NumberOfFrames,

        // Spatial calibration - affects all measurements
        DicomTag.PixelSpacing,
        DicomTag.ImagerPixelSpacing,
        DicomTag.ImageOrientationPatient,
        DicomTag.ImagePositionPatient,
        DicomTag.SliceThickness,
        DicomTag.SpacingBetweenSlices,
        DicomTag.SliceLocation,

        // Ultrasound-specific calibration
        DicomTag.SequenceOfUltrasoundRegions,
        DicomTag.PhysicalDeltaX,
        DicomTag.PhysicalDeltaY,
        DicomTag.PhysicalUnitsXDirection,
        DicomTag.PhysicalUnitsYDirection,
        DicomTag.RegionSpatialFormat,
        DicomTag.RegionDataType,
        DicomTag.RegionFlags,
        DicomTag.RegionLocationMinX0,
        DicomTag.RegionLocationMinY0,
        DicomTag.RegionLocationMaxX1,
        DicomTag.RegionLocationMaxY1,
        DicomTag.ReferencePixelX0,
        DicomTag.ReferencePixelY0,
        DicomTag.ReferencePixelPhysicalValueX,
        DicomTag.ReferencePixelPhysicalValueY,

        // Window/level - affects display interpretation
        DicomTag.WindowCenter,
        DicomTag.WindowWidth,
        DicomTag.RescaleIntercept,
        DicomTag.RescaleSlope,
        DicomTag.RescaleType,

        // Frame and cine data
        DicomTag.FrameTime,
        DicomTag.FrameTimeVector,
        DicomTag.RecommendedDisplayFrameRate,
        DicomTag.CineRate,
        DicomTag.HeartRate,
        DicomTag.FrameIncrementPointer,
    };

    // Tags that need careful consideration
    private static readonly HashSet<DicomTag> CautionTags = new()
    {
        DicomTag.SOPInstanceUID,
        DicomTag.MediaStorageSOPClassUID,
        DicomTag.MediaStorageSOPInstanceUID,
        DicomTag.ImplementationClassUID,
        DicomTag.ImplementationVersionName,
        DicomTag.SpecificCharacterSet,
        DicomTag.ImageType,
        DicomTag.Laterality,
        DicomTag.BodyPartExamined,
        DicomTag.ProtocolName,
        DicomTag.ContrastBolusAgent,
        DicomTag.ScanningSequence,
        DicomTag.SequenceVariant,
        DicomTag.LossyImageCompression,
        DicomTag.LossyImageCompressionRatio,
        DicomTag.BurnedInAnnotation,
    };

    public static (TagSafety safety, string reason) Classify(DicomTag tag)
    {
        if (SafeTags.Contains(tag))
            return (TagSafety.Safe, "Identification/routing only. No impact on image interpretation or measurements.");

        if (UnsafeTags.Contains(tag))
            return (TagSafety.Unsafe, GetUnsafeReason(tag));

        if (CautionTags.Contains(tag))
            return (TagSafety.Caution, GetCautionReason(tag));

        // Private tags (odd group number) are vendor-specific
        if (tag.Group % 2 != 0)
            return (TagSafety.Caution, "Private/vendor-specific tag. May contain calibration or processing data. Review carefully.");

        // Default unknown tags to caution
        return (TagSafety.Caution, "Unknown impact. Review before modifying.");
    }

    private static string GetUnsafeReason(DicomTag tag)
    {
        if (tag == DicomTag.SOPClassUID)
            return "Defines the type of DICOM object. Changing this makes software interpret the data using wrong algorithms. WILL affect measurements.";
        if (tag == DicomTag.Modality)
            return "Determines which measurement tools and algorithms are applied. Changing this causes incorrect clinical interpretation.";
        if (tag == DicomTag.TransferSyntaxUID)
            return "Defines how pixel data is encoded. Mismatch will corrupt or fail to render images.";
        if (tag == DicomTag.PixelSpacing || tag == DicomTag.ImagerPixelSpacing)
            return "Directly used for distance/area measurements. Wrong values = wrong measurements = wrong diagnosis.";
        if (tag == DicomTag.SequenceOfUltrasoundRegions || tag == DicomTag.PhysicalDeltaX || tag == DicomTag.PhysicalDeltaY)
            return "Ultrasound calibration data. Used for all echo measurements (EF, volumes, velocities). Changing this invalidates every measurement.";
        if (tag.Group == 0x0028 && tag.Element <= 0x0015)
            return "Pixel encoding parameter. Mismatch with actual pixel data will corrupt the image.";
        if (tag == DicomTag.RescaleSlope || tag == DicomTag.RescaleIntercept)
            return "Transforms stored pixel values to meaningful units (e.g., Hounsfield). Affects quantitative analysis.";
        if (tag == DicomTag.WindowCenter || tag == DicomTag.WindowWidth)
            return "Display settings that affect how the image appears. May alter clinical interpretation of contrast.";

        return "Affects image interpretation, pixel encoding, or measurement calibration. Modifying will produce incorrect clinical results.";
    }

    private static string GetCautionReason(DicomTag tag)
    {
        if (tag == DicomTag.SOPInstanceUID)
            return "Unique instance identifier. Must be regenerated (not copied) if creating a new instance, to avoid conflicts in PACS.";
        if (tag == DicomTag.ImageType)
            return "Describes image characteristics (ORIGINAL/DERIVED, PRIMARY/SECONDARY). May affect how software processes the image.";
        if (tag == DicomTag.SpecificCharacterSet)
            return "Defines text encoding. Mismatch can corrupt patient names and descriptions with special characters.";
        if (tag == DicomTag.LossyImageCompression)
            return "Indicates if image quality has been reduced. Misrepresenting this may violate regulatory requirements.";

        return "May affect processing or compliance. Review the specific context before modifying.";
    }
}
