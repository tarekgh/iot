﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Iot.Device.BrickPi3.Models;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace Iot.Device.BrickPi3.Sensors
{
    /// <summary>
    /// Create a NXT Sound sensor
    /// </summary>
    public class NXTSoundSensor : INotifyPropertyChanged, ISensor
    {

        private const int NXTCutoff = 512;

        private Brick _brick = null;
        private Timer _timer = null;
        private int _periodRefresh;
        private int _value;
        private string _valueAsString;

        /// <summary>
        /// Initialize a NXT Sound Sensor
        /// </summary>
        /// <param name="brick"></param>
        /// <param name="port">Sensor Port</param>
        public NXTSoundSensor(Brick brick, SensorPort port) : this(brick, port, 1000) { }

        /// <summary>
        /// Initialize a NXT Sound Sensor
        /// </summary>
        /// <param name="brick"></param>
        /// <param name="port">Sensor port</param>
        /// <param name="timeout">Period in millisecond to check sensor value changes</param>
        public NXTSoundSensor(Brick brick, SensorPort port, int timeout)
        {
            _brick = brick;
            Port = port;
            brick.SetSensorType((byte)Port, SensorType.Custom, new int[] { (int)SensorCustom.Pin1_9V });
            _periodRefresh = timeout;
            _timer = new Timer(UpdateSensor, this, TimeSpan.FromMilliseconds(timeout), TimeSpan.FromMilliseconds(timeout));
        }

        private void StopTimerInternal()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// To notify a property has changed. The minimum time can be set up
        /// with timeout property
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Period to refresh the notification of property changed in milliseconds
        /// </summary>
        public int PeriodRefresh
        {
            get { return _periodRefresh; }

            set
            {
                _periodRefresh = value;
                _timer.Change(TimeSpan.FromMilliseconds(_periodRefresh), TimeSpan.FromMilliseconds(_periodRefresh));
            }
        }

        /// <summary>
        /// Return the raw value of the sensor
        /// </summary>
        public int Value
        {
            get { return ReadRaw(); }

            internal set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// Return the raw value  as a string of the sensor
        /// </summary>
        public string ValueAsString
        {
            get { return ReadAsString(); }

            internal set
            {
                if (_valueAsString != value)
                {
                    _valueAsString = value;
                    OnPropertyChanged(nameof(ValueAsString));
                }
            }
        }

        /// <summary>
        /// Update the sensor and this will raised an event on the interface
        /// </summary>
        public void UpdateSensor(object state)
        {
            Value = ReadRaw();
            ValueAsString = ReadAsString();
        }

        /// <summary>
        /// Reads the sensor value as a string.
        /// </summary>
        /// <returns>The value as a string</returns>
        public string ReadAsString()
        {
            return Read().ToString();
        }

        private int Read()
        {
            try
            {
                var ret = _brick.GetSensor((byte)Port);
                return ((((ret[2] & 0xE0) >> 5) + (ret[3] << 8)));
            }
            catch (Exception ex) when (ex is IOException)
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Reads the raw sensor value
        /// </summary>
        /// <returns>The raw.</returns>
        public int ReadRaw()
        {
            try
            {
                var ret = _brick.GetSensor((byte)Port);
                return ((ret[2] & 0xE0) >> 5) + (ret[3] << 8);
            }
            catch (Exception)
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Return port
        /// </summary>
        public SensorPort Port { get; }

        public string GetSensorName()
        {
            return "NXT Sound";
        }

        public int NumberOfModes()
        {
            return 1;
        }

        public string SelectedMode()
        {
            return "Analog";
        }

        public void SelectNextMode() { }

        public void SelectPreviousMode() { }
    }
}
