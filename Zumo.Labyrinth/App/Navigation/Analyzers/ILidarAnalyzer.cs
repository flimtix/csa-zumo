namespace Zumo.Labyrinth.Navigation.Analyzers;

/// <summary>
/// Strategy interface for LiDAR data analysis and gap detection.
/// Implementations are domain-specific: a labyrinth solver, obstacle avoider, or mapper
/// would each implement this differently without changing the sensor driver.
/// </summary>
public interface ILidarAnalyzer
{
    LidarSector AnalyzeSector(LidarSnapshot snapshot, int startAngleDegrees, int endAngleDegrees);
}
