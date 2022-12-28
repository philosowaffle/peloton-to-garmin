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
using System;
using System.Collections.Generic;
using System.Linq;
using Dynastream.Fit;
using Extensions;

public class ActivityParser
{
    private FitMessages _messages;
    public bool IsActivityFile => _messages?.FileId != null ? (_messages?.FileId?.GetType() ?? File.Invalid) == File.Activity : false;

    public ActivityParser(FitMessages messages)
    {
        _messages = messages;
    }

    public List<SessionMessages> ParseSessions()
    {
        if (!IsActivityFile)
        {
            throw new Exception($"Expected FIT File Type: Activity, recieved File Type: {_messages?.FileId?.GetType()}");
        }

        // When there are no Sessions but there are Records create a Session message to recover as much data as possible
        if (_messages.Sessions.Count == 0 && _messages.Records.Count > 0)
        {
            Dynastream.Fit.DateTime startTime = _messages.Records[0].GetTimestamp();
            Dynastream.Fit.DateTime timestamp = _messages.Records[_messages.Records.Count - 1].GetTimestamp();

            var session = new SessionMesg();
            session.SetStartTime(startTime);
            session.SetTimestamp(timestamp);
            session.SetTotalElapsedTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());
            session.SetTotalTimerTime(timestamp.GetTimeStamp() - startTime.GetTimeStamp());

            _messages.Sessions.Add(session);
        }

        int recordsTaken = 0;

        var sessions = new List<SessionMessages>(_messages.Sessions.Count);
        foreach (SessionMesg sessionMesg in _messages.Sessions)
        {
            var session = new SessionMessages(sessionMesg)
            {
                Laps = _messages.Laps.Skip(sessionMesg.GetFirstLapIndex() ?? 0).Take(sessionMesg.GetNumLaps() ?? 0).ToList(),

                ClimbPros = _messages.ClimbPros.Where(climb => climb.Within(sessionMesg)).ToList(),
                Events = _messages.Events.Where(evt => evt.Within(sessionMesg)).ToList(),
                DeviceInfos = _messages.DeviceInfos.Where(deviceInfo => deviceInfo.Within(sessionMesg)).ToList(),
                Lengths = _messages.Lengths.Where(length => length.Overlaps(sessionMesg)).ToList(),
                Records = _messages.Records.Skip(recordsTaken).Where(record => record.Within(sessionMesg)).ToList(),
                SegmentLaps = _messages.SegmentLaps.Where(segmentLap => segmentLap.Overlaps(sessionMesg)).ToList(),

                TimerEvents = _messages.Events.Where(evt => evt.GetEvent() == Event.Timer && evt.Within(sessionMesg)).ToList(),
                FrontGearChangeEvents = _messages.Events.Where(evt => evt.GetEvent() == Event.FrontGearChange && evt.Within(sessionMesg)).ToList(),
                RearGearChangeEvents = _messages.Events.Where(evt => evt.GetEvent() == Event.RearGearChange && evt.Within(sessionMesg)).ToList(),
                RiderPositionChangeEvents = _messages.Events.Where(evt => evt.GetEvent() == Event.RiderPositionChange && evt.Within(sessionMesg)).ToList(),

                Activity = _messages.Activity,
                FileId = _messages.FileId,
                RecordFieldNames = _messages.RecordFieldNames,
                RecordDeveloperFieldNames = _messages.RecordDeveloperFieldNames,
                UserProfile = _messages.UserProfile,
                Workout = _messages.Workout,
                WorkoutSteps = _messages.WorkoutSteps,
                ZonesTarget = _messages.ZonesTarget,
            };

            recordsTaken += session.Records.Count;
            sessions.Add(session);
        }

        return sessions;
    }

    public List<DeviceInfoMesg> DevicesWhereBatteryStatusIsLow()
    {
        var batteryStatus = new List<byte>() { BatteryStatus.Critical, BatteryStatus.Low };
        var deviceInfos = new List<DeviceInfoMesg>();

        deviceInfos = _messages.DeviceInfos.Where(info => batteryStatus.Contains(info.GetBatteryStatus() ?? BatteryStatus.Unknown)).ToList();
        return deviceInfos;
    }
}
