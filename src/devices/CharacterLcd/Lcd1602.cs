﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Device.Gpio;
using System.Device.I2c.Devices;
using System.Drawing;

namespace Iot.Device.CharacterLcd
{
    /// <summary>
    /// 16x2 HD44780 compatible character LCD display.
    /// </summary>
    public class Lcd1602 : Hd44780
    {
        /// <summary>
        /// Constructs a new HD44780 based 16x2 LCD controller, using GPIO pins.
        /// </summary>
        /// <param name="registerSelectPin">The pin that controls the regsiter select.</param>
        /// <param name="enablePin">The pin that controls the enable switch.</param>
        /// <param name="dataPins">Collection of pins holding the data that will be printed on the screen.</param>
        /// <param name="backlightPin">The optional pin that controls the backlight of the display.</param>
        /// <param name="backlightBrightness">The brightness of the backlight. 0.0 for off, 1.0 for on.</param>
        /// <param name="readWritePin">The optional pin that controls the read and write switch.</param>
        /// <param name="controller">The controller to use with the LCD. If not specified, uses the platform default.</param>
        public Lcd1602(int registerSelectPin, int enablePin, int[] dataPins, int backlightPin = -1, float backlightBrightness = 1.0f, int readWritePin = -1, IGpioController controller = null)
            : base(new Size(16, 2), LcdInterface.CreateGpio(registerSelectPin, enablePin, dataPins, backlightPin, backlightBrightness, readWritePin, controller))
        {
        }

        /// <summary>
        /// Constructs a new HD44780 based 16x2 LCD controller with integrated I2c support.
        /// </summary>
        /// <remarks>
        /// This is for on-chip I2c support. For connecting via I2c GPIO expanders, use the GPIO constructor <see cref="Lcd1602(int, int, int[], int, float, int, IGpioController)"/>.
        /// </remarks>
        /// <param name="device">The I2c device for the LCD.</param>
        public Lcd1602(I2cDevice device)
            : base(new Size(16, 2), LcdInterface.CreateI2c(device))
        {
        }
    }
}
