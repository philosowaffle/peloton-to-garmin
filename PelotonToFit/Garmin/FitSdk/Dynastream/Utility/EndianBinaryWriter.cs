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

namespace Dynastream.Utility
{
    /// <summary>
    /// Extend framework BinaryWriter to support BigEndian destinations.
    /// When writing multibyte values, the bytes are reordered appropriately.
    /// </summary>
    public class EndianBinaryWriter : BinaryWriter
    {
        #region Fields
        private bool isBigEndian = false;
        #endregion

        #region Properties
        public bool IsBigEndian
        {
            get { return isBigEndian; }
            set { isBigEndian = value; }
        }
        #endregion

        #region Constructors
        public EndianBinaryWriter(Stream output, Encoding encoding, bool isBigEndian)
            : base(output, encoding)
        {
            this.isBigEndian = isBigEndian;
        }

        public EndianBinaryWriter(Stream output, bool isBigEndian)
            : this(output, Encoding.UTF8, isBigEndian)
        {
        }
        #endregion

        #region Methods
        public override void Write(short value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 2);
        }

        public override void Write(ushort value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 2);
        }

        public override void Write(int value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        public override void Write(uint value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        public override void Write(long value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }

        public override void Write(ulong value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }

        public override void Write(float value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        public override void Write(double value)
        {
            if (!IsBigEndian)
            {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }
        #endregion
    }
} // namespace