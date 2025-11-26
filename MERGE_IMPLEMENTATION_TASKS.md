# Merge Feature - Implementation Tasks

This document outlines the remaining tasks needed to complete the merge feature implementation, including building, testing, DI setup, and UX enhancements.

---

## Phase 1: Environment Setup & Building

### 1.1 Install .NET 9.0.101 SDK

```bash
# Option 1: Using dotnet-install script (recommended)
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 9.0.101 --install-dir /usr/lib/dotnet

# Option 2: Using package manager (Ubuntu/Debian)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0

# Verify installation
dotnet --version  # Should show 9.0.101
```

### 1.2 Build Project

```bash
cd /home/frizat/Downloads/peloton-to-garmin

# Clean and restore
dotnet clean
dotnet restore

# Build with warnings treated as errors (for quality)
dotnet build --no-restore

# Expected output:
# - No compilation errors
# - All projects compile successfully
# - Warnings should be reviewed/fixed
```

**Key projects to verify:**
- `src/Sync/Sync.csproj` - Contains MergeEngine and utilities
- `src/Api/Api.csproj` - Contains merge endpoints
- `src/WebUI/WebUI.csproj` - Will contain merge UX
- `src/UnitTests/UnitTests.csproj` - Contains test infrastructure

---

## Phase 2: Dependency Injection Setup

### 2.1 ConsoleClient/Program.cs

Add MergeEngine registration after the SYNC section (around line 100):

```csharp
// SYNC
services.AddSingleton<ISyncStatusDb, SyncStatusDb>();
services.AddSingleton<ISyncService, SyncService>();

// MERGE (NEW)
services.AddSingleton<MergeEngine>(sp =>
{
    var settingsService = sp.GetRequiredService<ISettingsService>();
    var settings = settingsService.GetSettingsAsync().GetAwaiter().GetResult();
    
    var mergeOpts = MergeOptions.FromSettings(settings.Merge ?? new MergeSettings());
    
    var dataDir = settings.App.DataDirectory;
    var mergedDir = Path.Combine(dataDir, "merged");
    
    return new MergeEngine(
        mergeOpts,
        sp.GetRequiredService<IPelotonService>(),
        sp.GetRequiredService<IGarminApiClient>(),
        sp.GetRequiredService<IGarminAuthenticationService>(),
        mergedDir
    );
});
```

**Changes:**
- Add after line 101 (after `ISyncService` registration)
- Inject into `SyncService` constructor
- Ensure `SyncService` accepts optional `MergeEngine` parameter

### 2.2 Api/Program.cs

**Status**: Already partially done (lines 56-59)

**Verify/Update:**
```csharp
// Around line 56 - ALREADY EXISTS
builder.Services.AddSingleton<IMergeEngine, MergeEngine>();
builder.Services.AddSingleton<IMergeScoreCalculator, DefaultMergeScoreCalculator>();
builder.Services.Configure<MergeSettings>(
    builder.Configuration.GetSection("MergeSettings"));
```

**Issues to check:**
- [ ] Verify `IMergeEngine` interface exists (or create it)
- [ ] Verify `DefaultMergeScoreCalculator` implementation exists
- [ ] Ensure all dependencies are properly registered
- [ ] Check if `MergeSettings` is being loaded from config correctly

### 2.3 WebUI/Program.cs

**Current state**: Minimal - only registers `IApiClient`

**Add merge-related services** (around line 36):

```csharp
builder.Services.AddScoped<IApiClient>(sp => new ApiClient(config.Api.HostUrl));

// MERGE SERVICES (NEW)
builder.Services.AddScoped<MergeService>();
builder.Services.AddScoped<IMergeStatusService, MergeStatusService>();

builder.Services.ConfigureSharedUIServices();
```

**What needs to be created:**
- `MergeService` - Calls API for merge operations
- `IMergeStatusService` - Tracks merge status/results for display

---

## Phase 3: Unit Tests

### 3.1 Test Structure

**Location**: `src/UnitTests/Sync/Merge/`

**Tests needed** (create these files):

