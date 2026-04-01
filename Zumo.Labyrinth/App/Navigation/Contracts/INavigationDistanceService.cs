namespace Zumo.Labyrinth.Navigation.Contracts;

public interface INavigationDistanceService
{
    Task<NavigationDistanceSnapshot> CaptureAsync();
    Task<int> GetFrontDistanceMmAsync();
    Task<int> GetRightDistanceMmAsync();
    Task<int> GetFrontLeftDistanceMmAsync();
    Task<int> GetFrontRightDistanceMmAsync();
}
