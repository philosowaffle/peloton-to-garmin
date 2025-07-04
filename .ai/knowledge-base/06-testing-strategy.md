# Testing Strategy and Guidelines

## Overview
This document outlines the comprehensive testing strategy for the P2G application, covering unit tests, integration tests, and end-to-end testing approaches.

## Testing Philosophy
- **Test Pyramid**: More unit tests, fewer integration tests, minimal E2E tests
- **Fast Feedback**: Tests should run quickly and provide immediate feedback
- **Reliable**: Tests should be deterministic and not flaky
- **Maintainable**: Tests should be easy to understand and maintain
- **Comprehensive**: Critical paths should have high test coverage

## Test Categories

### 1. Unit Tests
**Purpose**: Test individual components in isolation
**Location**: `src/UnitTests/`
**Framework**: xUnit, Moq, FluentAssertions

#### Test Structure
```csharp
[Fact]
public void SyncService_ShouldReturnSuccess_WhenWorkoutsExist()
{
    // Arrange
    var mockPelotonService = new Mock<IPelotonService>();
    var mockGarminUploader = new Mock<IGarminUploader>();
    var syncService = new SyncService(mockPelotonService.Object, mockGarminUploader.Object);
    
    // Act
    var result = await syncService.SyncAsync(5);
    
    // Assert
    result.SyncSuccess.Should().BeTrue();
}
```

#### Coverage Areas
- **Service Logic**: Business logic validation
- **Data Transformation**: DTO mapping and conversion
- **Error Handling**: Exception scenarios
- **Configuration**: Settings validation
- **Utilities**: Helper methods and extensions

### 2. Integration Tests
**Purpose**: Test component interactions and external dependencies
**Location**: `src/IntegrationTests/` (to be created)
**Framework**: ASP.NET Core Test Host, TestContainers

#### Database Integration Tests
```csharp
[Collection("Database")]
public class SettingsServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    
    public SettingsServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task SaveSettings_ShouldPersistToDatabase()
    {
        // Arrange
        var settingsService = new SettingsService(_fixture.DbContext);
        var settings = new Settings { /* ... */ };
        
        // Act
        await settingsService.SaveAsync(settings);
        
        // Assert
        var saved = await settingsService.GetAsync();
        saved.Should().BeEquivalentTo(settings);
    }
}
```

#### API Integration Tests
```csharp
[Collection("WebApp")]
public class SyncControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public SyncControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task PostSync_ShouldReturnSuccess()
    {
        // Arrange
        var request = new SyncRequest { NumWorkouts = 5 };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/sync", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### 3. End-to-End Tests
**Purpose**: Test complete user workflows
**Location**: `src/E2ETests/` (to be created)
**Framework**: Playwright, SpecFlow

#### Web UI E2E Tests
```csharp
[Test]
public async Task CompleteSync_ShouldUploadWorkouts()
{
    // Arrange
    await Page.GotoAsync("https://localhost:5001");
    await Page.FillAsync("#peloton-email", "test@example.com");
    await Page.FillAsync("#peloton-password", "password");
    
    // Act
    await Page.ClickAsync("#sync-button");
    await Page.WaitForSelectorAsync(".sync-success");
    
    // Assert
    var successMessage = await Page.TextContentAsync(".sync-success");
    successMessage.Should().Contain("Sync completed successfully");
}
```

## Test Data Management

### Sample Data Location
- **Peloton Responses**: `src/UnitTests/Data/peloton_responses/`
- **P2G Workouts**: `src/UnitTests/Data/p2g_workouts/`
- **Sample FIT Files**: `src/UnitTests/Data/sample_fit/`
- **Mock Responses**: `src/UnitTests/Data/github_responses/`

### Test Data Strategy
```csharp
public static class TestDataHelper
{
    public static P2GWorkout CreateSampleWorkout(WorkoutType type = WorkoutType.Cycling)
    {
        return new P2GWorkout
        {
            Workout = new Workout
            {
                Id = "test-workout-123",
                StartTime = DateTime.UtcNow.AddHours(-1),
                EndTime = DateTime.UtcNow,
                Title = "Test Workout"
            },
            WorkoutType = type,
            // ... other properties
        };
    }
    
