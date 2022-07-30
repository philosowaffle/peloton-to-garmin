////////////////////////////////////////////////////////////////////////////////
// The following FIT Protocol software provided may be used with FIT protocol
// devices only and remains the copyrighted property of Garmin Canada Inc.
// The software is being provided on an "as-is" basis and as an accommodation,
// and therefore all warranties, representations, or guarantees of any kind
// (whether express, implied or statutory) including, without limitation,
// warranties of merchantability, non-infringement, or fitness for a particular
// purpose, are specifically disclaimed.
//
// Copyright 2020 Garmin International, Inc.
////////////////////////////////////////////////////////////////////////////////
using System.Collections.Generic;
using Dynastream.Fit;

public class FitMessages
{
    public ActivityMesg Activity;
    public List<ClimbProMesg> ClimbPros = new List<ClimbProMesg>();
    public CourseMesg Course;
    public List<CoursePointMesg> CoursePoints = new List<CoursePointMesg>();
    public List<DeviceInfoMesg> DeviceInfos = new List<DeviceInfoMesg>();
    public List<EventMesg> Events = new List<EventMesg>();
    public FileIdMesg FileId;
    public List<HrMesg> HeartRates = new List<HrMesg>();
    public List<HrvMesg> HeartRateVariabilites = new List<HrvMesg>();
    public List<LapMesg> Laps = new List<LapMesg>();
    public List<LengthMesg> Lengths = new List<LengthMesg>();
    public List<ExtendedRecordMesg> Records = new List<ExtendedRecordMesg>();
    public List<SegmentLapMesg> SegmentLaps = new List<SegmentLapMesg>();
    public List<SessionMesg> Sessions = new List<SessionMesg>();
    public UserProfileMesg UserProfile;
    public WorkoutMesg Workout;
    public List<WorkoutStepMesg> WorkoutSteps = new List<WorkoutStepMesg>();
    public ZonesTargetMesg ZonesTarget;
    public List<DeveloperFieldDescription> DeveloperFieldDescriptions = new List<DeveloperFieldDescription>();
    public HashSet<string> RecordFieldNames = new HashSet<string>();
    public HashSet<string> RecordDeveloperFieldNames = new HashSet<string>();

    public FitMessages()
    {
    }
}
