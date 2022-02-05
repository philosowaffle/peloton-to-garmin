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
// Copyright 2020 Garmin Canada Inc.
////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  This file is auto-generated!  Do NOT edit this file.
// Profile Version = 21.40Release
// Tag = production/akw/21.40.00-0-g813c158
////////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;

namespace Dynastream.Fit
{
    /// <summary>
    /// Implements the ClimbPro profile message.
    /// </summary>
    public class ClimbProMesg : Mesg
    {
        #region Fields
        #endregion

        /// <summary>
        /// Field Numbers for <see cref="ClimbProMesg"/>
        /// </summary>
        public sealed class FieldDefNum
        {
            public const byte Timestamp = 253;
            public const byte PositionLat = 0;
            public const byte PositionLong = 1;
            public const byte ClimbProEvent = 2;
            public const byte ClimbNumber = 3;
            public const byte ClimbCategory = 4;
            public const byte CurrentDist = 5;
            public const byte Invalid = Fit.FieldNumInvalid;
        }

        #region Constructors
        public ClimbProMesg() : base(Profile.GetMesg(MesgNum.ClimbPro))
        {
        }

        public ClimbProMesg(Mesg mesg) : base(mesg)
        {
        }
        #endregion // Constructors

        #region Methods
        ///<summary>
        /// Retrieves the Timestamp field
        /// Units: s</summary>
        /// <returns>Returns DateTime representing the Timestamp field</returns>
        public DateTime GetTimestamp()
        {
            Object val = GetFieldValue(253, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return TimestampToDateTime(Convert.ToUInt32(val));
            
        }

        /// <summary>
        /// Set Timestamp field
        /// Units: s</summary>
        /// <param name="timestamp_">Nullable field value to be set</param>
        public void SetTimestamp(DateTime timestamp_)
        {
            SetFieldValue(253, 0, timestamp_.GetTimeStamp(), Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the PositionLat field
        /// Units: semicircles</summary>
        /// <returns>Returns nullable int representing the PositionLat field</returns>
        public int? GetPositionLat()
        {
            Object val = GetFieldValue(0, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToInt32(val));
            
        }

        /// <summary>
        /// Set PositionLat field
        /// Units: semicircles</summary>
        /// <param name="positionLat_">Nullable field value to be set</param>
        public void SetPositionLat(int? positionLat_)
        {
            SetFieldValue(0, 0, positionLat_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the PositionLong field
        /// Units: semicircles</summary>
        /// <returns>Returns nullable int representing the PositionLong field</returns>
        public int? GetPositionLong()
        {
            Object val = GetFieldValue(1, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToInt32(val));
            
        }

        /// <summary>
        /// Set PositionLong field
        /// Units: semicircles</summary>
        /// <param name="positionLong_">Nullable field value to be set</param>
        public void SetPositionLong(int? positionLong_)
        {
            SetFieldValue(1, 0, positionLong_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ClimbProEvent field</summary>
        /// <returns>Returns nullable ClimbProEvent enum representing the ClimbProEvent field</returns>
        public ClimbProEvent? GetClimbProEvent()
        {
            object obj = GetFieldValue(2, 0, Fit.SubfieldIndexMainField);
            ClimbProEvent? value = obj == null ? (ClimbProEvent?)null : (ClimbProEvent)obj;
            return value;
        }

        /// <summary>
        /// Set ClimbProEvent field</summary>
        /// <param name="climbProEvent_">Nullable field value to be set</param>
        public void SetClimbProEvent(ClimbProEvent? climbProEvent_)
        {
            SetFieldValue(2, 0, climbProEvent_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ClimbNumber field</summary>
        /// <returns>Returns nullable ushort representing the ClimbNumber field</returns>
        public ushort? GetClimbNumber()
        {
            Object val = GetFieldValue(3, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToUInt16(val));
            
        }

        /// <summary>
        /// Set ClimbNumber field</summary>
        /// <param name="climbNumber_">Nullable field value to be set</param>
        public void SetClimbNumber(ushort? climbNumber_)
        {
            SetFieldValue(3, 0, climbNumber_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the ClimbCategory field</summary>
        /// <returns>Returns nullable byte representing the ClimbCategory field</returns>
        public byte? GetClimbCategory()
        {
            Object val = GetFieldValue(4, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToByte(val));
            
        }

        /// <summary>
        /// Set ClimbCategory field</summary>
        /// <param name="climbCategory_">Nullable field value to be set</param>
        public void SetClimbCategory(byte? climbCategory_)
        {
            SetFieldValue(4, 0, climbCategory_, Fit.SubfieldIndexMainField);
        }
        
        ///<summary>
        /// Retrieves the CurrentDist field
        /// Units: m</summary>
        /// <returns>Returns nullable float representing the CurrentDist field</returns>
        public float? GetCurrentDist()
        {
            Object val = GetFieldValue(5, 0, Fit.SubfieldIndexMainField);
            if(val == null)
            {
                return null;
            }

            return (Convert.ToSingle(val));
            
        }

        /// <summary>
        /// Set CurrentDist field
        /// Units: m</summary>
        /// <param name="currentDist_">Nullable field value to be set</param>
        public void SetCurrentDist(float? currentDist_)
        {
            SetFieldValue(5, 0, currentDist_, Fit.SubfieldIndexMainField);
        }
        
        #endregion // Methods
    } // Class
} // namespace
