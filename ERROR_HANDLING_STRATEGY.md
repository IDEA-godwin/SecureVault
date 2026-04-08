# Error Handling Strategy - SecureVault

## Overview

This document outlines the comprehensive error handling implementation for SecureVault, covering exception mapping, error codes, and centralized logging for audit trails.

---

## 1. Architecture

### Exception Flow

```
Request → Endpoint → Command/Query Handler → Service Layer
    ↓
Exception Thrown (various types)
    ↓
MediatR Pipeline (catches + rethrows)
    ↓
ProblemDetailsExceptionHandler Middleware
    ↓
RFC 9110 ProblemDetails Response (400/401/403/404/500) + error codes
```

### Middleware Layers

1. **Exception Handler Middleware** - Catches all exceptions, maps to ProblemDetails
2. **Transfer Audit Logging Middleware** - Captures successful transfers and logs to AuditLog table

---

## 2. Exception Mapping

### Custom Exceptions

#### NotFoundException
- **Source**: `SecureVault.Application.Common.Exceptions.NotFoundException`
- **HTTP Status**: 404 Not Found
- **Usage**: When an entity (Account, Transaction) is not found
- **Alias**: Used as `AppNotFoundException` to avoid conflicts with `Ardalis.GuardClauses.NotFoundException`

**Example:**
```csharp
throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] Account with number {accountNumber} not found");
```

### Built-in Exception Types

| Exception Type | HTTP Status | Message | Error Code |
|---|---|---|---|
| `ArgumentException` | 400 | "The request contains invalid parameters." | From message (optional) |
| `ArgumentNullException` | 404 | "The requested entity was not found." | N/A |
| `InvalidOperationException` | 400 | "The operation cannot be completed due to a business rule violation." | Extracted from message |
| `ValidationException` (FluentValidation) | 400 | Property-level errors | N/A |
| `UnauthorizedAccessException` | 401 | "Unauthorized" | N/A |
| `ForbiddenAccessException` | 403 | "Forbidden" | N/A |

---

## 3. Error Codes

Error codes are business-logic error types that help clients handle specific scenarios. They are included in `InvalidOperationException` responses via the extensions field.

### Standard Error Codes

Located in: `SecureVault.Application.Common.ErrorCodes`

#### Account-Related
- `ACCOUNT_NOT_FOUND` - Requested account does not exist
- `DUPLICATE_ACCOUNT` - Email already exists
- `ACCOUNT_HAS_BALANCE` - Cannot delete account with non-zero balance

#### Transaction-Related
- `INSUFFICIENT_FUNDS` - Transfer amount exceeds available balance
- `INVALID_TRANSFER_AMOUNT` - Transfer amount is zero or negative
- `SELF_TRANSFER_NOT_ALLOWED` - Source and destination are the same

#### General
- `INVALID_REQUEST` - Invalid request parameters
- `OPERATION_FAILED` - Generic operation failure
- `CONSTRAINT_VIOLATION` - Database constraint violated

### Error Code Format

Error codes are embedded in exception messages using the `[CODE]` prefix:

```csharp
throw new InvalidOperationException($"[{ErrorCodes.InsufficientFunds}] Insufficient funds. Available balance: 100, Transfer amount: 200");
```

The middleware automatically extracts the error code and includes it in the ProblemDetails response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The operation cannot be completed due to a business rule violation.",
  "status": 400,
  "detail": "Insufficient funds. Available balance: 100, Transfer amount: 200",
  "extensions": {
    "code": "INSUFFICIENT_FUNDS"
  }
}
```

---

## 4. Response Format (RFC 9110)

All error responses follow RFC 9110 ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.X",
  "title": "HTTP Status Text",
  "status": 400,
  "detail": "User-friendly error message",
  "extensions": {
    "code": "ERROR_CODE"
  }
}
```

### Validation Errors (400)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation failures have occurred.",
  "status": 400,
  "errors": {
    "Amount": [
      "Transfer amount must be greater than 0"
    ],
    "ToAccountNumber": [
      "Cannot transfer to the same account"
    ]
  }
}
```

---

## 5. Service Layer Error Handling

All service methods throw specific exceptions with appropriate error codes:

### AccountService

```csharp
// CreateAccountAsync
throw new InvalidOperationException($"[{ErrorCodes.DuplicateAccount}] An account with email {email} already exists");

