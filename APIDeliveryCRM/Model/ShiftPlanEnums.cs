namespace APIDeliveryCRM.Model;

public enum ShiftPlanStatus : byte
{
    Draft = 1,

    Active = 2,

    Completed = 3,

    Replanned = 4,

    Cancelled = 5
}

public enum ShiftAssignmentStage : byte
{
    LocalUrban = 1,

    PickupToHub = 2,

    HubToHub = 3,

    HubToRecipient = 4,

    DirectIntercity = 5
}

public enum ShiftAssignmentStatus : byte
{
    Pending = 1,

    InProgress = 2,

    Done = 3,

    Skipped = 4,

    Reassigned = 5
}

public enum OrderHandoffStage : byte
{
    None = 0,

    AwaitingHubDropOff = 1,

    AtHub = 2,

    LastMileInProgress = 3,

    Completed = 4
}
