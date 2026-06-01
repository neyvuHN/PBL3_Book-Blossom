// BookBlossom.Core/Enums/ActionType.cs
namespace BookBlossom.Core.Enums
{
    public enum ActionType : byte
    {
        ADMIN_LOGIN = 0,
        LOGIN_FAILED = 1,
        ADMIN_LOGOUT = 2,
        LOCK_ACCOUNT = 3,
        UNLOCK_ACCOUNT = 4,
        EXPORT = 5
    }
}