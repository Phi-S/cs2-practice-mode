using ErrorOr;

namespace Cs2PracticeMode.Constants;

public static class Errors
{
    public static string ErrorMessage<TValue>(this ErrorOr<TValue> error)
    {
        return error.FirstError.Description;
    }

    public static Error Fail(string description = "A failure has occurred.")
    {
        return Error.Failure(description: description);
    }

    public static Error PlayerNullOrNotValid()
    {
        return Error.Custom((int)ErrorType.Conflict,
            "General.PlayerNullOrNotValid",
            "Player is null or not valid");
    }
}