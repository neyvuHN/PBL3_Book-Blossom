// BookBlossom.Core/Enums/ActionType.cs
namespace BookBlossom.Core.Enums
{
    public enum ActionType : byte
    {
        USER_LOGIN = 0,
        LOGIN_FAILED = 1,
        USER_LOGOUT = 2,
        CREATE_STAFF_ACCOUNT = 3,
        UPDATE_STAFF_ACCOUNT = 4,
        DELETE_STAFF_ACCOUNT = 5,
        LOCK_ACCOUNT = 6,
        UNLOCK_ACCOUNT = 7,
        EXPORT = 8,
        CREATE = 9,
        UPDATE = 10,
        DELETE = 11
    }
}