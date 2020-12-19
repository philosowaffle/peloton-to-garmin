#region Copyright
////////////////////////////////////////////////////////////////////////////////
// The following FIT Protocol software provided may be used with FIT protocol
// devices only and remains the copyrighted property of Garmin Canada Inc.
// The software is being provided on an "as-is" basis and as an accommodation,
// and therefore all warranties, representations, or guarantees of any kind
// (whether express, implied or statutory) including, without limitation,
// warranties of merchantability, non-infringement, or fitness for a particular
// purpose, are specifically disclaimed.
//
// Copyright 2015 Garmin Canada Inc.
////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Dynastream.Fit
{
   class HrToRecordMesgBroadcastPlugin : IMesgBroadcastPlugin
   {
      #region Fields
      private bool isActivityFile = false;
      private static readonly int INVALID_INDEX = -1;
      private DateTime record_range_start_time = new DateTime(0);
      private int mesg_count = 0;
      private int hr_start_index = INVALID_INDEX;
      private int hr_start_sub_index = INVALID_INDEX;
      #endregion

      public HrToRecordMesgBroadcastPlugin()
      {
      }

      public void OnIncomingMesg(object sender, IncomingMesgEventArgs e)
      {
         switch (e.mesg.Num) {
            case MesgNum.FileId:
               FileIdMesg fileIdMesg = new FileIdMesg(e.mesg);
               if (fileIdMesg.GetType() == File.Activity)
                  isActivityFile = true;
               break;
            case MesgNum.Session:
               SessionMesg sessionMesg = new SessionMesg(e.mesg);
               record_range_start_time = new DateTime(sessionMesg.GetStartTime());
               break;

            case MesgNum.Hr:
               if( hr_start_index == HrToRecordMesgBroadcastPlugin.INVALID_INDEX ) {
                  // Mark the first appearance of an HR message
                  hr_start_index = mesg_count;
                  hr_start_sub_index = 0;
               }
               break;

            default:
               break;
         } // switch

         mesg_count++;
      }

      public void OnBroadcast(object sender, MesgBroadcastEventArgs e)
      {
         List<Mesg> mesgs = e.mesgs;
         if (isActivityFile && (hr_start_index != HrToRecordMesgBroadcastPlugin.INVALID_INDEX)) {
            float? hr_anchor_event_timestamp = 0.0f;
            DateTime hr_anchor_timestamp = new DateTime(0);
            bool hr_anchor_set = false;
            byte? last_valid_hr = 0;
            DateTime last_valid_hr_time = new DateTime(0);

            for (int mesgCounter = 0; mesgCounter < mesgs.Count; mesgCounter++) {
               Mesg mesg = mesgs[mesgCounter];

               if (mesg.Num == MesgNum.Record) {
                  long hrSum = 0;
                  long hrSumCount = 0;

                  // Cast message to record message
                  RecordMesg recordMesg = new RecordMesg(mesg);

                  // Obtain the time for which the record message is valid
                  DateTime record_range_end_time = new DateTime(recordMesg.GetTimestamp());

                  // Need to determine timestamp range which applies to this record
                  bool findingInRangeHrMesgs = true;

                  // Start searching HR mesgs where we left off
                  int hr_mesg_counter = hr_start_index;
                  int hr_sub_mesg_counter = hr_start_sub_index;

                  while(findingInRangeHrMesgs && (hr_mesg_counter < mesgs.Count)) {

                     // Skip over any non HR messages
                     if(mesgs[hr_mesg_counter].Num == MesgNum.Hr) {
                        HrMesg hrMesg = new HrMesg(mesgs[hr_mesg_counter]);

                        // Update HR timestamp anchor, if present
                        if(hrMesg.GetTimestamp() != null && hrMesg.GetTimestamp().GetTimeStamp() != 0) {
                           hr_anchor_timestamp = new DateTime(hrMesg.GetTimestamp());
                           hr_anchor_set = true;

                           if(hrMesg.GetFractionalTimestamp() != null)
                              hr_anchor_timestamp.Add((double)hrMesg.GetFractionalTimestamp());

                           if(hrMesg.GetNumEventTimestamp() == 1) {
                               hr_anchor_event_timestamp = hrMesg.GetEventTimestamp(0);
                           }
                           else {
                               throw new FitException("FIT HrToRecordMesgBroadcastPlugin Error: Anchor HR mesg must have 1 event_timestamp");
                           }
                        }

                        if(hr_anchor_set == false) {
                           // We cannot process any HR messages if we have not received a timestamp anchor
                           throw new FitException("FIT HrToRecordMesgBroadcastPlugin Error: No anchor timestamp received in a HR mesg before diff HR mesgs");
                        }
                        else if(hrMesg.GetNumEventTimestamp() != hrMesg.GetNumFilteredBpm()) {
                           throw new FitException("FIT HrToRecordMesgBroadcastPlugin Error: HR mesg with mismatching event timestamp and filtered bpm");
                        }
                        for(int j = hr_sub_mesg_counter; j < hrMesg.GetNumEventTimestamp(); j++) {

                           // Build up timestamp for each message using the anchor and event_timestamp
                           DateTime hrMesgTime = new DateTime(hr_anchor_timestamp);
                           float? event_timestamp = hrMesg.GetEventTimestamp(j);

                           // Deal with roll over case
                           if(event_timestamp < hr_anchor_event_timestamp) {
                               if ((hr_anchor_event_timestamp - event_timestamp) > ( 1 << 21 )) {
                                   event_timestamp += ( 1 << 22 );
                               }
                           else {
                                   throw new FitException("FIT HrToRecordMesgBroadcastPlugin Error: Anchor event_timestamp is greater than subsequent event_timestamp. This does not allow for correct delta calculation.");
                               }
                           }
                           hrMesgTime.Add((double)(event_timestamp - hr_anchor_event_timestamp));

                           // Check if hrMesgTime is gt record start time
                           // and if hrMesgTime is lte to record end time
                           if((hrMesgTime.CompareTo(record_range_start_time) > 0) &&
                              (hrMesgTime.CompareTo(record_range_end_time) <= 0)) {
                              hrSum += (long)hrMesg.GetFilteredBpm(j);
                              hrSumCount++;
                              last_valid_hr_time = new DateTime(hrMesgTime);

                           }
                           // check if hrMesgTime exceeds the record time
                           else if(hrMesgTime.CompareTo(record_range_end_time) > 0) {

                              // Remember where we left off
                              hr_start_index = hr_mesg_counter;
                              hr_start_sub_index = j;
                              findingInRangeHrMesgs = false;

                              if(hrSumCount > 0) {
                                 // Update record heart rate
                                 last_valid_hr = (byte?)System.Math.Round((((float)hrSum) / hrSumCount), MidpointRounding.AwayFromZero);
                                 recordMesg.SetHeartRate(last_valid_hr);
                                 mesgs[mesgCounter] = (Mesg)recordMesg;
                              }
                              // If no stored HR is available, fill in record messages with the
                              // last valid filtered hr for a maximum of 5 seconds
                              else if((record_range_start_time.CompareTo(last_valid_hr_time) > 0) &&
                                      ((record_range_start_time.GetTimeStamp() - last_valid_hr_time.GetTimeStamp()) < 5)) {
                                 recordMesg.SetHeartRate(last_valid_hr);
                                 mesgs[mesgCounter] = (Mesg)recordMesg;
                              }

                              // Reset HR average
                              hrSum = 0;
                              hrSumCount = 0;

                              record_range_start_time = new DateTime(record_range_end_time);

                              // Breaks out of looping within the event_timestamp array
                              break;
                           }
                        }
                     }
                     hr_mesg_counter++;
                     hr_sub_mesg_counter = 0;
                  }
               }
            }
         }
      }
   }
}