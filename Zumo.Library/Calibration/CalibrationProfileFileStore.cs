using System.Text.Json;

namespace Zumo.Library.Calibration;

public sealed class CalibrationProfileFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CalibrationProfileFileStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path must not be null or whitespace.", nameof(filePath));
        }

        FilePath = filePath;
    }

    public string FilePath { get; }

    public static string GetDefaultPath()
    {
        string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string directory = Path.Combine(homePath, ".zumo");
        string filePath = Path.Combine(directory, "calibration.json");
        return filePath;
    }

    public CalibrationProfile LoadOrDefault()
    {
        bool loaded = TryLoad(out CalibrationProfile profile, out _);
        return loaded ? profile : CalibrationProfile.Default;
    }

    public bool TryLoad(out CalibrationProfile profile, out string? error)
    {
        profile = CalibrationProfile.Default;
        error = null;

        try
        {
            if (!File.Exists(FilePath))
            {
                return false;
            }

            string json = File.ReadAllText(FilePath);
            CalibrationProfile? candidate = JsonSerializer.Deserialize<CalibrationProfile>(json, JsonOptions);
            if (candidate is null)
            {
                error = "Calibration profile JSON is empty or invalid.";
                return false;
            }

            bool valid = TryValidate(candidate, out string validationError);
            if (!valid)
            {
                error = validationError;
                return false;
            }

            profile = candidate;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            error = ex.Message;
            return false;
        }
    }

    public void Save(CalibrationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        bool valid = TryValidate(profile, out string validationError);
        if (!valid)
        {
            throw new InvalidOperationException($"Calibration profile is invalid: {validationError}");
        }

        string directory = Path.GetDirectoryName(FilePath) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(profile, JsonOptions);
        string tempFilePath = FilePath + ".tmp";

        File.WriteAllText(tempFilePath, json);
        File.Move(tempFilePath, FilePath, overwrite: true);
    }

    private static bool TryValidate(CalibrationProfile profile, out string error)
    {
        if (profile.Version <= 0)
        {
            error = "Version must be greater than 0.";
            return false;
        }

        if (profile.DistanceCalibrationFactor == 0)
        {
            error = "DistanceCalibrationFactor must be greater than 0.";
            return false;
        }

        if (profile.RotationCalibrationFactor == 0)
        {
            error = "RotationCalibrationFactor must be greater than 0.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}