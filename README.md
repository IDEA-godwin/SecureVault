# SecureVault - Secure Fund Transfer System

A comprehensive clean architecture application for managing accounts and secure fund transfers with audit logging. Built with ASP.NET Core, Entity Framework Core, and MediatR.

## 📋 Features

### 1. Account Management
- **CRUD Operations**: Create, read, update (via credit), and delete user accounts
- **Account Details**: Name, Email, and Balance tracking
- **Initial Balance**: Accounts start with zero balance
- **Validation**: Comprehensive input validation for all account operations

### 2. Transaction Logic
- **Secure Transfers**: Transfer funds between accounts
- **Constraints**:
  - Prevents insufficient funds (automatic rejection)
  - Prevents negative transfers (validation)
  - Prevents transfers to the same account
- **Atomicity**: Transactions use SQL Server transactions to ensure both sides succeed or both fail - no partial transfers

### 3. Transaction History
- **Paginated Retrieval**: Get account transaction history with pagination support
- **Complete History**: Shows both incoming and outgoing transactions
- **Ordering**: Transactions ordered by date (most recent first)
- **Account Filtering**: History specific to an account number

### 4. Audit Logging
- **Automatic Logging**: Background service logs all successful transfers
- **Separate Table**: Audit logs stored in dedicated AuditLog table
- **Details Captured**: Transaction ID, accounts, amount, and audit timestamp
- **Middleware Support**: Automatic logging via TransferAuditLoggingMiddleware
- **Failure Handling**: Failed transfers are NOT logged (only completed transactions)

## 🏗️ Architecture

### Clean Architecture Layers

```
┌─────────────────────────────┐
│      Web (Presentation)     │
│  - Endpoints (OpenAPI)      │
│  - Middleware               │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│   Application (Use Cases)   │
│  - Commands & Queries       │
│  - Handlers (MediatR)       │
│  - Validators (FluentValidation)
│  - DTOs & Mappings          │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│    Domain (Business Logic)  │
│  - Entities                 │
│  - Value Objects            │
│  - Domain Events            │
│  - Exceptions               │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│   Infrastructure (Data)     │
│  - EF Core DbContext        │
│  - SQL Server Integration   │
│  - Background Services      │
└─────────────────────────────┘
```

### Key Architectural Patterns
- **CQRS**: Command Query Responsibility Segregation with MediatR
- **Dependency Injection**: Configured in each layer's DependencyInjection.cs
- **Entity Framework Core**: ORM for database operations
- **Repository Pattern**: Database access through DbContext
- **Error Handling**: Centralized exception mapping via middleware
- **Audit Trail**: Automatic logging of business events

## 📊 Database

### SQL Server Express / LocalDB
- **Connection**: SQL Server 2022 or compatible
- **Database**: SecureVaultDb
- **Migrations**: Automatic schema creation on first run
- **Tables**:
  - `Accounts` - User accounts with balances
  - `Transactions` - Transfer records (FromAccount, ToAccount, Amount)
  - `AuditLogs` - Audit trail entries
  - `Identity` - ASP.NET Core Identity tables

### Running on Linux with Docker

```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Update connection string in `src/Web/appsettings.json` or environment variables if needed.

## 🔒 Error Handling Strategy

Comprehensive centralized exception handling via middleware and exception mapping:

- **ValidationException** (400) - Input validation failures
- **NotFoundException** (404) - Account or resource not found
- **InsufficientFundsException** (400) - Transfer amount exceeds balance
- **InvalidOperationException** (400) - Business rule violations
- **Exception** (500) - Unexpected server errors

See [ERROR_HANDLING_STRATEGY.md](ERROR_HANDLING_STRATEGY.md) for detailed documentation.

## 📁 Project Structure

```
SecureVault/
├── src/
│   ├── Application/              # Use cases, Commands, Queries
│   │   ├── Features/
│   │   │   ├── Accounts/        # Account management
│   │   │   └── Transactions/    # Transfer & audit logic
│   │   ├── Common/
│   │   │   ├── DTOs/            # Data transfer objects
│   │   │   ├── Exceptions/      # Custom exceptions
│   │   │   └── Interfaces/      # Service contracts
│   │   └── DependencyInjection.cs
│   │
│   ├── Domain/                   # Business entities & logic
│   │   ├── Entities/
│   │   │   ├── Account.cs
│   │   │   ├── Transaction.cs
│   │   │   └── AuditLog.cs
│   │   ├── Enums/
│   │   │   └── TransactionStatus.cs
│   │   └── Exceptions/
│   │
│   ├── Infrastructure/           # Data access & services
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Initialiser.cs
│   │   ├── Services/
│   │   │   ├── AccountService.cs
│   │   │   └── AuditLoggingBackgroundService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── Web/                      # API layer
│   │   ├── Endpoints/            # REST endpoints
│   │   │   ├── Accounts.cs
│   │   │   ├── Transactions.cs
│   │   │   └── AuditLogs.cs
│   │   ├── Infrastructure/
│   │   │   └── TransferAuditLoggingMiddleware.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── Shared/                   # Shared utilities
│
├── tests/
│   ├── Application.UnitTests/    # Unit tests
│   ├── Application.FunctionalTests/  # E2E tests
│   │   ├── Features/
│   │   │   ├── Accounts/
│   │   │   │   └── AccountManagementTests.cs
│   │   │   └── Transactions/
│   │   │       ├── TransferFundsTests.cs
│   │   │       ├── TransactionHistoryTests.cs
│   │   │       └── AuditLoggingTests.cs
│   │   └── Infrastructure/
│   │       ├── DatabaseResetter.cs
│   │       ├── TestApp.cs
│   │       └── WebApiFactory.cs
│   ├── Domain.UnitTests/         # Domain tests
│   └── Infrastructure.IntegrationTests/  # Integration tests
│
└── docs/
    ├── README.md                 # This file
    ├── ERROR_HANDLING_STRATEGY.md
    └── MIDDLEWARE_AUDIT_REPORT.md
