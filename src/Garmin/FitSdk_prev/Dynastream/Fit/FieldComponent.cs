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

namespace Dynastream.Fit
{
    /// <summary>
    ///
    /// </summary>
    internal class FieldComponent
    {
        #region Fields
        internal byte fieldNum;
        internal bool accumulate;
        internal int bits;
        internal double scale;
        internal double offset;

        internal long accumulatedValue = 0;
        internal long lastValue = 0;
        #endregion // Fields

        #region Properties
        #endregion // Properties

        #region Constructors
        internal FieldComponent(byte fieldNum, bool accumulate, int bits, double scale, double offset)
        {
            this.fieldNum = fieldNum;
            this.accumulate = accumulate;
            this.bits = bits;
            this.scale = scale;
            this.offset = offset;
        }

        internal FieldComponent(FieldComponent component)
        {
            this.fieldNum = component.fieldNum;
            this.accumulate = component.accumulate;
            this.bits = component.bits;
            this.scale = component.scale;
            this.offset = component.offset;
            this.accumulatedValue = component.accumulatedValue;
            this.lastValue = component.lastValue;
        }
        #endregion // Constructors

        #region Methods
        public long Accumulate(long value)
        {
            long mask = (1L << bits) - 1;

            accumulatedValue += (value - lastValue) & mask;
            lastValue = value;

            return accumulatedValue;
        }
        #endregion // Methods
    } // Class
} // namespace
