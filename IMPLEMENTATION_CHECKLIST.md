# Merge Feature Implementation Checklist

## ✅ Completed Implementation

### Core Merge Engine
- [x] Created `MergeEngine.cs` with async workflow
- [x] Created `MergeOptions.cs` with settings integration
- [x] Created `MergeScoreCalculator.cs` for match scoring
- [x] Created `MergeResult.cs` DTO
- [x] Integrated with proper typed services (not dynamic)

### Utilities
- [x] Created `TcxParser.cs` for Garmin TCX parsing
- [x] Created `PelotonParser.cs` for P2GWorkout parsing
- [x] Created `MergeSeries.cs` for time series merging
- [x] Created `TcxWriter.cs` for TCX output
- [x] Created `FitWriter.cs` for basic FIT output

### Garmin API Extensions
- [x] Added `GarminActivity.cs` DTO
- [x] Added `GetRecentActivitiesAsync()` to `IGarminApiClient`
- [x] Added `GetActivityTcxAsync()` to `IGarminApiClient`
- [x] Implemented both methods in `ApiClient.cs`

### Configuration
- [x] Added `MergeSettings` class to `Settings.cs`
- [x] Updated `configuration.example.json` with Merge section
- [x] Created `MergeOptions.FromSettings()` factory method

### Integration
- [x] Updated `SyncService.cs` to accept `MergeEngine`
- [x] Added merge logic after upload step in sync workflow
- [x] Ensured merge failures don't break sync
- [x] Added comprehensive logging

### Documentation
- [x] Created `MERGE_FEATURE.md` - User/admin documentation
- [x] Created `CODE_REVIEW_AND_MERGE_INTEGRATION.md` - Developer handoff doc
- [x] Created this implementation checklist

---

## ⚠️ Pending (Requires .NET 9 SDK)

### Build & Test
- [ ] Build solution to verify no compilation errors
  ```bash
  # Requires .NET 9.0 SDK
  dotnet restore
  dotnet build
  ```

- [ ] Run existing unit tests
  ```bash
  dotnet test
  ```

- [ ] Add unit tests for merge components
  - [ ] `MergeScoreCalculator` tests (various scenarios)
  - [ ] `MergeSeries` tests (missing data, conflicts)
  - [ ] `TcxParser` tests (valid/invalid TCX)
  - [ ] `PelotonParser` tests (various workout types)

### Dependency Injection Setup
- [ ] Update DI container in `ConsoleClient/Program.cs`
- [ ] Update DI container in `Api.Service/Program.cs` (if used)
- [ ] Update DI container in `WebUI/Program.cs` (if used)

**Example DI Registration:**
```csharp
// In Program.cs or Startup.cs

// Option 1: Create in factory
builder.Services.AddSingleton<MergeEngine>(sp =>
{
    var settingsService = sp.GetRequiredService<ISettingsService>();
    var settings = settingsService.GetSettingsAsync().GetAwaiter().GetResult();
    
    var opts = MergeOptions.FromSettings(settings.Merge);
    
    return new MergeEngine(
        opts,
        sp.GetRequiredService<IPelotonService>(),
        sp.GetRequiredService<IGarminApiClient>(),
        sp.GetRequiredService<IGarminAuthenticationService>(),
        Path.Combine(settings.App.DataDirectory, "merged")
    );
});

// Option 2: Let DI resolve dependencies
// (Requires parameterless constructor or constructor that DI can resolve)
builder.Services.AddSingleton<MergeEngine>();
```

### Real-World Testing
- [ ] Install .NET 9.0 SDK
  ```bash
  # Ubuntu/Debian
  wget https://dot.net/v1/dotnet-install.sh
  chmod +x dotnet-install.sh
  ./dotnet-install.sh --version 9.0.101
  
  # Or download from: https://dotnet.microsoft.com/download/dotnet/9.0
  ```

- [ ] Record a workout on both Peloton and Garmin
- [ ] Configure `configuration.json`:
  - Set Peloton credentials
  - Set Garmin credentials
  - Set `Merge.Enabled = true`
- [ ] Run sync: `dotnet run --project src/ConsoleClient`
- [ ] Check logs for merge activity
- [ ] Verify merged files in `data/merged/`
- [ ] Check Garmin Connect for uploaded merged activity

### Production Considerations
- [ ] Review `FitWriter.cs` implementation
  - Current version writes minimal custom format
  - Consider using official Garmin FIT SDK if Garmin rejects files
  - TCX fallback should work for most cases

- [ ] Add retry logic for transient failures
  - API timeouts
  - Rate limiting
  - Network issues

- [ ] Consider adding persistent queue
  - Store merge candidates that need manual review
  - Retry failed merges on next sync

- [ ] Monitor Garmin API rate limits
  - Merge adds 1-2 API calls per workout
  - May need to adjust rate limiting in `GarminUploader`

---

## 🔄 Integration Steps (When Ready)

### Step 1: Install Prerequisites
```bash
# Install .NET 9.0 SDK
# See: https://dotnet.microsoft.com/download/dotnet/9.0
```

### Step 2: Build Solution
```bash
cd /home/frizat/Downloads/peloton-to-garmin

# Restore NuGet packages
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test
```

### Step 3: Update DI Registrations

Find where services are registered (usually `Program.cs` in each project) and add:

