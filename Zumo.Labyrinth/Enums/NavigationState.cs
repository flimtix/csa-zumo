namespace Zumo.Labyrinth.Enums;

public enum NavigationState
{
    MoveToInnerRing = 0,
    FollowCorridor = 1,
    CheckGap = 2,
    InnerRingSolved = 3,
    MoveToOuterRing = 4,
    FollowOuterCorridor = 5,
    ExitLabyrinth = 6,
    LabyrinthSolved = 7,
}
