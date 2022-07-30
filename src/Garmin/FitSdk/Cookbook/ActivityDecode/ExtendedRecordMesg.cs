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
using Dynastream.Fit;
public class ExtendedRecordMesg : RecordMesg
{
    public EventType EventType {get; private set;}

    public ExtendedRecordMesg(RecordMesg mesg) : base(mesg)
    {
        EventType = EventType.Invalid;
    }

    public ExtendedRecordMesg(EventMesg mesg)
    {
        SetTimestamp(mesg.GetTimestamp());
        EventType = mesg.GetEventType() ?? EventType.Invalid;
    }
}