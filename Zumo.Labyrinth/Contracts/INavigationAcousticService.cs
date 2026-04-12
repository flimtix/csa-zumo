namespace Zumo.Labyrinth.Contracts;

public interface INavigationAcousticService
{
    ValueTask PlayMarkDetectedToneAsync();
    ValueTask PlayMazeSolvedMelodyAsync();
}