// CreditAccountByAccountNumberAsync
throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] Account with number {accountNumber} not found");

// DeleteAccountByAccountNumberAsync
throw new InvalidOperationException($"[{ErrorCodes.AccountHasBalance}] Cannot delete account with active balance...");
```

### TransferFundsCommand Handler

```csharp
// Account not found
throw new AppNotFoundException($"[{ErrorCodes.AccountNotFound}] From account with number {request.FromAccountNumber} not found");

// Insufficient funds
throw new InvalidOperationException($"[{ErrorCodes.InsufficientFunds}] Insufficient funds. Available balance: {fromAccount.Balance}, Transfer amount: {request.Amount}");

// Invalid amount
throw new ArgumentException($"[{ErrorCodes.InvalidTransferAmount}] Transfer amount must be greater than zero");
```

---

## 6. Centralized Transfer Audit Logging Middleware

### Purpose

Captures successful transfer operations and automatically logs them to the AuditLog table. This provides a centralized audit trail independent of application logic.

### How It Works

1. **Detection**: Middleware detects if request path contains `/transfer` or `/transfers`
2. **Response Capture**: Wraps response body to read it without disrupting the response
3. **Success Check**: Only logs if HTTP response status is 2xx (successful)
4. **Data Extraction**: Parses TransactionDto from response JSON
5. **Database Logging**: Creates AuditLog record with transaction details
6. **Error Handling**: Wraps in try-catch to prevent logging failures from crashing responses

### Middleware Code Location

`src/Web/Infrastructure/TransferAuditLoggingMiddleware.cs`

### Registration

Added to middleware pipeline in `Program.cs`:

```csharp
app.UseExceptionHandler();
app.UseTransferAuditLogging();  // <-- Centralized transfer logging
app.MapEndpoints(typeof(Program).Assembly);
```

### Logged Fields

- `TransactionId` - UUID of the transaction
- `FromAccountNumber` - Source account
- `ToAccountNumber` - Destination account
- `Amount` - Transfer amount
- `AuditTime` - Server-side timestamp (UTC)
- `Notes` - "Transfer logged via AuditLoggingMiddleware"

### Error Handling

If an error occurs during logging (e.g., JSON parsing, database failure), it is:
1. Caught silently (try-catch in LogTransferAsync)
2. Logged as an Error via ILogger
3. **Not rethrown** - ensures user's successful transfer response is not affected

---

## 7. Testing Error Scenarios

### Test Cases

#### 1. Account Not Found (404)
```bash
POST /api/accounts/transfer
{
  "fromAccountNumber": "INVALID",
  "toAccountNumber": "ACC001",
  "amount": 100
}

Response: 404 ACCOUNT_NOT_FOUND
```

#### 2. Insufficient Funds (400)
```bash
POST /api/accounts/transfer
{
  "fromAccountNumber": "ACC001",
  "toAccountNumber": "ACC002",
  "amount": 10000
}

Response: 400 INSUFFICIENT_FUNDS
```

#### 3. Invalid Amount (400)
```bash
POST /api/accounts/transfer
{
  "fromAccountNumber": "ACC001",
  "toAccountNumber": "ACC002",
  "amount": -50
}

Response: 400 INVALID_REQUEST
```

#### 4. Validation Error (400)
```bash
POST /api/accounts/transfer
{
  "fromAccountNumber": "ACC001",
  "toAccountNumber": "ACC001",  # Same account
  "amount": 100
}

Response: 400 with validation errors
```

#### 5. Successful Transfer (200 + AuditLog)
```bash
POST /api/accounts/transfer
{
  "fromAccountNumber": "ACC001",
  "toAccountNumber": "ACC002",
  "amount": 100,
  "description": "Payment for invoice"
}

