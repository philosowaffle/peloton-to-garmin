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
using System.Text;
using Dynastream.Fit;
using Extensions;

public class Export
{
    private const string Unknown = "unknown";
    private const double MetersToYards = 1.09361;
    private const double DefaultPoolLength = 25.0;

    static public string RecordsToCSV(SessionMessages session)
    {
        var stringBuilder = new StringBuilder();

        // Add a comment row with: Sport Type, Sub Sport, Date/Time, Total Distance (meters), Calories, Duration (seconds)
        stringBuilder.AppendLine($"#Records,{session.Session.GetSport().ToString()},{session.Session.GetSubSport().ToString()},{session.Session.GetStartTime().GetDateTime().ToString("yyyy-MM-dd HH:mm:ss")},{session.Session.GetTotalDistance()},{session.Session.GetTotalCalories() ?? 0},{session.Session.GetTotalElapsedTime() ?? 0}");

        // Create the header row
        stringBuilder.Append("Seconds,");
        stringBuilder.Append($"{string.Join(",", session.RecordFieldNames)},");

        if (session.RecordDeveloperFieldNames.Count > 0)
        {
            stringBuilder.Append($"developerdata_{string.Join(",developerdata_", session.RecordDeveloperFieldNames).Replace(" ","_")},");
        }

        stringBuilder.Append("TimerEvent,Lap");
        stringBuilder.AppendLine();

        var lapQueue = new Queue<LapMesg>(session.Laps);
        var lap = lapQueue.Count > 0 ? lapQueue.Dequeue() : null;
        var lapId = 1;

        uint firstTimeStamp = session.Records[0].GetTimestamp().GetTimeStamp();

        foreach (ExtendedRecordMesg record in session.Records)
        {
            while (lap != null && record.GetTimestamp().GetTimeStamp() > lap.GetTimestamp().GetTimeStamp())
            {
                lap = lapQueue.Count > 0 ? lapQueue.Dequeue() : null;
                lapId++;
            }

            stringBuilder.Append($"{record.GetTimestamp().GetTimeStamp() - firstTimeStamp},");

            foreach (string fieldName in session.RecordFieldNames)
            {
                var numFieldValues = record.GetNumFieldValues(fieldName);
                if (numFieldValues > 1)
                {
                    for (int i = 0; i < numFieldValues; i++)
                    {
                        stringBuilder.Append($"{record.GetFieldValue(fieldName, i)}|");
                    }
                    stringBuilder.Length--;
                    stringBuilder.Append($",");
                }
                else
                {
                    stringBuilder.Append($"{record.GetFieldValue(fieldName)},");
                }
            }

            foreach (string devFieldName in session.RecordDeveloperFieldNames)
            {
                DeveloperField devField = record.DeveloperFields.Where(f => f.Name == devFieldName).FirstOrDefault();
                if (devField != null)
                {
                    stringBuilder.Append($"{devField.GetValue(0)}");
                }
                stringBuilder.Append(",");
            }

            stringBuilder.Append($"{(record.EventType == EventType.Invalid ? "" : record.EventType.ToString())},");
            stringBuilder.Append($"{lapId}");

            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();
    }

    static public string LengthsToCSV(SessionMessages session)
    {
        var isMetric = session.Session.GetPoolLengthUnit() == DisplayMeasure.Metric;
        var unitConversion = isMetric ? 1.0 : MetersToYards;
        double poolLength = session.Session.GetPoolLength() ?? DefaultPoolLength;
        var poolLengthString = $"{Math.Round(poolLength * unitConversion)}";
        var totalDistance = Math.Round((session.Session.GetNumActiveLengths() ?? 0) * poolLength * unitConversion);

        var stringBuilder = new StringBuilder();

        // Add a comment row with: Sport Type, Sub Sport, Date/Time, Total Distance, Pool Length, Units, Calories, Duration (Seconds)
        stringBuilder.AppendLine($"#Lengths,{session.Session.GetSport().ToString()},{session.Session.GetSubSport().ToString()},{session.Session.GetStartTime().GetDateTime().ToString("yyyy-MM-dd HH:mm:ss")},{totalDistance},{poolLengthString},{(isMetric ? "meters" : "yards")},{session.Session.GetTotalCalories() ?? 0},{session.Session.GetTotalElapsedTime() ?? 0}");

        // Create the header row
        stringBuilder.AppendLine($"LENGTH TYPE,DURATION (seconds),DISTANCE ({(isMetric ? "meters" : "yards")}),PACE,STOKE COUNT,SWOLF,DPS,STROKE RATE,STROKE TYPE");

        foreach (LengthMesg length in session.Lengths)
        {
            var type = length.GetLengthType() ?? LengthType.Invalid;
            float elapsedTime = length.GetTotalElapsedTime() ?? 0;
            double speed = (length.GetAvgSpeed() ?? 0) * unitConversion;
            ushort? totalStrokes = length.GetTotalStrokes();
            var swolf = elapsedTime + (totalStrokes ?? 0);
            double? distancePerStroke = totalStrokes.HasValue ? Math.Round(poolLength * unitConversion / totalStrokes ?? 1, 2) : (double?)null;

            stringBuilder.Append($"{type.ToString()},");
            stringBuilder.Append($"{elapsedTime},");
            stringBuilder.Append($"{(type == LengthType.Active ? poolLengthString : "")},");
            stringBuilder.Append($"{(type == LengthType.Active ? Math.Round(speed, 2).ToString() : "")},");
            stringBuilder.Append($"{(type == LengthType.Active ? totalStrokes.ToString() : "")},");
            stringBuilder.Append($"{(type == LengthType.Active ? swolf.ToString() : "")},");
            stringBuilder.Append($"{(type == LengthType.Active ? distancePerStroke.ToString() : "")},");
            stringBuilder.Append($"{length.GetAvgSwimmingCadence().ToString() ?? ""},");
            stringBuilder.Append($"{length.GetSwimStroke().ToString() ?? ""}");

            stringBuilder.AppendLine();
        }

        return stringBuilder.ToString();

    }
}