    public static string LoadSampleJson(string fileName)
    {
        var path = Path.Combine("Data", fileName);
        return File.ReadAllText(path);
    }
}
```

### Data Builders
```csharp
public class WorkoutBuilder
{
    private Workout _workout = new Workout();
    
    public WorkoutBuilder WithId(string id)
    {
        _workout.Id = id;
        return this;
    }
    
    public WorkoutBuilder WithDuration(int seconds)
    {
        _workout.EndTime = _workout.StartTime.AddSeconds(seconds);
        return this;
    }
    
    public Workout Build() => _workout;
}

// Usage
var workout = new WorkoutBuilder()
    .WithId("test-123")
    .WithDuration(1800)
    .Build();
```

## Mocking Strategy

### External API Mocking
```csharp
public class MockPelotonApiClient : IPelotonApi
{
    private readonly Queue<Workout> _workouts = new();
    
    public void QueueWorkout(Workout workout)
    {
        _workouts.Enqueue(workout);
    }
    
    public Task<PagedPelotonResponse<Workout>> GetWorkoutsAsync(int pageSize, int page)
    {
        var workouts = _workouts.Take(pageSize).ToList();
        return Task.FromResult(new PagedPelotonResponse<Workout>
        {
            Data = workouts,
            Total = workouts.Count
        });
    }
}
```

### Service Mocking
```csharp
[Fact]
public async Task SyncService_ShouldHandleAuthenticationFailure()
{
    // Arrange
    var mockPelotonService = new Mock<IPelotonService>();
    mockPelotonService
        .Setup(x => x.GetRecentWorkoutsAsync(It.IsAny<int>()))
        .ThrowsAsync(new PelotonAuthenticationError("Invalid credentials"));
    
    var syncService = new SyncService(mockPelotonService.Object, /* other deps */);
    
    // Act & Assert
    await Assert.ThrowsAsync<PelotonAuthenticationError>(
        () => syncService.SyncAsync(5));
}
```

## Test Configuration

### Test Settings
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=:memory:"
  },
  "Peloton": {
    "Email": "test@example.com",
    "Password": "test-password",
    "NumWorkoutsToDownload": 5
  },
  "Garmin": {
    "Upload": false,
    "Email": "test@example.com",
    "Password": "test-password"
  }
}
```

### Test Fixtures
```csharp
public class DatabaseFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public ISettingsDb SettingsDb { get; private set; }
    
    public DatabaseFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISettingsDb, SettingsDb>();
        // Configure in-memory database
        
        ServiceProvider = services.BuildServiceProvider();
        SettingsDb = ServiceProvider.GetRequiredService<ISettingsDb>();
    }
    
    public void Dispose()
    {
        ServiceProvider?.Dispose();
    }
}
```

## Performance Testing

### Load Testing
```csharp
[Fact]
public async Task SyncService_ShouldHandleMultipleWorkouts()
{
    // Arrange
    var workouts = Enumerable.Range(1, 100)
        .Select(i => TestDataHelper.CreateSampleWorkout())
        .ToList();
    
    var stopwatch = Stopwatch.StartNew();
    
    // Act
    var result = await syncService.SyncAsync(workouts);
    
    // Assert
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // 30 seconds
    result.SyncSuccess.Should().BeTrue();
}
```

### Memory Testing
```csharp
[Fact]
public async Task ConvertWorkouts_ShouldNotLeakMemory()
{
    // Arrange
    var initialMemory = GC.GetTotalMemory(true);
    var workouts = CreateLargeWorkoutSet();
    
    // Act
    await converter.ConvertAsync(workouts);
    
    // Assert
    GC.Collect();
    GC.WaitForPendingFinalizers();
    var finalMemory = GC.GetTotalMemory(true);
    
    (finalMemory - initialMemory).Should().BeLessThan(50_000_000); // 50MB
}
```

