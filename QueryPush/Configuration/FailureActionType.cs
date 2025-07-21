namespace QueryPush.Configuration;

public enum FailureActionType
{
    LogAndContinue,
    Halt,
    SlackAlert,
    EmailAlert
}