#### 3.1.1 MergeScoreCalculatorTests.cs
```csharp
namespace UnitTests.Sync.Merge;

public class MergeScoreCalculatorTests
{
    private MergeScoreCalculator _calculator;

    [SetUp]
    public void Setup()
    {
        _calculator = new MergeScoreCalculator();
    }

    [Test]
    public void CalculateScore_PerfectMatch_ReturnsOne()
    {
        // Arrange
        var pelotonStart = DateTime.Now;
        var garminStart = DateTime.Now;
        var pelotonDuration = 1800; // 30 min
        var garminDuration = 1800;

        // Act
        var score = _calculator.CalculateScore(
            pelotonStart, garminStart,
            pelotonDuration, garminDuration,
            timeWindowSeconds: 300, 
            durationDiffPct: 0.15);

        // Assert
        Assert.That(score, Is.EqualTo(1.0).Within(0.01));
    }

    [Test]
    public void CalculateScore_NoTimeMatch_LowScore()
    {
        // Arrange - 10 min time difference
        var pelotonStart = DateTime.Now;
        var garminStart = DateTime.Now.AddMinutes(10);
        var pelotonDuration = 1800;
        var garminDuration = 1800;

        // Act
        var score = _calculator.CalculateScore(
            pelotonStart, garminStart,
            pelotonDuration, garminDuration,
            timeWindowSeconds: 300,
            durationDiffPct: 0.15);

        // Assert
        Assert.That(score, Is.LessThan(0.5));
    }

    [Test]
    public void CalculateScore_DurationMismatch_ReducesScore()
    {
        // Arrange - Same time, 50% duration difference
        var pelotonStart = DateTime.Now;
        var garminStart = DateTime.Now;
        var pelotonDuration = 1800; // 30 min
        var garminDuration = 2700; // 45 min

        // Act
        var score = _calculator.CalculateScore(
            pelotonStart, garminStart,
            pelotonDuration, garminDuration,
            timeWindowSeconds: 300,
            durationDiffPct: 0.15);

        // Assert
        Assert.That(score, Is.LessThan(0.5));
    }
}
```

#### 3.1.2 TcxParserTests.cs
```csharp
namespace UnitTests.Sync.Merge;

public class TcxParserTests
{
    private TcxParser _parser;

    [SetUp]
    public void Setup()
    {
        _parser = new TcxParser();
    }

    [Test]
    public void Parse_ValidTcxContent_ReturnsSamples()
    {
        // Arrange
        var tcxContent = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TrainingCenterDatabase>
    <Activities>
        <Activity>
            <Lap>
                <Track>
                    <Trackpoint>
                        <Time>2024-01-15T10:00:00Z</Time>
                        <HeartRateBpm>120</HeartRateBpm>
                        <Cadence>80</Cadence>
                    </Trackpoint>
                </Track>
            </Lap>
        </Activity>
    </Activities>
</TrainingCenterDatabase>";

        // Act
        var samples = _parser.ParseTcx(tcxContent);

        // Assert
        Assert.That(samples, Is.Not.Empty);
        Assert.That(samples[0].HeartRate, Is.EqualTo(120));
        Assert.That(samples[0].Cadence, Is.EqualTo(80));
    }

    [Test]
    public void Parse_InvalidTcx_ThrowsException()
    {
        // Arrange
        var invalidXml = "not valid xml";

        // Act & Assert
        Assert.Throws<Exception>(() => _parser.ParseTcx(invalidXml));
    }

    [Test]
    public void Parse_EmptyTcx_ReturnsEmptyList()
    {
        // Arrange
        var emptyTcx = @"<?xml version=""1.0""?><TrainingCenterDatabase></TrainingCenterDatabase>";

        // Act
        var samples = _parser.ParseTcx(emptyTcx);

        // Assert
        Assert.That(samples, Is.Empty);
    }
}
```

