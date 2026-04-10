namespace Zumo.Labyrinth.Contracts;

public interface INavigationMotionService
{
    Task RotateInPlace(int angleDegrees);
    Task<SafeDriveStopReason> DriveStraightSafelyAsync(int targetDistanceMm);
    Task<CollisionAssessment> AssessCollisionRiskAsync();
    Task<bool> ResolveSideCollisionRiskAsync();
    Task CalibrateAgainstFrontWallAsync();
}
