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
using System.IO;

using Dynastream.Fit;

public class FitDecoder
{
    public FitMessages Messages { get; private set; }
    private Stream inputStream;
    private Dynastream.Fit.File fileType;

    public FitDecoder(Stream stream, Dynastream.Fit.File fileType)
    {
        inputStream = stream;
        this.fileType = fileType;

        Messages = new FitMessages();
    }

    public bool Decode()
    {
        // Create the Decode Object
        Decode decoder = new Decode();

        // Check that this is a FIT file
        if (!decoder.IsFIT(inputStream))
        {
            throw new FileTypeException($"Expected FIT File Type: {fileType}, received a non FIT file.");
        }

        // Create the Message Broadcaster Object
        MesgBroadcaster mesgBroadcaster = new MesgBroadcaster();

        // Connect the the Decode and Message Broadcaster Objects
        decoder.MesgEvent += mesgBroadcaster.OnMesg;
        decoder.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;
        decoder.DeveloperFieldDescriptionEvent += OnDeveloperFieldDescriptionEvent;

        // Connect the Message Broadcaster Events to the Message Listener Delegates
        mesgBroadcaster.ActivityMesgEvent += OnActivityMesg;
        mesgBroadcaster.ClimbProMesgEvent += OnClimbProMesg;
        mesgBroadcaster.CourseMesgEvent += OnCourseMesg;
        mesgBroadcaster.CoursePointMesgEvent += OnCoursePointMesg;
        mesgBroadcaster.DeviceInfoMesgEvent += OnDeviceInfoMesg;
        mesgBroadcaster.EventMesgEvent += OnEventMesg;
        mesgBroadcaster.FileIdMesgEvent += OnFileIdMesg;
        mesgBroadcaster.HrMesgEvent += OnHrMesg;
        mesgBroadcaster.HrvMesgEvent += OnHrvMesg;
        mesgBroadcaster.LapMesgEvent += OnLapMesg;
        mesgBroadcaster.LengthMesgEvent += OnLengthMesg;
        mesgBroadcaster.RecordMesgEvent += OnRecordMesg;
        mesgBroadcaster.SegmentLapMesgEvent += OnSegmentLapMesg;
        mesgBroadcaster.SessionMesgEvent += OnSessionMesg;
        mesgBroadcaster.UserProfileMesgEvent += OnUserProfileMesg;
        mesgBroadcaster.WorkoutMesgEvent += OnWorkoutMesg;
        mesgBroadcaster.WorkoutStepMesgEvent += OnWorkoutStepMesg;
        mesgBroadcaster.ZonesTargetMesgEvent += OnZonesTargetMesg;

        // Decode the FIT File
        try
        {
            bool readOK = decoder.Read(inputStream);

            // If there are HR messages, merge the heart-rate data with the Record messages.
            if (readOK && Messages.HeartRates.Count > 0)
            {
                HrToRecordMesgWithoutPlugin.MergeHeartRates(Messages);
            }

            return readOK;
        }
        catch (FileTypeException ex)
        {
            throw (ex);
        }
        catch (FitException ex)
        {
            throw (ex);
        }
        catch (System.Exception ex)
        {
            throw (ex);
        }
        finally
        {
        }
    }

    public void OnActivityMesg(object sender, MesgEventArgs e)
    {
        Messages.Activity = (ActivityMesg)e.mesg;
    }

    public void OnClimbProMesg(object sender, MesgEventArgs e)
    {
        Messages.ClimbPros.Add(e.mesg as ClimbProMesg);
    }

    public void OnCourseMesg(object sender, MesgEventArgs e)
    {
        Messages.Course = (CourseMesg)e.mesg;
    }

    public void OnCoursePointMesg(object sender, MesgEventArgs e)
    {
        Messages.CoursePoints.Add(e.mesg as CoursePointMesg);
    }

    public void OnDeviceInfoMesg(object sender, MesgEventArgs e)
    {
        Messages.DeviceInfos.Add(e.mesg as DeviceInfoMesg);
    }

    public void OnEventMesg(object sender, MesgEventArgs e)
    {
        var eventMesg = e.mesg as EventMesg;
        Messages.Events.Add(eventMesg);

        if (eventMesg?.GetEvent() == Event.Timer && eventMesg?.GetTimestamp() != null)
        {
            Messages.Records.Add(new ExtendedRecordMesg(eventMesg));
        }
    }

    public void OnFileIdMesg(object sender, MesgEventArgs e)
    {
        Messages.FileId = (FileIdMesg)e.mesg;
        if ((e.mesg as FileIdMesg).GetType() != fileType)
        {
            throw new FileTypeException($"Expected FIT File Type: {fileType}, recieved File Type: {(e.mesg as FileIdMesg).GetType()}");
        }
    }

    public void OnHrMesg(object sender, MesgEventArgs e)
    {
        Messages.HeartRates.Add(e.mesg as HrMesg);
    }

    public void OnHrvMesg(object sender, MesgEventArgs e)
    {
        Messages.HeartRateVariabilites.Add(e.mesg as HrvMesg);
    }

    public void OnLapMesg(object sender, MesgEventArgs e)
    {
        Messages.Laps.Add(e.mesg as LapMesg);
    }

    public void OnLengthMesg(object sender, MesgEventArgs e)
    {
        Messages.Lengths.Add(e.mesg as LengthMesg);
    }

    public void OnRecordMesg(object sender, MesgEventArgs e)
    {
        Messages.Records.Add(new ExtendedRecordMesg(e.mesg as RecordMesg));

        foreach (Field field in e.mesg.Fields)
        {
            if (field.Name.ToLower() != "unknown")
            {
                Messages.RecordFieldNames.Add(field.Name);
            }
        }

        foreach (DeveloperField devField in e.mesg.DeveloperFields)
        {
            Messages.RecordDeveloperFieldNames.Add(devField.Name);
        }
    }

    public void OnSegmentLapMesg(object sender, MesgEventArgs e)
    {
        Messages.SegmentLaps.Add(e.mesg as SegmentLapMesg);
    }

    public void OnSessionMesg(object sender, MesgEventArgs e)
    {
        Messages.Sessions.Add(e.mesg as SessionMesg);
    }

    public void OnUserProfileMesg(object sender, MesgEventArgs e)
    {
        Messages.UserProfile = (UserProfileMesg)e.mesg;
    }

    public void OnWorkoutMesg(object sender, MesgEventArgs e)
    {
        Messages.Workout = (WorkoutMesg)e.mesg;
    }

    public void OnWorkoutStepMesg(object sender, MesgEventArgs e)
    {
        Messages.WorkoutSteps.Add(e.mesg as WorkoutStepMesg);
    }

    public void OnZonesTargetMesg(object sender, MesgEventArgs e)
    {
        Messages.ZonesTarget = (ZonesTargetMesg)e.mesg;
    }

    private void OnDeveloperFieldDescriptionEvent(object sender, DeveloperFieldDescriptionEventArgs e)
    {
        Messages.DeveloperFieldDescriptions.Add(e.Description as DeveloperFieldDescription);
    }
}
