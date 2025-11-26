# Merge Feature - Peloton to Garmin

## Overview

The Merge feature allows Peloton-to-Garmin to intelligently merge Peloton workout data with existing Garmin Connect activities. This is useful when you've recorded the same workout on both platforms (e.g., using a Garmin watch while doing a Peloton workout) and want to combine the best data from each source into a single, comprehensive activity record.

## How It Works

1. **Matching**: When syncing a Peloton workout, the merge engine searches your recent Garmin activities to find potential matches based on:
   - Start time proximity (within configurable time window)
   - Duration similarity (within configurable percentage difference)

2. **Scoring**: Each potential match is scored from 0.0 to 1.0 based on:
   - Time match (60% weight): How close the start times are
   - Duration match (40% weight): How similar the workout durations are

3. **Merging**: If a suitable match is found (score above threshold):
   - Downloads the Garmin activity data (TCX format)
   - Parses both Peloton and Garmin data into unified time series
   - Merges the data, preferring:
     - **Garmin** for: Heart rate (from watch), GPS data, cadence (if available)
     - **Peloton** for: Power data, cadence (if not available from Garmin), heart rate (if missing from Garmin)

4. **Upload**: The merged workout is:
   - Saved as both TCX and FIT files
   - Automatically uploaded to Garmin Connect if auto-approve is enabled and score is high enough
   - Otherwise, saved for manual review

## Configuration

Add the `Merge` section to your `configuration.json`:

```json
{
  "Merge": {
    "Enabled": false,
    "MatchTimeWindowSeconds": 300,
    "MatchDurationDiffPct": 0.15,
    "MatchScoreThreshold": 0.50,
    "AutoApproveEnabled": true,
    "AutoApproveScoreThreshold": 0.75,
    "InterpolationResolutionSeconds": 1
  }
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Enabled` | bool | `false` | Enable/disable the merge feature |
| `MatchTimeWindowSeconds` | int | `300` | Maximum time difference (in seconds) between workout start times to consider a match. Default 5 minutes. |
| `MatchDurationDiffPct` | double | `0.15` | Maximum duration difference as a percentage (0.15 = 15%) to consider a match |
| `MatchScoreThreshold` | double | `0.50` | Minimum score (0.0-1.0) required to consider a Garmin activity a valid match |
| `AutoApproveEnabled` | bool | `true` | Whether to automatically upload merged workouts that meet the auto-approve threshold |
| `AutoApproveScoreThreshold` | double | `0.75` | Minimum score (0.0-1.0) required for automatic approval and upload |
| `InterpolationResolutionSeconds` | int | `1` | Time resolution in seconds for the merged data samples |

## When to Use Merge

### Good Use Cases

✅ **Outdoor cycling with Garmin watch + Peloton app**
- Your Garmin watch captures GPS, accurate HR, and route data
- Peloton app captures power data and ride details
- Merge combines the best of both

✅ **Treadmill run with Garmin watch + Peloton tread**
- Garmin captures HR and potentially GPS if you go outside
- Peloton captures accurate speed/pace from treadmill
- Merge provides complete picture

✅ **Strength training with wearable + Peloton**
- Garmin/Apple Watch tracks HR throughout
- Peloton tracks exercises and rep counts
- Merge provides full workout data

### When NOT to Use Merge

❌ **Regular sync without dual recording**: If you're only recording on Peloton, merge won't find matches
❌ **Time-shifted workouts**: If you record Peloton workout hours after the Garmin activity
❌ **Different workout types**: Merge is designed for the same workout recorded twice, not different activities

## Workflow

### Typical Sync Process

1. **Start your workout** with both devices recording:
   - Garmin watch/device running
   - Peloton bike/tread/app running

2. **Complete your workout** on both platforms

3. **Run P2G sync**:
   ```bash
   # Sync will automatically attempt merge if enabled
   p2g sync
   ```

4. **Review results**:
   - Check logs for merge scores and matches
   - High-confidence matches (score >= 0.75) are auto-uploaded
   - Lower-confidence matches are logged for manual review

### Manual Merge Review

Merged files are saved to `data/merged/` directory:
- `merged-{pelotonId}-{timestamp}.tcx` - TCX format for review
- `merged-{pelotonId}-{timestamp}.fit` - FIT format for upload