#### 3.1.3 PelotonParserTests.cs
```csharp
namespace UnitTests.Sync.Merge;

public class PelotonParserTests
{
    private PelotonParser _parser;

    [SetUp]
    public void Setup()
    {
        _parser = new PelotonParser();
    }

    [Test]
    public void Parse_ValidWorkout_ExtractsSamples()
    {
        // Arrange
        var workout = new P2GWorkout
        {
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddMinutes(30),
            Metrics = new List<P2GMetrics>
            {
                new P2GMetrics { Timestamp = 0, HeartRate = 130, Power = 200 }
            }
        };

        // Act
        var samples = _parser.Parse(workout);

        // Assert
        Assert.That(samples, Is.Not.Empty);
        Assert.That(samples[0].HeartRate, Is.EqualTo(130));
        Assert.That(samples[0].Power, Is.EqualTo(200));
    }

    [Test]
    public void Parse_NoMetrics_ReturnsEmptyList()
    {
        // Arrange
        var workout = new P2GWorkout { Metrics = new List<P2GMetrics>() };

        // Act
        var samples = _parser.Parse(workout);

        // Assert
        Assert.That(samples, Is.Empty);
    }
}
```

#### 3.1.4 MergeSeriesTests.cs
```csharp
namespace UnitTests.Sync.Merge;

public class MergeSeriesTests
{
    private MergeSeries _merger;

    [SetUp]
    public void Setup()
    {
        _merger = new MergeSeries();
    }

    [Test]
    public void Merge_PreferGarminHR_SelectsGarminWhenAvailable()
    {
        // Arrange
        var garminSamples = new List<Sample>
        {
            new Sample { Time = DateTime.Now, HeartRate = 150 }
        };
        var pelotonSamples = new List<Sample>
        {
            new Sample { Time = DateTime.Now, HeartRate = 140 }
        };

        // Act
        var merged = _merger.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

        // Assert
        Assert.That(merged[0].HeartRate, Is.EqualTo(150)); // Prefers Garmin
    }

    [Test]
    public void Merge_FallbackTopelotonPower_SelectsPelotonForPower()
    {
        // Arrange
        var garminSamples = new List<Sample>
        {
            new Sample { Time = DateTime.Now, Power = 0 } // No power
        };
        var pelotonSamples = new List<Sample>
        {
            new Sample { Time = DateTime.Now, Power = 250 }
        };

        // Act
        var merged = _merger.Merge(garminSamples, pelotonSamples, resolutionSeconds: 1);

        // Assert
        Assert.That(merged[0].Power, Is.EqualTo(250)); // Falls back to Peloton
    }

    [Test]
    public void Merge_MissingData_InterpolatesCorrectly()
    {
        // Arrange
        var garminSamples = new List<Sample>
        {
            new Sample { Time = DateTime.Now, HeartRate = 140 },
            new Sample { Time = DateTime.Now.AddSeconds(2), HeartRate = 160 }
        };

        // Act
        var merged = _merger.Merge(garminSamples, new List<Sample>(), resolutionSeconds: 1);

        // Assert
        Assert.That(merged.Count, Is.GreaterThan(2)); // Should have interpolated sample
    }
}
```

### 3.2 Run Tests

```bash
# Run all merge tests
dotnet test --filter "Category=Merge" --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~MergeScoreCalculatorTests" --verbosity detailed

# Run with coverage (if coverage tool installed)
dotnet test /p:CollectCoverage=true
```

---

## Phase 4: SyncService Integration Verification

### 4.1 Verify MergeEngine injection in SyncService

**File**: `src/Sync/SyncService.cs`

**Check:**
```csharp
public class SyncService : ISyncService
{
    private readonly MergeEngine _mergeEngine;

    public SyncService(
        // ... existing parameters ...
        MergeEngine mergeEngine = null) // Should be optional
    {
        // ... existing code ...
        _mergeEngine = mergeEngine;
    }

    public async Task SyncAsync()
    {
        // ... existing sync logic ...
        
        // After upload step
        if (_mergeEngine != null && settings.Merge?.Enabled == true)
        {
            foreach (var workout in syncedWorkouts)
            {
                try
                {
                    await _mergeEngine.PreviewMergeAsync(workout.PelotonId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail sync
                    _logger.LogWarning(ex, "Merge failed for {PelotonId}", workout.PelotonId);
                }
            }
        }
    }
}
```

### 4.2 Test Integration

Add integration test in `src/UnitTests/Sync/SyncServiceTests.cs`:

```csharp
[Test]
public async Task SyncAsync_WithMergeEnabled_AttemptsMerge()
{
    // Arrange
    var mergeEngine = Substitute.For<MergeEngine>();
    var syncService = new SyncService(
        /* other params */,
        mergeEngine);

    // Act
    await syncService.SyncAsync();

    // Assert
    mergeEngine.Received().PreviewMergeAsync(Arg.Any<string>());
}
```

