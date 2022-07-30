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

public class SessionMessages
{
    public ActivityMesg Activity;
    public List<ClimbProMesg> ClimbPros = new List<ClimbProMesg>();
    public List<DeviceInfoMesg> DeviceInfos = new List<DeviceInfoMesg>();
    public List<EventMesg> Events = new List<EventMesg>();
    public FileIdMesg FileId;
    public List<LapMesg> Laps = new List<LapMesg>();
    public List<LengthMesg> Lengths = new List<LengthMesg>();
    public List<ExtendedRecordMesg> Records = new List<ExtendedRecordMesg>();
    public HashSet<string> RecordFieldNames = new HashSet<string>();
    public HashSet<string> RecordDeveloperFieldNames = new HashSet<string>();
    public List<SegmentLapMesg> SegmentLaps = new List<SegmentLapMesg>();
    public SessionMesg Session;
    public UserProfileMesg UserProfile;
    public WorkoutMesg Workout;
    public List<WorkoutStepMesg> WorkoutSteps = new List<WorkoutStepMesg>();
    public ZonesTargetMesg ZonesTarget;

    public List<EventMesg> TimerEvents = new List<EventMesg>();
    public List<EventMesg> FrontGearChangeEvents = new List<EventMesg>();
    public List<EventMesg> RearGearChangeEvents = new List<EventMesg>();
    public List<EventMesg> RiderPositionChangeEvents = new List<EventMesg>();

    public SessionMessages(SessionMesg session)
    {
        Session = session;
    }
}