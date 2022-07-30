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
using System.Diagnostics;
using System.Text;
using System.IO;

namespace Dynastream.Fit
{
    public class ThreeDSensorAdjustmentPlugin : IMesgBroadcastPlugin
    {

        #region Fields
        private const int NUM_AXIS = 3;
        private const int NUM_COLUMNS = 3;
        private const int NUM_ROWS = 3;
        private const int X_AXIS_OFFSET = 0;
        private const int Y_AXIS_OFFSET = 1;
        private const int Z_AXIS_OFFSET = 2;
        private const ushort accelDataFieldNum = MesgNum.AccelerometerData;
        private readonly string[] accelDataFieldNameXYZ = { "CalibratedAccelX", "CalibratedAccelY", "CalibratedAccelZ" };
        private const ushort gyroDataFieldNum = MesgNum.GyroscopeData;
        private readonly string[] gyroDataFieldNameXYZ = {"CalibratedGyroX", "CalibratedGyroY", "CalibratedGyroZ"};
        private const ushort magDataFieldNum = MesgNum.MagnetometerData;
        private readonly string[] magDataFieldNameXYZ = {"CalibratedMagX", "CalibratedMagY", "CalibratedMagZ"};
        private bool haveAccelCal = false;
        private bool haveGyroCal = false;
        private bool haveMagCal = false;
        private CalibrationParameters accelCalParams = new CalibrationParameters();
        private CalibrationParameters gyroCalParams = new CalibrationParameters();
        private CalibrationParameters magCalParams = new CalibrationParameters();
        #endregion

        #region Construcotrs
        public ThreeDSensorAdjustmentPlugin()
        {
        }
        #endregion

        private class CalibrationParameters
        {
            #region Fields
            private long[] channelOffset = new long[NUM_ROWS];
            private float[,] rotationMatrix = new float[NUM_COLUMNS, NUM_ROWS];
            #endregion

            #region Properties
            internal long CalDivisor { get; private set; }
            internal long CalFactor { get; private set; }
            internal long[] ChannelOffset
            {
                get { return channelOffset; }
            }
            internal long LevelShift { get; private set; }
            internal float[,] RotationMatrix
            {
                get { return rotationMatrix; }
            }
            #endregion

            #region Methods
            public void LoadParams(ThreeDSensorCalibrationMesg calMesg)
            {
                this.CalFactor = (long)calMesg.GetCalibrationFactor();
                this.CalDivisor = (long)calMesg.GetCalibrationDivisor();
                this.LevelShift = (long)calMesg.GetLevelShift();

                this.channelOffset[X_AXIS_OFFSET] = (long)calMesg.GetOffsetCal(X_AXIS_OFFSET);
                this.channelOffset[Y_AXIS_OFFSET] = (long)calMesg.GetOffsetCal(Y_AXIS_OFFSET);
                this.channelOffset[Z_AXIS_OFFSET] = (long)calMesg.GetOffsetCal(Z_AXIS_OFFSET);

                // Rotation Matrix row major
                this.rotationMatrix[0,0] = (float)calMesg.GetOrientationMatrix(0);
                this.rotationMatrix[0,1] = (float)calMesg.GetOrientationMatrix(1);
                this.rotationMatrix[0,2] = (float)calMesg.GetOrientationMatrix(2);
                this.rotationMatrix[1,0] = (float)calMesg.GetOrientationMatrix(3);
                this.rotationMatrix[1,1] = (float)calMesg.GetOrientationMatrix(4);
                this.rotationMatrix[1,2] = (float)calMesg.GetOrientationMatrix(5);
                this.rotationMatrix[2,0] = (float)calMesg.GetOrientationMatrix(6);
                this.rotationMatrix[2,1] = (float)calMesg.GetOrientationMatrix(7);
                this.rotationMatrix[2,2] = (float)calMesg.GetOrientationMatrix(8);
            }
            #endregion
        }

        #region Methods
        public void OnIncomingMesg(object sender, IncomingMesgEventArgs e)
        {
            switch (e.mesg.Num)
            {
                case MesgNum.ThreeDSensorCalibration:
                    ThreeDSensorCalibrationMesg calMesg = new ThreeDSensorCalibrationMesg(e.mesg);
                    switch (calMesg.GetSensorType())
                    {
                        case SensorType.Accelerometer:
                            accelCalParams.LoadParams(calMesg);
                            haveAccelCal = true;
                            break;
                        case SensorType.Gyroscope:
                            gyroCalParams.LoadParams(calMesg);
                            haveGyroCal = true;
                            break;
                        case SensorType.Compass:
                            magCalParams.LoadParams(calMesg);
                            haveMagCal = true;
                            break;
                        default:
                            break;

                    } // switch
                    break;
                default:
                    break;

            } //switch
        }

        private float[] AdjustSensorData(int[] rawData, CalibrationParameters calParams)
        {
            float[] calibratedValues = new float[rawData.Length];
            float[] rotatedValues = new float[rawData.Length];

            //Apply the calibration parameters
            for (int i = 0; i < rawData.Length; i++)
            {
                calibratedValues[i] = (float)rawData[i];
                calibratedValues[i] -= calParams.LevelShift;
                calibratedValues[i] -= calParams.ChannelOffset[i];
                calibratedValues[i] *= calParams.CalFactor;
                calibratedValues[i] /= calParams.CalDivisor;
            }

            // Apply the rotation matrix
            // [Rotation] * [XYZ]
            rotatedValues[0] = (calParams.RotationMatrix[0, 0] * calibratedValues[0]) + (calParams.RotationMatrix[0, 1] * calibratedValues[1]) + (calParams.RotationMatrix[0, 2] * calibratedValues[2]);
            rotatedValues[1] = (calParams.RotationMatrix[1, 0] * calibratedValues[0]) + (calParams.RotationMatrix[1, 1] * calibratedValues[1]) + (calParams.RotationMatrix[1, 2] * calibratedValues[2]);
            rotatedValues[2] = (calParams.RotationMatrix[2, 0] * calibratedValues[0]) + (calParams.RotationMatrix[2, 1] * calibratedValues[1]) + (calParams.RotationMatrix[2, 2] * calibratedValues[2]);

            return rotatedValues;
        }

