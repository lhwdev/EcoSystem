public enum CreatureAction {
    None,
	RestingIdle,
	RestingWalking,
    Exploring,
    GoingToFood,
    GoingToWater,
    GoingToMate,
    Flee,
    Eating,
    Drinking,
    Mating
}


public enum CreatureActionReason {
    Rest,
    Thirsty,
    Hungry,
    PreHungry, // not hungry yet, but just food is near
    Flee,
    Mate
}