```

## 🔌 API Endpoints

### Accounts
- `GET /api/accounts` - List all accounts
- `POST /api/accounts` - Create account
- `GET /api/accounts/{accountNumber}` - Get account details
- `PATCH /api/accounts/credit` - Credit (add funds to) account
- `DELETE /api/accounts/{accountNumber}` - Delete account
- `POST /api/accounts/transfer` - Transfer funds

### Transactions
- `GET /api/transactions/history` - Get paginated transaction history
- `GET /api/transactions/audit-logs` - Get audit logs (paginated, filterable by date)

### Documentation
- `GET /openapi/v1.json` - OpenAPI/Swagger specification
- `GET /scalar/v1` - Interactive API documentation (Scalar)

## ✅ Testing

Comprehensive test suite with 45+ test methods covering all requirements:

### Test Classes
1. **AccountManagementTests** (14 tests)
   - CRUD operations validation
   - Account creation, retrieval, deletion
   - Balance management

2. **TransferFundsTests** (11 tests)
   - Successful transfers
   - Insufficient funds detection
   - Atomicity verification
   - Constraint validation

3. **TransactionHistoryTests** (10 tests)
   - Pagination
   - Filtering and ordering
   - History retrieval

4. **AuditLoggingTests** (10 tests)
   - Successful transfer logging
   - Failed transfer non-logging
   - Date filtering
   - No duplicate logs

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Application.FunctionalTests/Application.FunctionalTests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- SQL Server 2022+ or Docker
- Git

### Setup & Run

1. **Clone the repository**
```bash
git clone https://github.com/yourusername/SecureVault.git
cd SecureVault
```

2. **Restore dependencies**
```bash
dotnet restore
```

3. **Configure database** (choose one):

   **Option A: Docker SQL Server (Linux/Mac)**
   ```bash
   docker run -e "ACCEPT_EULA=Y" \
     -e "SA_PASSWORD=YourPassword123!" \
     -p 1433:1433 \
     -d mcr.microsoft.com/mssql/server:2022-latest
   ```

   **Option B: LocalDB (Windows)**
   - Already available on Windows, no setup needed

   **Option C: Custom SQL Server**
   - Update connection string in `src/Web/appsettings.json`

4. **Run the application**
```bash
dotnet run --project src/Web
```

The API will be available at:
- API: `https://localhost:7046`
- Scalar Docs: `https://localhost:7046/scalar/v1`
- OpenAPI: `https://localhost:7046/openapi/v1.json`

5. **Run tests**
```bash
dotnet test
```

## 📦 Key Dependencies

- **ASP.NET Core 10.0** - Web framework
- **Entity Framework Core 10.0** - ORM
- **MediatR 14.1** - CQRS pattern
- **FluentValidation 12.1** - Input validation
- **AutoMapper 16.1** - Object mapping
- **Respawn 7.0** - Database cleanup for tests
- **NUnit 4.5 + Shouldly 4.3** - Testing frameworks

## 🔄 Middleware Pipeline

```
Request →
  HTTPS Redirect →
  CORS →
  Exception Handler (ProblemDetailsExceptionHandler) →
  Transfer Audit Logging Middleware →
  Endpoints →
Response
```

### Middleware Details
- **Exception Handling**: Catches all exceptions and formats as ProblemDetails (RFC 9110)
- **Transfer Audit Logging**: Captures successful transfer responses and logs to AuditLog table
- **Request/Response Tracking**: Complete audit trail of all transfers

## 🔐 Data Integrity Guarantees

1. **Atomic Transactions**: Uses SQL Server transactions for transfer operations
2. **Constraint Validation**: Server-side and client-side validation
3. **Concurrent Safety**: Proper locking and isolation levels
4. **Audit Trail**: Immutable audit log of all transfers
5. **Error Recovery**: Failed transfers automatically rolled back

## 📝 Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "SecureVaultDb": "Server=(localdb)\\mssqllocaldb;Database=SecureVaultDb;Trusted_Connection=true;Encrypt=false;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

### Environment Variables
- `ConnectionStrings__SecureVaultDb` - Override database connection
- `ASPNETCORE_ENVIRONMENT` - Set to "Development" or "Production"

## 📄 Additional Documentation

- [ERROR_HANDLING_STRATEGY.md](ERROR_HANDLING_STRATEGY.md) - Complete error handling documentation
- [MIDDLEWARE_AUDIT_REPORT.md](MIDDLEWARE_AUDIT_REPORT.md) - Audit middleware analysis and recommendations
- [OpenAPI Specification](https://localhost:7046/openapi/v1.json) - API documentation

## 🤝 Contributing

See the template repository for contributing guidelines: [Clean.Architecture.Solution.Template](https://github.com/jasontaylordev/CleanArchitecture)

## 📚 Learning Resources

- [Clean Architecture](https://cleanarchitecture.jasontaylor.dev)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [MediatR Documentation](https://github.com/jbogard/MediatR)

## 📄 License

Licensed under the MIT License. See LICENSE file for details.