        public void OnBroadcast(object sender, MesgBroadcastEventArgs e)
        {
            float[] calibratedXYZ;
            int count;
            List<Mesg> mesgs = e.mesgs;
            int[] rawXYZ = new int[NUM_AXIS];

            foreach (Mesg mesg in mesgs)
            {
                switch (mesg.Num)
                {
                    case MesgNum.AccelerometerData:
                        if (haveAccelCal)
                        {
                            AccelerometerDataMesg accelData = new AccelerometerDataMesg(mesg);
                            count = accelData.GetNumAccelX();
                            for(int i = 0; i < count; i++ )
                            {
                                //Extract the uncalibrated accel data from incoming message
                                rawXYZ[X_AXIS_OFFSET] = Convert.ToInt32(accelData.GetAccelX(i));
                                rawXYZ[Y_AXIS_OFFSET] = Convert.ToInt32(accelData.GetAccelY(i));
                                rawXYZ[Z_AXIS_OFFSET] = Convert.ToInt32(accelData.GetAccelZ(i));

                                // Apply calibration to the values
                                calibratedXYZ = AdjustSensorData( rawXYZ, accelCalParams );

                                // Update the message
                                ProcessCalibrationFactor( mesg, accelDataFieldNameXYZ, calibratedXYZ, accelDataFieldNum );
                            }
                        }
                        break;
                    case MesgNum.GyroscopeData:
                        if (haveGyroCal)
                        {
                            GyroscopeDataMesg gyroData = new GyroscopeDataMesg(mesg);
                            count = gyroData.GetNumGyroX();
                            for (int i = 0; i < count; i++)
                            {
                                //Extract the uncalibrated gyro data from incoming message
                                rawXYZ[X_AXIS_OFFSET] = Convert.ToInt32(gyroData.GetGyroX(i));
                                rawXYZ[Y_AXIS_OFFSET] = Convert.ToInt32(gyroData.GetGyroY(i));
                                rawXYZ[Z_AXIS_OFFSET] = Convert.ToInt32(gyroData.GetGyroZ(i));

                                // Apply calibration to the values
                                calibratedXYZ = AdjustSensorData( rawXYZ, gyroCalParams);

                                // Update the message
                                ProcessCalibrationFactor( mesg, gyroDataFieldNameXYZ, calibratedXYZ, gyroDataFieldNum );
                            }
                        }
                        break;
                    case MesgNum.MagnetometerData:
                        if (haveMagCal)
                        {
                            MagnetometerDataMesg magData = new MagnetometerDataMesg(mesg);
                            count = magData.GetNumMagX();
                            for (int i = 0; i < count; i++)
                            {
                                //Extract the uncalibrated mag data from incoming message
                                rawXYZ[X_AXIS_OFFSET] = Convert.ToInt32(magData.GetMagX(i));
                                rawXYZ[Y_AXIS_OFFSET] = Convert.ToInt32(magData.GetMagY(i));
                                rawXYZ[Z_AXIS_OFFSET] = Convert.ToInt32(magData.GetMagZ(i));

                                // Apply calibration to the values
                                calibratedXYZ = AdjustSensorData( rawXYZ, magCalParams);

                                // Update the message
                                ProcessCalibrationFactor( mesg, magDataFieldNameXYZ, calibratedXYZ, magDataFieldNum );
                            }
                        }
                        break;
                    default:
                        break;
                }// switch
            }// foreach
        }

        private void ProcessCalibrationFactor( Mesg mesg, string[] fieldsXYZ, float[] calibratedXYZ, ushort globalMesgNum )
        {
            if ((fieldsXYZ.Length != NUM_AXIS) || (calibratedXYZ.Length != NUM_AXIS))
            {
                //Invalid number of arguments
                return;
            }

            //Add the newly calculated calibrated values to the calibrated data fields
            if ( mesg.GetField(fieldsXYZ[X_AXIS_OFFSET]) == null )
            {
                mesg.SetField(new Field(Profile.GetField(globalMesgNum, fieldsXYZ[X_AXIS_OFFSET])));
            }

            if (mesg.GetField(fieldsXYZ[Y_AXIS_OFFSET]) == null)
            {
                mesg.SetField(new Field(Profile.GetField(globalMesgNum, fieldsXYZ[Y_AXIS_OFFSET])));
            }

            if (mesg.GetField(fieldsXYZ[Z_AXIS_OFFSET]) == null)
            {
                mesg.SetField(new Field(Profile.GetField(globalMesgNum, fieldsXYZ[Z_AXIS_OFFSET])));
            }

            mesg.GetField(fieldsXYZ[X_AXIS_OFFSET]).AddValue(calibratedXYZ[X_AXIS_OFFSET]);
            mesg.GetField(fieldsXYZ[Y_AXIS_OFFSET]).AddValue(calibratedXYZ[Y_AXIS_OFFSET]);
            mesg.GetField(fieldsXYZ[Z_AXIS_OFFSET]).AddValue(calibratedXYZ[Z_AXIS_OFFSET]);
        }
        #endregion
    } // Class
} // namespace