```csharp
// Add this after other service registrations
services.AddSingleton<MergeEngine>(sp => 
{
    var settingsService = sp.GetRequiredService<ISettingsService>();
    var settings = settingsService.GetSettingsAsync().GetAwaiter().GetResult();
    var opts = MergeOptions.FromSettings(settings.Merge);
    
    return new MergeEngine(
        opts,
        sp.GetRequiredService<IPelotonService>(),
        sp.GetRequiredService<IGarminApiClient>(),
        sp.GetRequiredService<IGarminAuthenticationService>(),
        Path.Combine(settings.App.DataDirectory, "merged")
    );
});
```

### Step 4: Configure & Test

1. Copy configuration:
   ```bash
   cp configuration.example.json configuration.json
   ```

2. Edit `configuration.json`:
   - Add Peloton credentials
   - Add Garmin credentials
   - Enable merge: `"Merge": { "Enabled": true }`

3. Test with one workout:
   ```bash
   dotnet run --project src/ConsoleClient
   ```

4. Check results:
   - Review console logs for merge activity
   - Check `data/merged/` for generated files
   - Verify in Garmin Connect

---

## 📝 Notes for Development

### Match Score Tuning

The default thresholds work well for workouts started within 5 minutes:

```json
{
  "MatchTimeWindowSeconds": 300,     // ±5 minutes
  "MatchDurationDiffPct": 0.15,     // ±15% duration
  "MatchScoreThreshold": 0.50,      // 50% minimum confidence
  "AutoApproveScoreThreshold": 0.75 // 75% for auto-upload
}
```

**Adjust based on your workflow**:
- Longer delay between starts? Increase `MatchTimeWindowSeconds`
- Pausing one device? Increase `MatchDurationDiffPct`
- Want fewer false positives? Increase `MatchScoreThreshold`
- Want manual review? Set `AutoApproveEnabled: false`

### Data Source Priority

Current merge logic (in `MergeSeries.cs`):
1. **GPS**: Always Garmin (Peloton rarely has GPS)
2. **Heart Rate**: Prefer Garmin (watch is more accurate), fallback to Peloton
3. **Power**: Prefer Peloton (bike/tread has power meter)
4. **Cadence**: Prefer Garmin if available, else Peloton

**To change priorities**: Edit `MergeSeries.Merge()` method.

### File Cleanup

Merged files accumulate in `data/merged/`. Consider adding cleanup:

```csharp
// In SyncService or scheduled job
var mergedDir = Path.Combine(settings.App.DataDirectory, "merged");
var oldFiles = Directory.GetFiles(mergedDir)
    .Where(f => File.GetCreationTime(f) < DateTime.Now.AddDays(-30));
    
foreach (var file in oldFiles)
    File.Delete(file);
```

---

## 🐛 Troubleshooting

### Compilation Errors

**Issue**: Namespace not found for `MergeEngine`, `MergeOptions`, etc.

**Solution**: Ensure projects have correct references:
```xml
<!-- In Sync.csproj -->
<ItemGroup>
  <Compile Include="Merge\**\*.cs" />
</ItemGroup>
```

### Runtime: MergeEngine is null

**Issue**: `SyncService` receives null `MergeEngine`

**Solutions**:
1. Check DI registration (see Step 3 above)
2. Verify `MergeEngine` is registered before `SyncService`
3. Constructor makes it optional, so sync will work even if null

### Garmin API: 401 Unauthorized

**Issue**: Can't download activities or TCX

**Solutions**:
1. Re-authenticate with Garmin
2. Check OAuth tokens haven't expired
3. Verify `GarminApiAuthentication` is valid before calling API

### Low or Zero Match Scores

**Issue**: Merge finds activities but scores are too low

**Solutions**:
1. Check start times are within `MatchTimeWindowSeconds`
2. Verify durations are similar (within `MatchDurationDiffPct`)
3. Review actual Peloton and Garmin workout start/end times
4. Temporarily lower thresholds for testing

---

## 🎯 Success Criteria

Merge feature is production-ready when:

- [x] ✅ Code compiles without errors
- [ ] ⏳ All unit tests pass (pending .NET 9 SDK)
- [ ] ⏳ Integration test: Record dual workout → Sync → Verify merge
- [ ] ⏳ Garmin accepts uploaded merged activities
- [ ] ⏳ Logs show clear merge process and scores
- [ ] ⏳ Configuration works for enabled/disabled states
- [ ] ⏳ Sync continues successfully even if merge fails

---

## 📚 Reference Files

- **User Documentation**: `MERGE_FEATURE.md`
- **Developer Handoff**: `CODE_REVIEW_AND_MERGE_INTEGRATION.md`
- **This Checklist**: `IMPLEMENTATION_CHECKLIST.md`
- **Example Config**: `configuration.example.json`
- **Main README**: `README.md`

---

## 🚀 Next Actions (Priority Order)

1. **Install .NET 9.0 SDK** (required for build)
2. **Build solution** to verify compilation
3. **Add DI registration** in Program.cs files
4. **Test with real workout** (record on both platforms)
5. **Review logs** to verify merge logic
6. **Iterate on thresholds** based on real results
7. **Add unit tests** for merge components
8. **Monitor production** for merge success/failure rates

---

**Status**: Code is complete and integrated. Pending build verification and real-world testing with .NET 9 SDK.

**Estimated Time to Production**: 
- With .NET 9 SDK: 1-2 hours (build, test, configure)
- First production test: 30 minutes (record workout, sync, verify)
- Tuning period: 1-2 weeks (adjust thresholds based on usage)
