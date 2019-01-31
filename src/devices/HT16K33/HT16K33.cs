// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.I2c.Drivers;
using System.Device.Spi;
using System.Device.Spi.Drivers;
using System.Linq;

namespace Iot.Device.HT16K33
{
    public class HT16K33 : IDisposable
    {
        const int defaultBusID = 1; //Raspberry Pi 3
        byte[] buffer = Enumerable.Repeat<byte>(0x00, 16).ToArray();
        //Buffer buffer
        private I2cDevice _i2CDevice;

        public HT16K33(): this(new I2cConnectionSettings(defaultBusID, (byte)Address.DEFAULT_ADDRESS))
        {}

        public HT16K33(I2cConnectionSettings i2CConnectionSettings): this(new UnixI2cDevice(i2CConnectionSettings))
        {}

        public HT16K33(I2cDevice i2CDevice)
        {
            _i2CDevice = i2CDevice;
        }

        /**
         * Begin()
         * Initialize driver with LEDs enabled and all turned off.*
         */

        public void Begin()
        {
            //Enable LEDs & Turn on the oscillator.

            _i2CDevice.Write(new Span<byte>(new byte[] 
                                    {  (byte)Address.SYSTEM_SETUP,
                                       (byte)Address.OSCILLATOR
                                    }));

            //Turn display on with no blinking.
            SetBlink((int)Address.BLINK_OFF);

            //Set display to full brightness.
            SetBrightness((int)Address.MAX_BRIGHTNESS);
            
        }

        /**
         * Set Blink()
         * Blink display at specified frequency.  
         * Note that frequency must be a value allowed by the HT16K33
         **/
        public void SetBlink(int frequency)
        {
            
            if (!Enum.IsDefined(typeof(Address), frequency))
            {
                throw new NotSupportedException("Frequency must be one of BLINK_OFF:0x00, BLINK_2HZ:0x02, BLINK_1HZ:0x04, or BLINK_HALFHZ:0x06");
            }

            _i2CDevice.Write(new Span<byte>(new byte[] 
                                { (byte)Address.BLINK_CMD,
                                  (byte)Address.BLINK_DISPLAYON,
                                  (byte)frequency
                                }));
            
        }

        public void SetBrightness(int brightness)
        {

            if (brightness < 0 || brightness > 15)
            {
                throw new NotSupportedException("Brightness must be a value of 0 to 15.");
            }

            _i2CDevice.Write(new Span<byte>(new byte[] 
                                    { (byte)Address.CMD_BRIGHTNESS,
                                      (byte)brightness
                                    }));
           
        }

        public void SetLED(int led, int value)
        {
            int pos, offset;

            if (led < 0 || led > 127)
            {
                throw new NotSupportedException("LED must be value of 0 to 127.");
            }

            //Calculate position in byte buffer and bit offset of desired LED.
            pos = led / 8;
            offset = led % 8;

            buffer[pos] = value == 0 ? buffer[pos] &= (byte)~(1<<offset)
                                : buffer[pos] |= (byte)(1 << offset);
            
        }


        /**
         * WriteDisplay()
         * Write Display buffer to hardware
        **/
        public void WriteDisplay()
        {

            /*"""Write display buffer to display hardware."""
             *

            for i, value in enumerate(self.buffer) :

                self._device.write8(i, value) */
        }
        
        public void Clear()
        {
          /*"""Clear contents of display buffer."""*/
             buffer = Enumerable.Repeat<byte>(0, 16).ToArray();
        }

        public void Dispose()
        {
        }
    }
}
