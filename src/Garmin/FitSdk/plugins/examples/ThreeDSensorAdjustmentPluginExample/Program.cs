#region copyright
////////////////////////////////////////////////////////////////////////////////
// The following FIT Protocol software provided may be used with FIT protocol
// devices only and remains the copyrighted property of Garmin Canada Inc.
// The software is being provided on an "as-is" basis and as an accommodation,
// and therefore all warranties, representations, or guarantees of any kind
// (whether express, implied or statutory) including, without limitation,
// warranties of merchantability, non-infringement, or fitness for a particular
// purpose, are specifically disclaimed.
//
// Copyright 2016 Garmin Canada Inc.
////////////////////////////////////////////////////////////////////////////////
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

using Dynastream.Fit;

namespace ThreeDSensorAdjustmentPluginExample
{
    class Program
    {
        private static FileStream fitSource;

        static void Main(string[] args)
        {
            StreamWriter sw;
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ThreeDSensorAdjustmentPluginExample.exe <filename>");
                return;
            }
            try
            {
                // Attempt to open .FIT file
                fitSource = new FileStream(args[0], FileMode.Open);
                Console.WriteLine("Opening {0}", args[0]);

                //Attempt to create an output file
                string fileName = String.Format("{0}.csv", args[0].Split('.')); //Strip off the first part of the file name
                FileStream fs = new FileStream(fileName, FileMode.Create);

                // First, save the standard output.
                sw = new StreamWriter(fs);
                sw.AutoFlush = true;
                Console.SetOut(sw);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ThreeDSensorAdjustmentPluginExample caught Exception: " + ex.Message);
                return;
            }

            Decode decodeDemo = new Decode();
            BufferedMesgBroadcaster mesgBroadcaster = new BufferedMesgBroadcaster();

            // Connect the Broadcaster to our events (message) source (in this case the Decoder)
            decodeDemo.MesgEvent += mesgBroadcaster.OnMesg;
            decodeDemo.MesgDefinitionEvent += mesgBroadcaster.OnMesgDefinition;

            //Subscribe to the message events of the interest by connecting to the Broadcaster
            mesgBroadcaster.MesgEvent += OnMesg;

            IMesgBroadcastPlugin plugin = new ThreeDSensorAdjustmentPlugin();
            mesgBroadcaster.RegisterMesgBroadcastPlugin(plugin);

            try
            {
                //Writing headers for columns
                int maxFieldNum = 9;
                Console.Write("Type,Local Number,Message,");
                for (int i = 1; i <= maxFieldNum; i++)
                {
                    Console.Write("Field {0},Value {0},Units {0},", i);
                }
                Console.WriteLine();

                //Attempting to Decode the file
                decodeDemo.Read(fitSource);
                mesgBroadcaster.Broadcast();
            }
            catch (FitException ex)
            {
                Console.WriteLine("ThreeDSensorAdjustmentPluginExample caught Exception: decoding threw a FitException: " + ex.Message);
            }

            fitSource.Close();
            sw.Close();
            return;
        }

        static void OnMesg(object sender, MesgEventArgs e)
        {
            Mesg mesg = e.mesg;
            switch (mesg.Num)
            {
                case MesgNum.FileId:
                    FileIdMesg fileIdMesg = new FileIdMesg(mesg);
                    Console.Write("Data,{0},{1},", fileIdMesg.LocalNum, fileIdMesg.Name);
                    PrintField(fileIdMesg);
                    break;

                case MesgNum.ThreeDSensorCalibration:
                    ThreeDSensorCalibrationMesg calMesg = new ThreeDSensorCalibrationMesg(mesg);
                    Console.Write("Data,{0},{1},", calMesg.LocalNum, calMesg.Name);
                    PrintField(calMesg);
                    break;

                case MesgNum.AccelerometerData:
                    AccelerometerDataMesg accelMesg = new AccelerometerDataMesg(mesg);
                    Console.Write("Data,{0},{1},", accelMesg.LocalNum, accelMesg.Name);
                    PrintField(accelMesg);
                    break;

                case MesgNum.GyroscopeData:
                    GyroscopeDataMesg gyroMesg = new GyroscopeDataMesg(mesg);
                    Console.Write("Data,{0},{1},", gyroMesg.LocalNum, gyroMesg.Name);
                    PrintField(gyroMesg);
                    break;

                case MesgNum.MagnetometerData:
                    MagnetometerDataMesg magMesg = new MagnetometerDataMesg(mesg);
                    Console.Write("Data,{0},{1},", magMesg.LocalNum, magMesg.Name);
                    PrintField(magMesg);
                    break;

                default:
                    break;
            }
        }

        private static void PrintField(Mesg mesg)
        {
            ushort activeSubfieldIndex;
            string name;
            string value;
            string units;

            //Loop through each field
            foreach (Field field in mesg.Fields)
            {
                if (mesg.GetFieldValue(field.Num) != null)
                {
                    //Set the name, value, and units to their standard values
                    name = field.Name;
                    value = (field.GetValue()).ToString();
                    units = field.GetUnits();

                    //Checks if there is an active subfield and updates the name and units appropriately
                    activeSubfieldIndex = mesg.GetActiveSubFieldIndex(field.Num);
                    if (activeSubfieldIndex != Fit.SubfieldIndexMainField)
                    {
                        name = field.GetName((byte)activeSubfieldIndex);
                        units = field.GetUnits((byte)activeSubfieldIndex);
                    }

                    //Checks if a field has multiple values and updates value appropriately
                    if (field.GetNumValues() > 1)
                    {
                        value = FieldArrayToString(field);
                    }

                    Console.Write("{0},{1},{2},", name, value, units);
                }
            }
            Console.WriteLine();
        }

        //Grabs all the values in a field and creates a string of them joined together by "|"
        private static string FieldArrayToString(Field field)
        {
            string fieldArrayString = (field.GetValue(0)).ToString();
            int count = field.GetNumValues();
            for (int i = 1; i < count; i++)
            {
                fieldArrayString = String.Concat(fieldArrayString, ("|" + (field.GetValue((byte)i))).ToString());
            }
            return fieldArrayString;
        }
    }
}
