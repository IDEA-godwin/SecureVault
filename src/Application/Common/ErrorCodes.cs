namespace SecureVault.Application.Common;

public static class ErrorCodes
{
    // Account-related errors
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string DuplicateAccount = "DUPLICATE_ACCOUNT";
    public const string AccountHasBalance = "ACCOUNT_HAS_BALANCE";

    // Transaction-related errors
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    public const string InvalidTransferAmount = "INVALID_TRANSFER_AMOUNT";
    public const string SelfTransferNotAllowed = "SELF_TRANSFER_NOT_ALLOWED";

    // General errors
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string OperationFailed = "OPERATION_FAILED";
    public const string ConstraintViolation = "CONSTRAINT_VIOLATION";
}