Response: 200 with TransactionDto
Result: AuditLog record automatically created
```

### Expected Response Formats

**Error Response (400 - Business Rule)**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The operation cannot be completed due to a business rule violation.",
  "status": 400,
  "detail": "Insufficient funds. Available balance: 100, Transfer amount: 200",
  "extensions": {
    "code": "INSUFFICIENT_FUNDS"
  }
}
```

**Error Response (404 - Not Found)**
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "The specified resource was not found.",
  "status": 404,
  "detail": "From account with number INVALID not found"
}
```

**Success Response (200)**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "description": "Payment for invoice",
  "amount": 100,
  "transactionDate": "2026-04-08T10:30:00Z",
  "transactionType": 1,
  "status": 0,
  "fromAccountNumber": "ACC001",
  "fromAccountName": "Alice",
  "toAccountNumber": "ACC002",
  "toAccountName": "Bob",
  "failureReason": null
}
```

---

## 8. Implementation Files

### Core Files

| File | Purpose |
|------|---------|
| [src/Application/Common/Exceptions/NotFoundException.cs](../src/Application/Common/Exceptions/NotFoundException.cs) | Custom exception for 404 Not Found |
| [src/Application/Common/ErrorCodes.cs](../src/Application/Common/ErrorCodes.cs) | Centralized business error code constants |
| [src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs](../src/Web/Infrastructure/ProblemDetailsExceptionHandler.cs) | Exception → ProblemDetails mapper |
| [src/Web/Infrastructure/TransferAuditLoggingMiddleware.cs](../src/Web/Infrastructure/TransferAuditLoggingMiddleware.cs) | Transfer audit logging middleware |
| [src/Web/Program.cs](../src/Web/Program.cs) | Middleware registration |

### Service Files (Updated)

| File | Changes |
|------|---------|
| [src/Infrastructure/Services/AccountService.cs](../src/Infrastructure/Services/AccountService.cs) | Throws exceptions with error codes |
| [src/Application/Features/Transactions/Commands/TransferFundsCommand.cs](../src/Application/Features/Transactions/Commands/TransferFundsCommand.cs) | Throws exceptions with error codes |
| [src/Application/Features/Accounts/Queries/GetAccountByIdQuery.cs](../src/Application/Features/Accounts/Queries/GetAccountByIdQuery.cs) | Uses AppNotFoundException |
| [src/Application/Features/Transactions/Queries/GetTransactionHistoryQuery.cs](../src/Application/Features/Transactions/Queries/GetTransactionHistoryQuery.cs) | Uses AppNotFoundException |

---

## 9. Best Practices

### When Throwing Exceptions

1. **Use specific exception types** - Don't always use `Exception`
2. **Include error codes for business errors** - Use `[CODE]` prefix format
3. **Provide descriptive messages** - Answer "what went wrong" and "why"
4. **Don't expose sensitive data** - Be careful with error details in production

### When Handling Errors

1. **Let exceptions bubble up** - Don't swallow exceptions silently
2. **Wrap with try-catch for side effects** - Database operations, external APIs
3. **Log before swallowing** - Use ILogger to record context
4. **Return meaningful status codes** - Use appropriate HTTP status

### Database Operation Errors

- Foreign key violations → 404 NotFoundException
- Constraint violations → 400 InvalidOperationException
- Concurrency issues → 409 Conflict (future: not yet implemented)

---

## 10. Future Enhancements

1. **Correlation IDs** - Add request tracing across services
2. **Structured Logging** - Integrate Serilog for structured logs
3. **Circuit Breaker** - Add resilience for external service calls
4. **Rate Limiting Error Codes** - Specific error codes for throttling
5. **Field-Level Error Codes** - Validation error codes per property
6. **Concurrency Conflict Handling** - 409 Conflict responses with retry guidance

---

## Summary

This error handling strategy provides:

✅ **Centralized exception mapping** via middleware  
✅ **Consistent error response format** (RFC 9110 ProblemDetails)  
✅ **Business error codes** for client-side handling  
✅ **Automatic transfer audit logging** via middleware  
✅ **Graceful degradation** (logging failures don't crash responses)  
✅ **Clear error messages** for debugging and user feedback  

All errors are handled consistently across endpoints, services, and commands queries, with automatic audit logging for critical operations like transfers.
