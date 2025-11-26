# Merge Feature - UX Research & Implementation Roadmap

**Date**: November 26, 2024  
**Focus**: User Experience Design and Implementation Plan for Merge Feature

---

## Table of Contents

1. [Current UX Architecture](#current-ux-architecture)
2. [Merge UX Requirements Analysis](#merge-ux-requirements-analysis)
3. [Proposed UX Components](#proposed-ux-components)
4. [API Design](#api-design)
5. [Implementation Roadmap](#implementation-roadmap)
6. [User Workflows](#user-workflows)
7. [Data Persistence Strategy](#data-persistence-strategy)
8. [Mockups & Component Examples](#mockups--component-examples)

---

## Current UX Architecture

### Technology Stack
- **Frontend Framework**: Blazor Server
- **Styling**: Bootstrap 5
- **Components**: Razor Components (`.razor` files)
- **Communication**: HTTP/REST API
- **State Management**: Blazor component state + SignalR (optional)

### Existing UX Patterns

#### 1. Settings Form Pattern
**Location**: `SharedUI/Shared/FormatSettingsForm.razor`
**Features**:
- Form-based configuration
- Real-time validation
- Save/Cancel buttons
- Input type selectors

**Example Structure**:
```razor
@page "/settings/format"
@inject ISettingsService SettingsService

<EditForm Model="@settings" OnSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <div class="form-group">
        <label>Setting Name</label>
        <InputCheckbox @bind-Value="settings.Enabled" />
    </div>
    
    <button type="submit" class="btn btn-primary">Save</button>
</EditForm>
```

#### 2. Modal Dialog Pattern
**Location**: `SharedUI/Shared/GarminMfaModal.razor`
**Features**:
- Modal overlay
- Form inputs
- Action buttons
- Error handling

#### 3. Logs View Pattern
**Location**: `SharedUI/Shared/ApiLogs.razor`
**Features**:
- Real-time log display
- Filtering options
- Auto-scroll to latest
- Clear logs button

#### 4. Status Indicators
- Loading spinners
- Success/error badges
- Progress indicators
- Icons for status

---

## Merge UX Requirements Analysis

### User Stories

#### Story 1: Configure Merge Settings
**As a** power user  
**I want to** configure merge thresholds and preferences  
**So that** I can fine-tune when workouts are merged

**Acceptance Criteria**:
- [ ] Can enable/disable merge feature with toggle
- [ ] Can adjust time window (300-900 seconds)
- [ ] Can adjust duration tolerance (5%-25%)
- [ ] Can set match threshold (0.3-0.9)
- [ ] Can enable/disable auto-approve
- [ ] Settings persist to configuration
- [ ] Live preview shows impact of settings

#### Story 2: View Merge Status
**As a** user  
**I want to** see which of my recent workouts were merged  
**So that** I understand what data is being combined

**Acceptance Criteria**:
- [ ] Dashboard shows recent merge operations
- [ ] Can see merge score for each operation
- [ ] Can see which Garmin activity was matched
- [ ] Can see timestamp of merge
- [ ] Can see merge status (pending/approved/uploaded)

#### Story 3: Approve Merged Workouts
**As a** user  
**I want to** review and approve merges before upload  
**So that** I have control over what gets uploaded

**Acceptance Criteria**:
- [ ] Can see pending merges
- [ ] Can preview merged data (both sources)
- [ ] Can see field source (Garmin vs Peloton)
- [ ] Can approve to upload
- [ ] Can reject and keep original
- [ ] Can override data sources for specific fields

#### Story 4: View Merge History
**As a** user  
**I want to** see history of all merged workouts  
**So that** I can verify past merges

**Acceptance Criteria**:
- [ ] Can see list of merged workouts
- [ ] Can filter by date, status, score
- [ ] Can sort by date, score, type
- [ ] Can download merged files (TCX/FIT)
- [ ] Can re-merge or re-upload
- [ ] Can delete merge results

#### Story 5: Real-time Merge Progress
**As a** user  
**I want to** see merge progress during sync  
**So that** I know the system is working

**Acceptance Criteria**:
- [ ] Merge activity shows in sync progress
- [ ] Can see "searching for matches" status
- [ ] Can see "found match" status with score
- [ ] Can see "merging data" status
- [ ] Can see "uploading" status
- [ ] Can see results after completion

---

## Proposed UX Components

### 1. Merge Settings Form Component

**File**: `src/SharedUI/Shared/MergeSettingsForm.razor`  
**Purpose**: Configure merge behavior  
**Location**: Settings page  
**Interaction**: Forms

```razor
@page "/settings/merge"
@inject ISettingsService SettingsService

<div class="card">
    <div class="card-header">
        <h4>Merge Settings</h4>
    </div>
    <div class="card-body">
        <EditForm Model="@mergeSettings" OnSubmit="@HandleSubmit">
            <DataAnnotationsValidator />
            <ValidationSummary />
            
            <!-- Enable/Disable Toggle -->
            <div class="form-group">
                <label class="form-check-label">
                    <InputCheckbox class="form-check-input" 
                                   @bind-Value="mergeSettings.Enabled" />
                    Enable Merge Feature
                </label>
                <small class="form-text text-muted">
                    Allows merging Peloton workouts with existing Garmin activities
                </small>
            </div>
            
            <!-- Time Window Slider -->
            <div class="form-group">
                <label>Time Window (seconds)</label>
                <input type="range" class="form-range" 
                       min="60" max="900" step="30"
                       @bind="mergeSettings.MatchTimeWindowSeconds"
                       @onchange="@(() => UpdatePreview())" />
                <div class="form-text">
                    Current: @mergeSettings.MatchTimeWindowSeconds seconds 
                    (@TimeSpan.FromSeconds(mergeSettings.MatchTimeWindowSeconds).TotalMinutes:F1 minutes)
                </div>
                <small class="form-text text-muted">
                    Maximum time difference between workout start times to consider a match
                </small>
            </div>
            
            <!-- Duration Tolerance -->
            <div class="form-group">
                <label>Duration Tolerance (%)</label>
                <div class="input-group">
                    <InputNumber class="form-control" 
                                 @bind-Value="mergeSettings.MatchDurationDiffPct"
                                 min="0.05" max="0.50" step="0.05" />
                    <span class="input-group-text">%</span>
                </div>
                <small class="form-text text-muted">
                    Allowed difference in workout duration (e.g., 0.15 = ±15%)
                </small>
            </div>
            
            <!-- Match Threshold -->
            <div class="form-group">
                <label>Match Score Threshold</label>
                <div class="input-group">
                    <InputNumber class="form-control" 
                                 @bind-Value="mergeSettings.MatchScoreThreshold"
                                 min="0.3" max="0.9" step="0.1" />
                    <span class="input-group-text">/1.0</span>
                </div>
                <small class="form-text text-muted">
                    Minimum score (0-1) to consider it a valid match
                </small>
            </div>
            
            <!-- Auto-Approve Settings -->
            <div class="form-group">
                <label class="form-check-label">
                    <InputCheckbox class="form-check-input" 
                                   @bind-Value="mergeSettings.AutoApproveEnabled" />
                    Auto-Approve High-Confidence Merges
                </label>
            </div>
            
            @if (mergeSettings.AutoApproveEnabled)
            {
                <div class="form-group">
                    <label>Auto-Approve Threshold</label>
                    <div class="input-group">
                        <InputNumber class="form-control" 
                                     @bind-Value="mergeSettings.AutoApproveScoreThreshold"
                                     min="0.5" max="1.0" step="0.05" />
                        <span class="input-group-text">/1.0</span>
                    </div>
                    <small class="form-text text-muted">
                        Score required for automatic upload
                    </small>
                </div>
            }
            
            <!-- Preview of Thresholds -->
            <div class="alert alert-info">
                <strong>Impact Preview:</strong>
                <ul>
                    <li>Merge accepted if score ≥ @mergeSettings.MatchScoreThreshold</li>
                    <li>Auto-uploaded if score ≥ @mergeSettings.AutoApproveScoreThreshold</li>
                    <li>Time window: ±@mergeSettings.MatchTimeWindowSeconds seconds</li>
                    <li>Duration tolerance: ±@(mergeSettings.MatchDurationDiffPct * 100)%</li>
                </ul>
            </div>
            
            <button type="submit" class="btn btn-primary">Save Settings</button>
            <button type="button" class="btn btn-secondary" @onclick="ResetToDefaults">
                Reset to Defaults
            </button>
        </EditForm>
    </div>
</div>

@code {
    private MergeSettings mergeSettings = new();
    
    protected override async Task OnInitializedAsync()
    {
        var settings = await SettingsService.GetSettingsAsync();
        mergeSettings = settings.Merge ?? new MergeSettings();
    }
    
    private async Task HandleSubmit()
    {
        await SettingsService.UpdateMergeSettingsAsync(mergeSettings);
    }
    
    private void UpdatePreview()
    {
        StateHasChanged();
    }
    
    private void ResetToDefaults()
    {
        mergeSettings = new MergeSettings(); // Reset to defaults
    }
}
```

### 2. Merge Status Dashboard Component

**File**: `src/WebUI/Pages/MergeStatus.razor`  
**Purpose**: View recent merge operations  
**Location**: Navigation menu  
**Interaction**: Read-only display + action buttons

```razor
@page "/merge/status"
@inject IMergeService MergeService
@inject ILogger<MergeStatus> Logger

<div class="container-fluid">
    <h2>Merge Status</h2>
    
    @if (isLoading)
    {
        <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    }
    else if (statusError != null)
    {
        <div class="alert alert-danger">@statusError</div>
    }
    else if (status == null)
    {
        <div class="alert alert-info">Merge feature not enabled</div>
    }
    else
    {
        <!-- Overall Status Cards -->
        <div class="row mb-4">
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h3 class="card-title text-primary">@status.EnabledStatus</h3>
                        <p class="card-text">Status</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h3 class="card-title">@status.LastSyncMerges</h3>
                        <p class="card-text">Merges This Sync</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h3 class="card-title">@status.AverageScore.ToString("F2")</h3>
                        <p class="card-text">Avg Score</p>
                    </div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="card text-center">
                    <div class="card-body">
                        <h3 class="card-title">@status.RecentOperations.Count</h3>
                        <p class="card-text">Recent Merges</p>
                    </div>
                </div>
            </div>
        </div>
        
        <!-- Recent Merge Operations -->
        <div class="card">
            <div class="card-header">
                <h4>Recent Merge Operations</h4>
            </div>
            <div class="card-body">
                @if (status.RecentOperations?.Count > 0)
                {
                    <div class="table-responsive">
                        <table class="table table-hover">
                            <thead>
                                <tr>
                                    <th>Peloton ID</th>
                                    <th>Garmin Activity</th>
                                    <th>Score</th>
                                    <th>Status</th>
                                    <th>Time</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var op in status.RecentOperations)
                                {
                                    <tr>
                                        <td>
                                            <code>@op.PelotonId</code>
                                        </td>
                                        <td>
                                            @if (op.GarminActivityId.HasValue)
                                            {
                                                <span>@op.GarminActivityId</span>
                                            }
                                            else
                                            {
                                                <span class="text-muted">—</span>
                                            }
                                        </td>
                                        <td>
                                            <span class="badge" style="background-color: @GetScoreColor(op.Score)">
                                                @op.Score.ToString("F2")
                                            </span>
                                        </td>
                                        <td>
                                            @if (op.AutoApproved)
                                            {
                                                <span class="badge bg-success">Auto-Approved</span>
                                            }
                                            else if (!string.IsNullOrEmpty(op.Note))
                                            {
                                                <span class="badge bg-warning">Pending Review</span>
                                            }
                                            else
                                            {
                                                <span class="badge bg-secondary">Unknown</span>
                                            }
                                        </td>
                                        <td>
                                            @op.CreatedAt?.ToString("g")
                                        </td>
                                        <td>
                                            <button class="btn btn-sm btn-info" 
                                                    @onclick="() => ViewDetails(op)">
                                                Details
                                            </button>
                                            <button class="btn btn-sm btn-download"
                                                    @onclick="() => DownloadMerged(op)">
                                                Download
                                            </button>
                                        </td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                }
                else
                {
                    <div class="alert alert-info">No recent merge operations</div>
                }
            </div>
        </div>
    }
</div>

@code {
    private MergeStatusDto status;
    private bool isLoading = true;
    private string statusError;
    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;
            status = await MergeService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading merge status");
            statusError = "Failed to load merge status";
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private string GetScoreColor(double score)
    {
        if (score >= 0.75) return "#28a745"; // green
        if (score >= 0.50) return "#ffc107"; // yellow
        return "#dc3545"; // red
    }
    
    private async Task ViewDetails(MergeResultDto merge)
    {
        // Show modal with merge details
    }
    
    private async Task DownloadMerged(MergeResultDto merge)
    {
        try
        {
            var fileBytes = await MergeService.DownloadMergedFileAsync(
                merge.PelotonId, asFit: false);
            // Trigger browser download
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error downloading merge");
        }
    }
}
```

### 3. Merge History Component

**File**: `src/SharedUI/Shared/MergeHistory.razor`  
**Purpose**: Browse historical merge operations  
**Location**: Settings page  
**Interaction**: Filterable/sortable table

```razor
@inject IMergeService MergeService

<div class="card">
    <div class="card-header">
        <h4>Merge History</h4>
        <div class="row mt-2">
            <div class="col-md-6">
                <input type="date" class="form-control" @bind="filterStartDate" 
                       @onchange="@(() => RefreshHistory())" />
            </div>
            <div class="col-md-6">
                <select class="form-select" @bind="filterStatus"
                        @onchange="@(() => RefreshHistory())">
                    <option value="">All Statuses</option>
                    <option value="approved">Approved</option>
                    <option value="rejected">Rejected</option>
                    <option value="pending">Pending</option>
                </select>
            </div>
        </div>
    </div>
    <div class="card-body">
        <div class="table-responsive">
            <table class="table table-striped">
                <thead>
                    <tr>
                        <th @onclick="() => Sort('date')" style="cursor: pointer;">
                            Date @GetSortIndicator("date")
                        </th>
                        <th @onclick="() => Sort('score')" style="cursor: pointer;">
                            Score @GetSortIndicator("score")
                        </th>
                        <th>Peloton Details</th>
                        <th>Garmin Activity</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var merge in filteredHistory)
                    {
                        <tr>
                            <td>@merge.CreatedAt?.ToString("g")</td>
                            <td>
                                <span class="badge bg-@GetScoreBadgeColor(merge.Score)">
                                    @merge.Score.ToString("F2")
                                </span>
                            </td>
                            <td>@merge.PelotonId</td>
                            <td>@merge.GarminActivityId</td>
                            <td>
                                @if (merge.AutoApproved)
                                {
                                    <span class="badge bg-success">Auto-Approved</span>
                                }
                                else
                                {
                                    <span class="badge bg-warning">Needs Review</span>
                                }
                            </td>
                            <td>
                                <button class="btn btn-sm btn-primary"
                                        @onclick="() => ViewComparison(merge)">
                                    Compare
                                </button>
                                <button class="btn btn-sm btn-danger"
                                        @onclick="() => DeleteMerge(merge)">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
</div>

@code {
    private List<MergeResultDto> history = new();
    private List<MergeResultDto> filteredHistory = new();
    private DateTime filterStartDate = DateTime.Today.AddMonths(-1);
    private string filterStatus = "";
    private string sortBy = "date";
    private bool sortDesc = true;
    
    protected override async Task OnInitializedAsync()
    {
        await RefreshHistory();
    }
    
    private async Task RefreshHistory()
    {
        history = await MergeService.GetHistoryAsync();
        ApplyFilters();
    }
    
    private void ApplyFilters()
    {
        filteredHistory = history
            .Where(m => m.CreatedAt >= filterStartDate)
            .Where(m => string.IsNullOrEmpty(filterStatus) || 
                        (filterStatus == "approved" && m.AutoApproved) ||
                        (filterStatus == "pending" && !m.AutoApproved))
            .OrderByDescending(m => sortDesc)
            .ToList();
    }
    
    private void Sort(string column)
    {
        if (sortBy == column)
            sortDesc = !sortDesc;
        else
            sortBy = column;
        ApplyFilters();
    }
    
    private string GetSortIndicator(string column)
        => sortBy == column ? (sortDesc ? "↓" : "↑") : "";
    
    private string GetScoreBadgeColor(double score)
        => score >= 0.75 ? "success" : score >= 0.50 ? "warning" : "danger";
    
    private async Task ViewComparison(MergeResultDto merge)
    {
        // Show comparison modal
    }
    
    private async Task DeleteMerge(MergeResultDto merge)
    {
        await MergeService.DeleteAsync(merge.PelotonId);
        await RefreshHistory();
    }
}
```

### 4. Merge Activity Panel (Sync Progress)

**File**: `src/SharedUI/Shared/MergeActivityPanel.razor`  
**Purpose**: Show merge progress during sync  
**Location**: Sync status page  
**Interaction**: Real-time updates

```razor
@inject IMergeStatusService StatusService
@implements IAsyncDisposable

<div class="card">
    <div class="card-header">
        <h5>Merge Activity (During Sync)</h5>
    </div>
    <div class="card-body">
        @if (mergeActivities?.Count > 0)
        {
            <div class="timeline">
                @foreach (var activity in mergeActivities.OrderByDescending(a => a.Timestamp))
                {
                    <div class="timeline-item @activity.Status.ToLower()">
                        <div class="timeline-marker">
                            @if (activity.Status == "searching")
                            {
                                <span class="spinner-border spinner-border-sm" role="status">
                                    <span class="visually-hidden">Searching...</span>
                                </span>
                            }
                            else if (activity.Status == "found")
                            {
                                <span class="badge bg-success">✓</span>
                            }
                            else if (activity.Status == "merging")
                            {
                                <span class="spinner-border spinner-border-sm" role="status">
                                    <span class="visually-hidden">Merging...</span>
                                </span>
                            }
                            else if (activity.Status == "uploaded")
                            {
                                <span class="badge bg-success">✓</span>
                            }
                            else
                            {
                                <span class="badge bg-secondary">—</span>
                            }
                        </div>
                        <div class="timeline-content">
                            <p class="mb-0">
                                <strong>@activity.WorkoutName</strong>
                                <span class="text-muted">@activity.Timestamp.ToString("HH:mm:ss")</span>
                            </p>
                            <p class="text-small text-muted mb-0">@activity.Message</p>
                            @if (activity.Score.HasValue)
                            {
                                <span class="badge bg-@GetScoreBadge(activity.Score.Value)">
                                    Score: @activity.Score.Value.ToString("F2")
                                </span>
                            }
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <p class="text-muted">No merge activity</p>
        }
    </div>
</div>

<style>
    .timeline {
        position: relative;
        padding-left: 30px;
    }
    
    .timeline-item {
        position: relative;
        padding-bottom: 20px;
    }
    
    .timeline-marker {
        position: absolute;
        left: -30px;
        top: 0;
    }
</style>

@code {
    private List<MergeActivityDto> mergeActivities = new();
    private Timer refreshTimer;
    
    protected override async Task OnInitializedAsync()
    {
        await RefreshActivities();
        
        // Refresh every 2 seconds
        refreshTimer = new Timer(async _ =>
        {
            await RefreshActivities();
            StateHasChanged();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }
    
    private async Task RefreshActivities()
    {
        try
        {
            mergeActivities = await StatusService.GetRecentActivitiesAsync(20);
        }
        catch (Exception ex)
        {
            // Log error
        }
    }
    
    private string GetScoreBadge(double score)
        => score >= 0.75 ? "success" : score >= 0.50 ? "warning" : "danger";
    
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        refreshTimer?.Dispose();
    }
}
```

---

## API Design

### REST Endpoints

#### 1. Get Merge Status
```http
GET /api/merge/status
```
**Response**:
```json
{
  "enabled": true,
  "lastSyncMerges": 5,
  "averageScore": 0.82,
  "pendingCount": 2,
  "recentOperations": [
    {
      "pelotonId": "abc123",
      "garminActivityId": 987654321,
      "score": 0.85,
      "autoApproved": true,
      "createdAt": "2024-11-26T10:00:00Z"
    }
  ]
}
```

#### 2. Get Merge History
```http
GET /api/merge/history?page=1&pageSize=50&startDate=2024-01-01&status=approved
```
**Response**:
```json
{
  "results": [
    {
      "pelotonId": "abc123",
      "garminActivityId": 987654321,
      "score": 0.85,
      "autoApproved": true,
      "note": "Successfully merged",
      "mergedTcxPath": "/data/merged/abc123-tcx",
      "mergedFitPath": "/data/merged/abc123-fit",
      "createdAt": "2024-11-26T10:00:00Z"
    }
  ],
  "total": 150,
  "page": 1,
  "pageSize": 50
}
```

#### 3. Preview Merge
```http
POST /api/merge/preview/{pelotonId}
```
**Response**:
```json
{
  "pelotonId": "abc123",
  "pelotonWorkoutName": "Morning Ride",
  "pelotonDuration": 1800,
  "garminActivityId": 987654321,
  "garminActivityName": "Cycling",
  "garminDuration": 1805,
  "score": 0.85,
  "timeMatchScore": 0.95,
  "durationMatchScore": 0.80,
  "mergedData": {
    "heartRate": { "source": "garmin", "value": 150 },
    "power": { "source": "peloton", "value": 250 },
    "cadence": { "source": "garmin", "value": 95 }
  },
  "wouldAutoApprove": true
}
```

#### 4. Approve Merge
```http
POST /api/merge/approve
Content-Type: application/json

{
  "pelotonId": "abc123",
  "garminActivityId": 987654321,
  "score": 0.85
}
```
**Response**:
```json
{
  "success": true,
  "uploaded": true,
  "garminActivityId": 987654321,
  "message": "Merged workout uploaded successfully"
}
```

#### 5. Reject Merge
```http
POST /api/merge/reject
Content-Type: application/json

{
  "pelotonId": "abc123"
}
```

#### 6. Download Merged File
```http
GET /api/merge/download/{pelotonId}?format=tcx
```
**Response**: Binary TCX/FIT file

#### 7. Delete Merge Result
```http
DELETE /api/merge/{pelotonId}
```

---

## Implementation Roadmap

### Phase 1: Backend Services (Week 1-2)

#### 1.1 Create MergeService
**File**: `src/Api.Service/Services/MergeService.cs`
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

#### 1.2 Create MergeStatusService
**File**: `src/Api.Service/Services/MergeStatusService.cs`
```csharp
public interface IMergeStatusService
{
    Task SaveMergeResultAsync(MergeResult result);
    Task<List<MergeResult>> GetRecentMergesAsync(int count = 50);
    Task<MergeResult> GetByPelotonIdAsync(string pelotonId);
}
```

#### 1.3 Create MergeController
**File**: `src/Api/Controllers/MergeController.cs`
- Routes to service methods
- Error handling
- Logging

### Phase 2: Database Layer (Week 2)

#### 2.1 Create Migration
```sql
CREATE TABLE MergeResults (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    PelotonId VARCHAR(255) NOT NULL UNIQUE,
    GarminActivityId BIGINT,
    Score DECIMAL(3,2),
    TimeMatchScore DECIMAL(3,2),
    DurationMatchScore DECIMAL(3,2),
    MergedTcxPath VARCHAR(500),
    MergedFitPath VARCHAR(500),
    AutoApproved BOOLEAN DEFAULT FALSE,
    Status VARCHAR(50) DEFAULT 'pending',
    Note TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    DeletedAt DATETIME,
    
    FOREIGN KEY (PelotonId) REFERENCES Workouts(PelotonId),
    INDEX idx_status (Status),
    INDEX idx_created (CreatedAt),
    INDEX idx_score (Score)
);
```

#### 2.2 Create MergeResultDb
**File**: `src/Common/Database/MergeResultDb.cs`
- CRUD operations
- Queries for filtering/sorting
- Soft delete support

### Phase 3: API Endpoints (Week 2-3)

#### 3.1 Implement MergeController
- GET /api/merge/status
- GET /api/merge/history
- POST /api/merge/preview/{id}
- POST /api/merge/approve
- GET /api/merge/download/{id}
- DELETE /api/merge/{id}

### Phase 4: Frontend Components (Week 3-4)

#### 4.1 Create MergeSettingsForm.razor
- Settings form with validation
- Real-time preview
- Save functionality

#### 4.2 Create MergeStatus.razor page
- Dashboard with status cards
- Recent operations table
- Action buttons

#### 4.3 Create MergeHistory.razor component
- Filterable/sortable table
- Pagination support
- Download buttons

#### 4.4 Create MergeActivityPanel.razor
- Real-time activity timeline
- Status indicators
- Score badges

### Phase 5: Integration (Week 4-5)

#### 5.1 Wire DI for WebUI
- Register services
- Register components
- Configure API client

#### 5.2 Add Navigation Links
- Add Merge link to main menu
- Add settings link
- Add status link

#### 5.3 Integration Testing
- Test complete workflow
- Performance testing
- Error scenarios

### Phase 6: Polish & Documentation (Week 5)

#### 6.1 UI Polish
- Responsive design
- Accessibility (a11y)
- Loading states

#### 6.2 Help Text
- Tooltips for settings
- Hover explanations
- User guide

#### 6.3 Documentation
- User guide
- Admin guide
- API documentation

---

## User Workflows

### Workflow 1: Enable and Configure Merge

```
User → Settings → Merge Settings
↓
Enable Toggle → Configure Thresholds
↓
[Time Window: 300 sec, Duration: 15%, Score: 0.50]
↓
Enable Auto-Approve → Set Auto-Approve Threshold (0.75)
↓
Save Settings
↓
Configuration Stored → Settings take effect on next sync
```

### Workflow 2: Monitor Merge During Sync

```
User → Sync → Merge Activity Panel
↓
See real-time status:
  - "Searching for matches..." (spinner)
  - "Found match: Score 0.82" (green checkmark)
  - "Merging data..." (spinner)
  - "Uploading..." (spinner)
  - "Complete!" (success badge)
↓
View in Merge Status dashboard
```

### Workflow 3: Review Pending Merges

```
User → Merge Status
↓
See Recent Merge Operations table
↓
Click "Pending Review" badge
↓
Modal shows:
  - Peloton workout details
  - Garmin activity details
  - Merge score breakdown
  - Field sources (which data from where)
↓
User chooses:
  - Approve & Upload
  - Reject & Keep Original
  - Edit Field Sources (advanced)
```

### Workflow 4: Browse Merge History

```
User → Settings → Merge History
↓
Filter by:
  - Date range
  - Status (Approved/Pending/Rejected)
↓
Sort by:
  - Date (newest first)
  - Score (highest first)
↓
For each merge:
  - View details
  - Download TCX/FIT
  - Compare Garmin vs Peloton
  - Delete if needed
```

---

## Data Persistence Strategy

### Option A: Simple File-Based (Current - Merge Phase 1)
- Merged TCX/FIT files saved to `data/merged/`
- Results tracked in logs
- No database persistence
- Manual review of files

**Pros**: Simple, no DB changes  
**Cons**: No history, manual reviews

### Option B: Database-Backed (Merge Phase 2)
- MergeResults table tracks all merges
- File references stored in DB
- Query-able history
- UI-based review

**Pros**: Full history, searchable, UI integration  
**Cons**: Requires migration, DB disk space

**Recommendation**: Start with Option A, upgrade to B in Phase 2

### Retention Policy
- Keep merge results for 90 days
- Archive old results monthly
- Clean up merged files after upload succeeds
- Option to permanently delete old merges

---

## Mockups & Component Examples

### Settings Page Layout
```
┌─────────────────────────────────────────┐
│ Settings                                │
├─────────────────────────────────────────┤
│ [Tabs: General | Format | Merge | Help] │
├─────────────────────────────────────────┤
│                                         │
│ ☑ Enable Merge Feature                 │
│                                         │
│ Time Window (seconds): [━━━━|━━━━] 300  │
│   ├─ 5 minutes max difference          │
│                                         │
│ Duration Tolerance (%): [15] %         │
│   ├─ +/- allowed duration variance     │
│                                         │
│ Match Score Threshold: [0.50] / 1.0    │
│   ├─ Minimum score to match            │
│                                         │
│ ☑ Auto-Approve High-Confidence Merges  │
│   └─ Threshold: [0.75] / 1.0           │
│                                         │
│ ┌─ Impact Preview ─────────────────┐   │
│ │ • Merge accepted if ≥ 0.50       │   │
│ │ • Auto-uploaded if ≥ 0.75        │   │
│ │ • Time window: ±300 seconds      │   │
│ │ • Duration tolerance: ±15%       │   │
│ └──────────────────────────────────┘   │
│                                         │
│ [Save Settings] [Reset to Defaults]   │
└─────────────────────────────────────────┘
```

### Merge Status Page
```
┌─────────────────────────────────────────┐
│ Merge Status                            │
├─────────────────────────────────────────┤
│ ┌──────┬──────┬──────┬──────┐          │
│ │ Enabled
 │ Merges │ Score │Recent │          │
│ │ Yes    │   5   │ 0.82  │   15   │          │
│ └──────┴──────┴──────┴──────┘          │
│                                         │
│ Recent Merge Operations                │
│ ┌─────────────────────────────────┐    │
│ │ Date  │ Score │ Status   │ ▼   │    │
│ ├─────────────────────────────────┤    │
│ │ 10:15 │ 0.85  │ Approved │ ⋯   │    │
│ │ 10:10 │ 0.72  │ Pending  │ ⋯   │    │
│ │ 10:05 │ 0.91  │ Approved │ ⋯   │    │
│ │ 10:00 │ 0.45  │ Rejected │ ⋯   │    │
│ └─────────────────────────────────┘    │
│                                         │
│ [View History] [Download All]         │
└─────────────────────────────────────────┘
```

---

## Summary

| Component | Status | Owner | Timeline |
|-----------|--------|-------|----------|
| Settings Form | 📋 Designed | Frontend | Week 3 |
| Status Dashboard | 📋 Designed | Frontend | Week 3 |
| History Component | 📋 Designed | Frontend | Week 4 |
| Activity Panel | 📋 Designed | Frontend | Week 4 |
| API Service | 📋 Designed | Backend | Week 1 |
| MergeController | 📋 Designed | Backend | Week 2 |
| Database Layer | 📋 Designed | Database | Week 2 |
| Integration | 📋 Designed | Full Stack | Week 4 |
| Testing | 📋 Designed | QA | Week 5 |

---

**Created**: November 26, 2024  
**Updated**: November 26, 2024  
**Author**: GitHub Copilot CLI  
**Status**: Ready for Implementation

Next: Start Phase 1 (Backend Services)

