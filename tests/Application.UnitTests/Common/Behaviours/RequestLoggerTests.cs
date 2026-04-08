using SecureVault.Application.Common.Behaviours;
using SecureVault.Application.Common.Interfaces;
using SecureVault.Application.Features.Accounts.Commands;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace SecureVault.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateAccountCommand>> _logger = null!;
    private Mock<IUser> _user = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateAccountCommand>>();
        _user = new Mock<IUser>();
    }

    [Test]
    public async Task ShouldLogRequestWithUserIdIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<CreateAccountCommand>(_logger.Object, _user.Object);

        await requestLogger.Process(new CreateAccountCommand { Name = "Test User", Email = "test@example.com" }, new CancellationToken());

        // Verify that logging was called
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldLogRequestWithoutUserIdIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<CreateAccountCommand>(_logger.Object, _user.Object);

        await requestLogger.Process(new CreateAccountCommand { Name = "Test User", Email = "test@example.com" }, new CancellationToken());

        // Verify that logging was called
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
