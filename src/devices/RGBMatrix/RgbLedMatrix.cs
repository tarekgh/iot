// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Interop;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Runtime.CompilerServices;
using Iot.Device.Graphics;

namespace Iot.Device.LEDMatrix
{
    public class RGBMatrix
    {
        private GpioController _controller;
        private Gpio _gpio;
        private PinMapping _mapping;
        private int        _width; // number of columns
        private int        _rows; // number of rows
        private int        _isRendering;
        private bool       _safeToDispose;
        private bool       _swapRequested;
        private byte [] _colorsBuffer;
        private byte [] _colorsBackBuffer;
        private ulong [] _rowSetMasks;
        private ulong [] colorsMake;
        private long _duration = (long) (Stopwatch.Frequency * 1800 / 1E9); // 100 nanoseconds;
        private long _expirationTicks;
        private bool _showFrameTime;

        public RGBMatrix(PinMapping mapping, int width, int rows)
        {
            _mapping = mapping;
            _gpio = new Gpio(mapping, rows);
            _controller = new GpioController(PinNumberingScheme.Logical, _gpio);

            OpenAndWriteToPin(_mapping.A, PinValue.Low);
            OpenAndWriteToPin(_mapping.B, PinValue.Low);
            OpenAndWriteToPin(_mapping.C, PinValue.Low);

            if (rows > 16)
            {
                OpenAndWriteToPin(_mapping.D, PinValue.Low);
            }

            if (rows > 32)
            {
                _duration = (long) ((double) Stopwatch.Frequency * 400 / 1E9);
                OpenAndWriteToPin(_mapping.E, PinValue.Low);
            }

            // OE set High means disable output (confusing)
            OpenAndWriteToPin(_mapping.OE, PinValue.High);
            OpenAndWriteToPin(_mapping.Clock, PinValue.Low);
            OpenAndWriteToPin(_mapping.Latch, PinValue.Low);

            OpenAndWriteToPin(_mapping.R1, PinValue.Low);
            OpenAndWriteToPin(_mapping.G1, PinValue.Low);
            OpenAndWriteToPin(_mapping.B1, PinValue.Low);
            OpenAndWriteToPin(_mapping.R2, PinValue.Low);
            OpenAndWriteToPin(_mapping.G2, PinValue.Low);
            OpenAndWriteToPin(_mapping.B2, PinValue.Low);

            _rowSetMasks   = new ulong [rows >> 1];

            for (int i = 1; i < rows >> 1; i++)
            {
                if ((i & 1)    != 0) _rowSetMasks[i] |= _gpio.AMask;
                if ((i & 2)    != 0) _rowSetMasks[i] |= _gpio.BMask;
                if ((i & 4)    != 0) _rowSetMasks[i] |= _gpio.CMask;
                if ((i & 8)    != 0) _rowSetMasks[i] |= _gpio.DMask;
                if ((i & 0x10) != 0) _rowSetMasks[i] |= _gpio.EMask;
            }

            colorsMake = new ulong[16]; // 8 for RGB1 and 8 for RGB2
            for (int i = 1; i < 8; i++)
            {
                if ((i & 1) != 0) { colorsMake[i] |= _gpio.R1Mask; colorsMake[i + 8] |= _gpio.R2Mask; }
                if ((i & 2) != 0) { colorsMake[i] |= _gpio.G1Mask; colorsMake[i + 8] |= _gpio.G2Mask; }
                if ((i & 4) != 0) { colorsMake[i] |= _gpio.B1Mask; colorsMake[i + 8] |= _gpio.B2Mask; }
            }

            _colorsBuffer = new byte[8 * width * (rows >> 1)];
            _colorsBackBuffer = new byte[8 * width * (rows >> 1)];

            _width = width;
            _rows = rows;

            Brightness = 255;

            _safeToDispose = true;
            _swapRequested = false;
        }

        public int Width => _width;
        public int Height => _rows;
        public byte Brightness { set; get; }

        public long PWMDuration
        {
            get => (long) (((double) _duration / Stopwatch.Frequency) * 1E9);
            set =>  _duration = (long) ((double) Stopwatch.Frequency * value / 1E9); // value nanoseconds;
        }

        public bool ShowFrameTime { set => _showFrameTime = true; }