You can manually review TCX files and upload to Garmin if needed.

## Data Merge Strategy

The merge algorithm uses a conservative approach:

### Priority Rules

1. **GPS Data**: Always from Garmin (Peloton typically doesn't have GPS)
2. **Heart Rate**: Prefer Garmin watch (more accurate), fallback to Peloton
3. **Power/Watts**: From Peloton (bike/tread has this, watch doesn't)
4. **Cadence**: Prefer Garmin if available, otherwise Peloton
5. **Time Series**: Unified timeline from start to end of both workouts

### Example Merged Data Point

```
Time: 2024-01-15 10:00:30
├─ HeartRate: 145 bpm (from Garmin watch)
├─ Power: 180 watts (from Peloton bike)
├─ Cadence: 85 rpm (from Peloton)
├─ GPS: lat/lon (from Garmin, if outdoor)
└─ Source: "merged"
```

## Troubleshooting

### No Matches Found

**Problem**: Merge engine reports no matching Garmin activities

**Solutions**:
- Ensure you recorded the workout on both platforms within the time window
- Check that Garmin activity was uploaded to Garmin Connect
- Increase `MatchTimeWindowSeconds` if workouts were started with delay
- Lower `MatchScoreThreshold` to be more permissive (but review carefully)

### Low Match Scores

**Problem**: Matches found but scores below threshold

**Solutions**:
- Check if start times are significantly different
- Verify duration difference - did you pause one but not the other?
- Review the actual Garmin and Peloton workout details in their respective apps

### Merge Fails During Upload

**Problem**: Merge creates files but upload fails

**Solutions**:
- Check Garmin authentication (may need to re-authenticate)
- Verify TCX/FIT files in `data/merged/` are valid
- Try manually uploading the TCX file to Garmin Connect
- Check Garmin Connect isn't rejecting duplicate activities

## Architecture

### Components

```
SyncService
    ├─ Downloads Peloton workouts
    ├─ Converts to FIT/TCX
    ├─ Uploads to Garmin
    └─ [NEW] MergeEngine
           ├─ Searches recent Garmin activities
           ├─ Scores potential matches
           ├─ Downloads matching Garmin TCX
           ├─ Merges data from both sources
           └─ Uploads merged result
```

### Key Classes

- **`MergeEngine`**: Orchestrates the merge process
- **`MergeScoreCalculator`**: Computes match scores
- **`TcxParser`**: Parses Garmin TCX data
- **`PelotonParser`**: Parses Peloton workout data
- **`MergeSeries`**: Combines time series from both sources
- **`TcxWriter`**: Writes merged TCX files
- **`FitWriter`**: Writes minimal FIT files

## API Integration

The merge functionality is automatically integrated into the sync workflow when enabled. No separate API calls are needed for basic usage.

### Advanced: Manual Merge via API

If using the API/WebUI, you can trigger merges manually:

```http
POST /api/merge/preview/{pelotonId}
Response: { 
  "pelotonId": "abc123",
  "garminActivityId": "987654321",
  "score": 0.85,
  "mergedTcxPath": "/data/merged/...",
  "autoApproved": true
}

POST /api/merge/approve
Body: { /* MergeResult from preview */ }
Response: { "success": true, "uploaded": "987654321", "note": "uploaded" }
```

## Performance

- **Additional API Calls**: ~1-2 per workout (list activities, download TCX)
- **Storage**: Merged files saved to `data/merged/` (cleaned up periodically)
- **Processing**: Minimal overhead, typically <1 second per workout
- **Rate Limiting**: Respects existing Garmin API rate limits

## Future Enhancements

Potential improvements for future versions:

- [ ] Web UI for reviewing merge candidates
- [ ] Batch merge for historical workouts
- [ ] Configurable data priority (prefer Peloton HR over Garmin, etc.)
- [ ] Support for other platforms (Strava, TrainingPeaks, etc.)
- [ ] Machine learning for better match scoring
- [ ] Conflict resolution UI for manual data selection

## Credits

This merge feature integrates seamlessly with the existing P2G architecture and builds upon the solid foundation of Peloton and Garmin API integration already present in the codebase.