---

## Phase 5: UX Research & Implementation Plan

### 5.1 Current UX Architecture

**Technology Stack:**
- Blazor Server (`.razor` components)
- Bootstrap/CSS for styling
- Shared UI components in `SharedUI/` folder
- API client for backend communication

**Existing UX Patterns:**
- Settings pages: `SharedUI/Shared/FormatSettingsForm.razor`
- Modal dialogs: `SharedUI/Shared/GarminMfaModal.razor`
- Logs view: `SharedUI/Shared/ApiLogs.razor`

### 5.2 Merge UX Requirements

#### A. Merge Status Dashboard
**Location**: New page `src/WebUI/Pages/MergeStatus.razor`
**Features**:
- View recent merge operations (last 50)
- Show merge score for each operation
- Display data source for merged fields (Garmin/Peloton)
- Download merged TCX/FIT files
- Manual approve/reject interface

#### B. Merge Configuration UI
**Location**: Extend `src/SharedUI/Shared/MergeSettingsForm.razor` (NEW)
**Features**:
- Enable/disable toggle
- Time window configuration slider
- Duration diff % input
- Score thresholds
- Auto-approve settings
- Live preview of threshold impact

#### C. Merge Activity Panel
**Location**: Add to `src/WebUI/Pages/Index.razor` or sync status page
**Features**:
- Real-time merge status during sync
- Match found/not found indicators
- Score progress visualization
- Quick access to merge results

#### D. Historical Merge View
**Location**: New component `src/SharedUI/Shared/MergeHistory.razor`
**Features**:
- Table of merged workouts
- Sort by date, score, type
- Filter by auto-approved vs manual
- Comparison view (Garmin vs Peloton fields)

### 5.3 API Endpoints Required

**GET `/api/merge/status`** - Get overall merge status
```json
{
  "enabled": true,
  "lastSyncMerges": 5,
  "averageScore": 0.82,
  "recentOperations": [ { /* MergeResult */ } ]
}
```

**GET `/api/merge/history`** - Get merge history with pagination
```json
{
  "results": [ { /* MergeResult */ } ],
  "total": 150,
  "page": 1,
  "pageSize": 50
}
```

**POST `/api/merge/preview/{pelotonId}`** - Preview merge for a specific workout

**POST `/api/merge/approve`** - Approve and upload a merge

**GET `/api/merge/download/{pelotonId}`** - Download merged TCX/FIT file

**DELETE `/api/merge/{pelotonId}`** - Delete a merge result

### 5.4 Backend Services Needed

#### MergeService (ConsoleClient/WebUI accessible)
```csharp
public interface IMergeService
{
    Task<MergeStatusDto> GetStatusAsync();
    Task<List<MergeResultDto>> GetHistoryAsync(int page = 1, int pageSize = 50);
    Task<MergeResultDto> PreviewAsync(string pelotonId);
    Task ApproveAsync(string pelotonId);
    Task<byte[]> DownloadMergedFileAsync(string pelotonId, bool asFit = false);
    Task DeleteAsync(string pelotonId);
}
```

#### MergeStatusService (Tracking merge results)
```csharp
public interface IMergeStatusService
{
    Task SaveMergeResultAsync(MergeResult result);
    Task<List<MergeResult>> GetRecentMergesAsync(int count = 50);
    Task<MergeResult> GetByPelotonIdAsync(string pelotonId);
}
```

### 5.5 Database Schema Extension

Add to merge tracking:

```sql
CREATE TABLE MergeResults (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    PelotonId VARCHAR(255) NOT NULL UNIQUE,
    GarminActivityId BIGINT,
    Score DECIMAL(3,2),
    MergedTcxPath VARCHAR(500),
    MergedFitPath VARCHAR(500),
    AutoApproved BOOLEAN,
    Status VARCHAR(50), -- 'Pending', 'Approved', 'Rejected', 'Uploaded'
    Note TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    FOREIGN KEY (PelotonId) REFERENCES Workouts(PelotonId)
);
```

---

## Phase 6: Implementation Checklist