        public unsafe void Fill(byte red, byte green, byte blue)
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    SetPixel(i, j, red, green, blue);
                }
            }
        }

        // public unsafe void Fill(byte red, byte green, byte blue)
        // {
        //     // SetPixel(0, 0, red, green, blue);
        //     // SetPixel(0, _rows >> 1, red, green, blue);
        //     SetBackBufferPixel(0, 0, red, green, blue);
        //     SetBackBufferPixel(0, _rows >> 1, red, green, blue);

        //     // fixed (byte *pColorsBuffer = _colorsBackBuffer)
        //     fixed (byte *pColorsBuffer = _colorsBuffer)
        //     {
        //         ulong* pBuffer = (ulong*) pColorsBuffer;

        //         for (int i = 1; i < (_colorsBuffer.Length / 8); i++)
        //         {
        //             pBuffer[i] = pBuffer[0];
        //         }
        //     }

        //     SwapBuffers();
        // }

        public void SetPixel(int column, int row, byte red, byte green, byte blue)
        {
            if ((column | row) < 0 || column >= Width || row >= Height)
            {
                return;
            }

            red = s_gamma[red];
            green = s_gamma[green];
            blue = s_gamma[blue];

            int pos = 8 * column + 8 * (row % (_rows >> 1)) * _width;
            byte mask = (byte) (row >= (_rows >> 1) ? 0x08 : 0x01);

            // red   = (byte) (Brightness * red   / 255);
            // green = (byte) (Brightness * green / 255);
            // blue  = (byte) (Brightness * blue  / 255);

            for (int i = 0; i < 8; i++)
            {
                int bit = 1 << i;

                if ((red & bit) != 0)
                    _colorsBuffer[pos + i] |= mask;
                else
                    _colorsBuffer[pos + i] &= (byte) (~mask);

                if ((green & bit) != 0)
                    _colorsBuffer[pos + i] |= (byte) (mask << 1);
                else
                    _colorsBuffer[pos + i] &= (byte) ~(mask << 1);

                if ((blue & bit) != 0)
                    _colorsBuffer[pos + i] |= (byte) (mask << 2);
                else
                    _colorsBuffer[pos + i] &= (byte) ~(mask << 2);
            }
        }

        public void SetBackBufferPixel(int column, int row, byte red, byte green, byte blue)
        {
            red = s_gamma[red];
            green = s_gamma[green];
            blue = s_gamma[blue];

            int pos = 8 * column + 8 * (row % (_rows >> 1)) * _width;
            byte mask = (byte) (row >= (_rows >> 1) ? 0x08 : 0x01);

            for (int i = 0; i < 8; i++)
            {
                int bit = 1 << i;

                if ((red & bit) != 0)
                    _colorsBackBuffer[pos + i] |= mask;
                else
                    _colorsBackBuffer[pos + i] &= (byte) (~mask);

                if ((green & bit) != 0)
                    _colorsBackBuffer[pos + i] |= (byte) (mask << 1);
                else
                    _colorsBackBuffer[pos + i] &= (byte) ~(mask << 1);

                if ((blue & bit) != 0)
                    _colorsBackBuffer[pos + i] |= (byte) (mask << 2);
                else
                    _colorsBackBuffer[pos + i] &= (byte) ~(mask << 2);
            }
        }

        private void SwapBuffersInternal()
        {
            var temp = _colorsBackBuffer;
            _colorsBackBuffer = _colorsBuffer;
            _colorsBuffer = temp;
        }

        public void SwapBuffers()
        {
            _swapRequested = true;

            while (_swapRequested );
        }

        public void Dispose()
        {
            if (_controller != null)
            {
                StopRendering();

                while (!_safeToDispose)
                {
                    Thread.SpinWait(1);
                }

                _controller.Dispose();
                _controller = null;
            }
        }

        public void StartRendering()
        {
            if (Interlocked.CompareExchange(ref _isRendering, 1, 0) != 0)
            {
                return; // we already rendering
            }

            Task.Factory.StartNew(
                Render,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
        public void StopRendering() => Interlocked.CompareExchange(ref _isRendering, 0, 1);

        public unsafe void DrawBitmap(int x, int y, Bitmap bitmap)
        {
            if (y >= Height || x >= Width || x + bitmap.Width <= 0 || y + bitmap.Height <= 0)
            {
                return;
            }

            Rectangle fullImageRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            Rectangle partialBitmap = new Rectangle(x, y, bitmap.Width, bitmap.Height);
            partialBitmap.Intersect(new Rectangle(0, 0, Width, Height));

            BitmapData bitmapData = bitmap.LockBits(fullImageRectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int pos = 3 * ((y < 0 ? Math.Abs(y) * bitmap.Width : 0) + (x < 0 ? Math.Abs(x) : 0));
            int stride = (bitmapData.Stride - 3 * bitmap.Width) + 3 * (bitmap.Width - partialBitmap.Width);

            Span<byte> span = new Span<byte>((void*) bitmapData.Scan0, fullImageRectangle.Width * fullImageRectangle.Height * 3);

            for (int j = 0; j < partialBitmap.Height; j++)
            {
                for (int i = 0; i < partialBitmap.Width; i++)
                {
                    SetPixel(partialBitmap.X + i, partialBitmap.Y + j, span[pos + 2], span[pos + 1], span[pos]);
                    pos += 3;
                }

                pos += stride;
            }

            bitmap.UnlockBits(bitmapData);
        }

        static int counter = 0;
        public unsafe void DrawBitmap(int x, int y, Bitmap bitmap, byte red, byte green, byte blue, byte replRed, byte replGreen, byte replBlue)
        {
            if (y >= Height || x >= Width || x + bitmap.Width <= 0 || y + bitmap.Height <= 0)
            {
                return;
            }

            int bitmapX = x < 0 ? -x : 0;
            int bitmapY = y < 0 ? -y : 0;
            int bitmapWidth = Math.Min(bitmap.Width - bitmapX, x < 0 ? Width : Width - x);
            int bitmapHeight = Math.Min(bitmap.Height - bitmapY, y < 0 ? Height : Height - y);
            int coorX = Math.Max(0, x);
            int coorY = Math.Max(0, y);

            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int pos = 3 * (bitmapY * bitmap.Width + bitmapX);
            int stride = (bitmapData.Stride - 3 * bitmap.Width) + 3 * (bitmap.Width - bitmapWidth);

            Span<byte> span = new Span<byte>((void*) bitmapData.Scan0, bitmapData.Stride * bitmap.Height);

            if (counter < 18)
            {
                counter++;
            }

            for (int j = 0; j < bitmapHeight; j++)
            {
                for (int i = 0; i < bitmapWidth; i++)
                {
                    if (red == span[pos + 2] && green == span[pos + 1] && blue == span[pos])
                    {
                        SetPixel(coorX + i, coorY + j, replRed, replGreen, replBlue);
                    }
                    else
                    {
                        SetPixel(coorX + i, coorY + j, span[pos + 2], span[pos + 1], span[pos]);
                    }
                    pos += 3;
                }

                pos += stride;
            }

            bitmap.UnlockBits(bitmapData);
        }

        public void DrawText(int x, int y, string text, BdfFont font, byte textR, byte textG, byte textB, byte bkR, byte bkG, byte bkB)
        {
            int charWidth = font.Width;
            int totalTextWith = charWidth * text.Length;

            if (y < 0 || y >= Height || x >= Width || x + totalTextWith <= 0)
            {
                return;
            }

            int index = 0;
            while (index < text.Length)
            {
                if (x + charWidth < 0)
                {
                    x += charWidth;
                    index++;
                    continue;
                }

                DrawChar(x, y, text[index], font, textR, textG, textB, bkR, bkG, bkB);

                x += charWidth;
                index++;
            }
        }

        public void DrawCircle(int xCenter, int yCenter, int radius, byte red, byte green, byte blue)
        {
            for (double angle = 0.0; angle < 6.2832; angle += 1.0 / radius)
            {
                SetPixel((int) Math.Round(xCenter + radius * Math.Cos(angle)), (int) Math.Round(yCenter + radius * Math.Sin(angle)), red, green, blue);
            }

            //
            // 1
            //

            // int xMost = (int) Math.Round(Math.Sqrt(2) * radius / 2);
            // for (int x = 0; x <= xMost; x++)
            // {
            //     int y = (int) Math.Round(Math.Sqrt(radius * radius - x * x));
            //     SetPixel(x + xCenter, y + yCenter, red, green, blue);
            //     SetPixel(xCenter - x, y + yCenter, red, green, blue);
            //     SetPixel(x + xCenter, yCenter - y, red, green, blue);
            //     SetPixel(xCenter - x, yCenter - y, red, green, blue);
            //     SetPixel(y + xCenter, x + yCenter, red, green, blue);
            //     SetPixel(y + xCenter, yCenter - x, red, green, blue);
            //     SetPixel(xCenter - y, yCenter + x, red, green, blue);
            //     SetPixel(xCenter - y, yCenter - x, red, green, blue);
            // }

            //
            // 2
            //

            // int error = 0;
            // int x = 0;
            // int y = radius;

            // while (x <= y)
            // {
            //     SetPixel(x + xCenter, y + yCenter, red, green, blue);
            //     SetPixel(xCenter - x, y + yCenter, red, green, blue);
            //     SetPixel(x + xCenter, yCenter - y, red, green, blue);
            //     SetPixel(xCenter - x, yCenter - y, red, green, blue);
            //     SetPixel(y + yCenter, x + xCenter, red, green, blue);
            //     SetPixel(y + yCenter, xCenter - x, red, green, blue);
            //     SetPixel(yCenter - y, xCenter + x, red, green, blue);
            //     SetPixel(yCenter - y, xCenter - x, red, green, blue);
            //     if ((error += 1 + 2 * x++) >= y)
            //     {
            //         error += 1 + 2 * y--;
            //     }
            // }
        }

        private void DrawChar(int x, int y, char c, BdfFont font, byte textR, byte textG, byte textB, byte bkR, byte bkG, byte bkB)
        {
            int hightToDraw = Math.Min(Height - y, font.Height);
            int firstColumnToDraw = x < 0 ? Math.Abs(x) : 0;
            int lastColumnToDraw  = x + font.Width > Width ? Width - x : font.Width;

            font.GetCharData(c, out ReadOnlySpan<ushort> charData);

            int b = 8 * (sizeof(ushort) - (int) Math.Ceiling(((double)font.Width) / 8)) + firstColumnToDraw;

            for (int j = firstColumnToDraw; j < lastColumnToDraw; j++)
            {
                for (int i = 0; i < hightToDraw; i++)
                {
                    int value = charData[i] << (b + j - firstColumnToDraw);

                    if ((value & 0x8000) != 0)
                        SetPixel(x + j, y + i, textR, textG, textB);
                    else
                        SetPixel(x + j, y + i, bkR, bkG, bkB);
                }
            }
        }

        private int _voluntarilySleepCounter = 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Sleep(long duration)
        {
            if (_voluntarilySleepCounter++ >= 4_000_000)
            {
                _voluntarilySleepCounter = 0;
                // ThreadHelper.sched_yield();
            }

            long startTicks = Stopwatch.GetTimestamp();

            // if (duration > _duration * 4)
            // {
            //     Thread.SpinWait(1);
            // }

            while (Stopwatch.GetTimestamp() - startTicks < duration) { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitForExpiration()
        {
            while (Stopwatch.GetTimestamp() < _expirationTicks) { } // busy wait
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetNextExpiration(long periodFromNow)
        {
            _expirationTicks = Stopwatch.GetTimestamp() + periodFromNow;
        }

        private void Render()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            System.Interop.ThreadHelper.SetCurrentThreadHighPriority();
            // System.Interop.ThreadHelper.SetCurrentThreadNormalHighPriority();

            _safeToDispose = false;

            // Line 0
            _gpio.WriteClear(_gpio.AMask | _gpio.BMask | _gpio.CMask | _gpio.DMask);

            long startTime = Stopwatch.GetTimestamp();
            bool showFrameTime = false;
            int row = 0;

            while (_isRendering == 1)
            {
                RenderRow(row);

                if (row == 15)
                {
                    if (_showFrameTime)
                    {
                        if (showFrameTime)
                        {
                            long totalTime = Stopwatch.GetTimestamp() - startTime;
                            Console.WriteLine($"Frame Time: {((double) totalTime / Stopwatch.Frequency) * 1E6} \u00B5s ... {totalTime} {totalTime / (_rows >> 1)}");
                            Console.WriteLine($"Duration : { PWMDuration }");
                            showFrameTime = false;
                            _showFrameTime = false;
                        }
                        else
                        {
                            showFrameTime = true;
                            startTime = Stopwatch.GetTimestamp();
                        }
                    }
                }

                row = (row + 1) % (_rows >> 1);

                if (row == 0 && _swapRequested)
                {
                    SwapBuffersInternal();
                    _swapRequested = false;
                }

                _gpio.WriteSet(_gpio.OEMask | _rowSetMasks[row]); // Disable the output and push the next row
                _gpio.WriteClear((~_rowSetMasks[row]) & _gpio.ABCDEMask);
            }

            _safeToDispose = true;
        }
        private void RenderRow(int row)
        {
            int pos = (row % (_rows >> 1)) * _width * 8;
            for (int bit = 0; bit < 8; bit++)
            {
                for (int column = 0; column < _width; column++)
                {
                    byte colorsBits = _colorsBuffer[pos + (column << 3) + bit];

                    ulong mask = colorsMake[colorsBits & 0x07] | colorsMake[8 + ((colorsBits >> 3) & 0x07)];
                    _gpio.WriteSet(mask);
                    _gpio.WriteClear((~mask) & _gpio.AllColorsMask);

                    _gpio.WriteSet(_gpio.ClockMask);
                    _gpio.WriteClear(_gpio.ClockMask);
                }

                _gpio.WriteSet(_gpio.OEMask | _gpio.LatchMask);

                _gpio.WriteClear(_gpio.LatchMask);
                _gpio.WriteClear(_gpio.OEMask);

                Sleep(_duration * (1 << bit));
            }
        }

        private void OpenAndWriteToPin(int pinNumber, PinValue value)
        {
            _controller.OpenPin(pinNumber, PinMode.Output);
            _controller.Write(pinNumber, value);
        }

        private static readonly byte [] s_gamma = new byte []
        {
        //           0    1     2    3    4    5    6    7    8    9    A    B    C    D    E    F
        /* 00 */      0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,
        /* 10 */      0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   0,   1,   1,   1,   1,
        /* 20 */      1,   1,   1,   1,   1,   2,   2,   2,   2,   2,   2,   2,   3,   3,   3,   3,
        /* 30 */      3,   4,   4,   4,   4,   5,   5,   5,   5,   6,   6,   6,   6,   7,   7,   7,
        /* 40 */      8,   8,   8,   9,   9,   9,  10,  10,  10,  11,  11,  11,  12,  12,  13,  13,
        /* 50 */     14,  14,  14,  15,  15,  16,  16,  17,  17,  18,  18,  19,  19,  20,  21,  21,
        /* 60 */     22,  22,  23,  23,  24,  25,  25,  26,  27,  27,  28,  29,  29,  30,  31,  31,
        /* 70 */     32,  33,  34,  34,  35,  36,  37,  37,  38,  39,  40,  41,  42,  42,  43,  44,
        /* 80 */     45,  46,  47,  48,  49,  50,  51,  52,  52,  53,  54,  55,  56,  57,  59,  60,
        /* 90 */     61,  62,  63,  64,  65,  66,  67,  68,  69,  71,  72,  73,  74,  75,  77,  78,
        /* A0 */     79,  80,  82,  83,  84,  85,  87,  88,  89,  91,  92,  93,  95,  96,  98,  99,
        /* B0 */    100, 102, 103, 105, 106, 108, 109, 111, 112, 114, 115, 117, 119, 120, 122, 123,
        /* C0 */    125, 127, 128, 130, 132, 133, 135, 137, 138, 140, 142, 144, 145, 147, 149, 151,
        /* D0 */    153, 155, 156, 158, 160, 162, 164, 166, 168, 170, 172, 174, 176, 178, 180, 182,
        /* E0 */    184, 186, 188, 190, 192, 194, 197, 199, 201, 203, 205, 207, 210, 212, 214, 216,
        /* F0 */    219, 221, 223, 226, 228, 230, 233, 235, 237, 240, 242, 245, 247, 250, 252, 255
        };

    }
}