## Test Automation

### CI/CD Pipeline Tests
```yaml
# .github/workflows/test.yml
name: Test Suite
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 9.0.x
    
    - name: Run Unit Tests
      run: dotnet test --configuration Release --logger trx --collect:"XPlat Code Coverage"
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
```

### Test Categories
```csharp
// Fast running unit tests
[Trait("Category", "Unit")]
public class FastUnitTests { }

// Slower integration tests
[Trait("Category", "Integration")]
public class IntegrationTests { }

// External dependency tests
[Trait("Category", "External")]
public class ExternalApiTests { }
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test class
dotnet test --filter "FullyQualifiedName~SyncServiceTests"
```

## Test Quality Guidelines

### Test Naming
```csharp
// Good: Descriptive method names
[Fact]
public void SyncService_ShouldReturnFailure_WhenPelotonAuthenticationFails()

// Bad: Unclear purpose
[Fact]
public void TestSync()
```

### Test Organization
```csharp
public class SyncServiceTests
{
    private readonly Mock<IPelotonService> _mockPelotonService;
    private readonly Mock<IGarminUploader> _mockGarminUploader;
    private readonly SyncService _syncService;
    
    public SyncServiceTests()
    {
        _mockPelotonService = new Mock<IPelotonService>();
        _mockGarminUploader = new Mock<IGarminUploader>();
        _syncService = new SyncService(_mockPelotonService.Object, _mockGarminUploader.Object);
    }
    
    [Fact]
    public async Task SyncAsync_ShouldReturnSuccess_WhenWorkoutsExist()
    {
        // Test implementation
    }
}
```

### Assertion Patterns
```csharp
// Use FluentAssertions for better readability
result.SyncSuccess.Should().BeTrue();
result.Errors.Should().BeEmpty();
result.ConvertedWorkouts.Should().HaveCount(5);

// Verify mock calls
_mockPelotonService.Verify(x => x.GetRecentWorkoutsAsync(5), Times.Once);
```

## Test Maintenance

### Regular Tasks
1. **Update Test Data**: Keep sample data current
2. **Review Coverage**: Maintain >80% code coverage
3. **Remove Obsolete Tests**: Clean up unused tests
4. **Update Dependencies**: Keep test frameworks updated

### Test Metrics
- **Code Coverage**: Target >80% line coverage
- **Test Execution Time**: Unit tests <5 minutes
- **Test Reliability**: <1% flaky test rate
- **Test Maintenance**: Monthly review and cleanup

## Debugging Tests

### Test Debugging
```csharp
[Fact]
public async Task DebugSyncProcess()
{
    // Enable detailed logging for debugging
    var loggerFactory = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
    
    var logger = loggerFactory.CreateLogger<SyncService>();
    
    // Test with detailed logging
}
```

### Test Data Inspection
```csharp
[Fact]
public void InspectTestData()
{
    var workout = TestDataHelper.CreateSampleWorkout();
    
    // Use debugger to inspect object state
    System.Diagnostics.Debugger.Break();
    
    // Or output to test console
    _output.WriteLine($"Workout: {JsonSerializer.Serialize(workout)}");
}
```

## Best Practices

### Do's
- ✅ Write tests before fixing bugs
- ✅ Test edge cases and error conditions
- ✅ Use descriptive test names
- ✅ Keep tests independent and isolated
- ✅ Mock external dependencies
- ✅ Use test data builders for complex objects

### Don'ts
- ❌ Don't test implementation details
- ❌ Don't write tests that depend on external services
- ❌ Don't ignore flaky tests
- ❌ Don't test multiple concerns in one test
- ❌ Don't use production data in tests
- ❌ Don't skip tests without good reason

## Future Improvements

### Planned Enhancements
1. **Visual Testing**: Screenshot comparison for UI tests
2. **Contract Testing**: API contract validation
3. **Chaos Engineering**: Fault injection testing
4. **Performance Monitoring**: Continuous performance testing
5. **Test Data Management**: Automated test data generation 