### Build Phase
- [ ] Install .NET 9.0.101 SDK
- [ ] Run `dotnet restore`
- [ ] Run `dotnet build` - verify no errors
- [ ] Fix any compilation warnings

### DI Setup Phase
- [ ] Add MergeEngine to ConsoleClient/Program.cs
- [ ] Verify Api/Program.cs merge registrations
- [ ] Add merge services to WebUI/Program.cs
- [ ] Build and verify DI resolution

### Testing Phase
- [ ] Create test files in `src/UnitTests/Sync/Merge/`
- [ ] Implement all test cases
- [ ] Run `dotnet test` - all tests pass
- [ ] Verify coverage > 80% for merge components

### SyncService Integration
- [ ] Verify MergeEngine is properly injected
- [ ] Verify null-safe merge execution
- [ ] Add integration tests
- [ ] Run full test suite

### UX Phase
- [ ] Create MergeStatus.razor page
- [ ] Create MergeSettingsForm.razor component
- [ ] Add merge status to Index/sync pages
- [ ] Create MergeHistory component
- [ ] Implement merge API endpoints
- [ ] Create backend services
- [ ] Test UI workflows

### Database Phase
- [ ] Create migration for MergeResults table
- [ ] Create MergeResultDb repository
- [ ] Implement IMergeStatusService
- [ ] Test persistence

### Final Verification
- [ ] Full build passes
- [ ] All tests pass
- [ ] Manual testing with real workouts
- [ ] Performance testing (merge overhead)
- [ ] Documentation updated

---

## Phase 7: Key Files Summary

### To Create
```
src/Sync/Merge/                           (Already exists)
├── MergeEngine.cs                        ✓
├── MergeOptions.cs                       ✓
├── MergeScoreCalculator.cs               ✓
├── MergeResult.cs                        ✓
├── Utilities/
│   ├── TcxParser.cs                      ✓
│   ├── PelotonParser.cs                  ✓
│   ├── MergeSeries.cs                    ✓
│   ├── TcxWriter.cs                      ✓
│   └── FitWriter.cs                      ✓
└── IMergeEngine.cs (Interface)           [CREATE if missing]

src/UnitTests/Sync/Merge/                 (NEW)
├── MergeScoreCalculatorTests.cs          [CREATE]
├── TcxParserTests.cs                     [CREATE]
├── PelotonParserTests.cs                 [CREATE]
└── MergeSeriesTests.cs                   [CREATE]

src/WebUI/Pages/
├── MergeStatus.razor                     [CREATE]

src/SharedUI/Shared/
├── MergeSettingsForm.razor               [CREATE]
├── MergeHistory.razor                    [CREATE]
└── MergeActivityPanel.razor              [CREATE]

src/Api/Controllers/
└── MergeController.cs                    [CREATE]

src/Api.Service/Services/
└── MergeService.cs                       [CREATE]

src/Common/Database/
└── MergeResultDb.cs                      [CREATE]
```

### To Modify
```
src/ConsoleClient/Program.cs              [ADD DI setup]
src/WebUI/Program.cs                      [ADD DI setup]
src/Api/Program.cs                        [VERIFY existing]
src/Sync/SyncService.cs                   [VERIFY integration]
src/Common/Dto/Settings.cs                [VERIFY MergeSettings]
configuration.example.json                [VERIFY Merge section]
Database migrations                       [ADD MergeResults table]
```

---

## Performance Considerations

- **Merge Overhead**: ~1-2 Garmin API calls per workout
- **File Storage**: Merged TCX/FIT files in `data/merged/`
- **Database Impact**: One row per merge operation
- **Cleanup**: Consider archiving old merge results (>30 days)

---

## Next Steps

1. **Immediate**: Install .NET 9.0.101 SDK and build project
2. **Week 1**: Complete DI setup and unit tests
3. **Week 2**: Verify SyncService integration
4. **Week 3**: Implement UX components
5. **Week 4**: Backend services and database
6. **Week 5**: Integration testing and documentation

---

## Questions for Review

- [ ] Should merge results be persisted to database?
- [ ] What's the retention policy for merged files?
- [ ] Should UI show real-time merge progress?
- [ ] Do we need a "re-merge" feature for existing activities?
- [ ] Should merge logs be separate or integrated with sync logs?

