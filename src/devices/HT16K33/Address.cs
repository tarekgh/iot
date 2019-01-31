using System;
using System.Collections.Generic;
using System.Text;

namespace Iot.Device.HT16K33
{
    public enum Address : byte 
    {
        DEFAULT_ADDRESS = 0x70,

        BLINK_CMD = 0x80,
        BLINK_DISPLAYON = 0x01,
        BLINK_OFF = 0x00,
        BLINK_2HZ = 0x02,
        BLINK_1HZ = 0x04,
        BLINK_HALFHZ = 0x06,

        SYSTEM_SETUP = 0x20,
        OSCILLATOR = 0x01,

        CMD_BRIGHTNESS = 0xE0,
        MAX_BRIGHTNESS = 0x0F

    }
}
