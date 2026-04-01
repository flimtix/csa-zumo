namespace Zumo.Labyrinth.Navigation.Models;

public sealed class NavigationDistanceSnapshot
{
    public static NavigationDistanceSnapshot Empty { get; } = new(0, 0, 0, 0, false);

    public NavigationDistanceSnapshot(
        int frontDistanceMm,
        int rightDistanceMm,
        int frontLeftDistanceMm,
        int frontRightDistanceMm,
        bool hasAnySample)
    {
        FrontDistanceMm = frontDistanceMm;
        RightDistanceMm = rightDistanceMm;
        FrontLeftDistanceMm = frontLeftDistanceMm;
        FrontRightDistanceMm = frontRightDistanceMm;
        HasAnySample = hasAnySample;
    }

    public int FrontDistanceMm { get; }
    public int RightDistanceMm { get; }
    public int FrontLeftDistanceMm { get; }
    public int FrontRightDistanceMm { get; }
    public bool HasAnySample { get; }
}
