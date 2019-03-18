// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.LEDMatrix;
using Iot.Device.Graphics;

namespace dotnettest
{
    public class Program
    {
        private static void DrawRect(RGBMatrix matrix, int x, int y, int width, int height, byte r, byte g, byte b)
        {
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                {
                    matrix.SetPixel(i + x, j + y, r, g, b);
                }
        }

        private static void DrawRect(RGBMatrix matrix, int x, int y, int length, byte r, byte g, byte b)
        {
            for (int j = 0; j < length; j++)
                for (int i = 0; i < length; i++)
                {
                    matrix.SetPixel(i + x, j + y, r, g, b);
                }
        }

        internal static void Sleep(int milliseconds)
        {
            long now = Stopwatch.GetTimestamp();
            long ticks = (long) (Stopwatch.Frequency * ((double) milliseconds / 1000));

            while (ticks > Stopwatch.GetTimestamp() - now) {}
        }

        static double dd = 1.9;//1.9;//3.3;
        static double ee = 0.95;//3.3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Color Pixel(Vector2 uv, float time)
        {
            //time = 0.5f;
            return ToSRGB(
                0.5 + 0.5 * Math.Cos(time + uv.X),
                0.5 + 0.5 * Math.Cos(time + uv.Y + 2.0),
                0.5 + 0.5 * Math.Cos(time + uv.X + 4.0)
            );
        }

        static Color ToSRGB(double x, double y, double z)
        {
            return Color.FromArgb(
                ColR(x),
                ColG(y),
                ColB(z));
        }

        public static byte ColR(double x)
        {
            return Col(x, 1.9, 0.95);
        }

        public static byte ColG(double x)
        {
            return Col(x, 1.9, 0.95);
        }

        public static byte ColB(double x)
        {
            return Col(x, 1.9, 0.95);
        }

        static byte Col(double x, double d, double e)
        {
            x *= e;
            x = Math.Pow(x, d);
            x = Math.Clamp(x, 0.0f, 1.0f);
            return (byte)(x * 255);
        }

        private static bool play = false;
        private static int scenario = 2;

        static unsafe void Demo1(RGBMatrix matrix)
        {
            play = true;

            try
            {
                BdfFont font = BdfFont.Load(@"fonts/10x20.bdf");
                BdfFont font1 = BdfFont.Load(@"fonts/8x13B.bdf");
                matrix.Fill(0, 0, 0);
                Program.Sleep(100);

                int x = matrix.Width - 1;
                string text = "Hello .NET IoT";
                int fullTextWidth = text.Length * font.Width;

                while (play)
                {
                    matrix.DrawText(x, 0, text, font, 0, 0, 255, 0, 0, 0);
                    x--;
                    if (x == -fullTextWidth)
                        x = matrix.Width - 1;

                    string d = DateTime.Now.ToString("hh:mm:ss");
                    matrix.DrawText(0, font.Height + 1, d, font1, 128, 128, 0, 0, 0, 0);

                    Thread.Sleep(25);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Demo2(RGBMatrix matrix)
        {
            int length = matrix.Width / 4;
            int height =  matrix.Height / 4;

            play = true;
            while (play)
            {
                DrawRect(matrix, 0,          0, length, 255, 0,   0);
                DrawRect(matrix, length,     0, length, 0,   255, 0);
                DrawRect(matrix, 2 * length, 0, length, 0,   0,   255);
                DrawRect(matrix, 3 * length, 0, length, 255, 255, 0);

                DrawRect(matrix, 0,  height, length, 255, 0,   255);
                DrawRect(matrix, length,  height, length, 255, 255, 255);
                DrawRect(matrix, 2 * length, height, length, 0,   130, 0);
                DrawRect(matrix, 3 * length, height, length, 130, 0,   0);

                DrawRect(matrix, 0,  2 * height, length, 0,   0,   128);
                DrawRect(matrix, length,  2 * height, length, 192, 192, 192);
                DrawRect(matrix, 2 * length, 2 * height, length, 128, 128, 0);
                DrawRect(matrix, 3 * length, 2 * height, length, 128, 128, 128);

                DrawRect(matrix, 0,  3 * height, length, 255, 0, 0);
                DrawRect(matrix, length,  3 * height, length,  0, 255, 0);
                DrawRect(matrix, 2 * length, 3 * height, length,  0,   0,  255);
                DrawRect(matrix, 3 * length, 3 * height, length,  255,  255,  0);

                // DrawRect(matrix, 0,  3 * height, length, 120, 200, 200);
                // DrawRect(matrix, length,  3 * height, length,  66, 244, 212);
                // DrawRect(matrix, 2 * length, 3 * height, length,  38,   9,  53);
                // DrawRect(matrix, 3 * length, 3 * height, length,  30,  40,  50);

                Program.Sleep(100);
            }
        }


        private static void ScrollText(
                                RGBMatrix matrix,
                                string text,
                                BdfFont font,
                                int startPos,
                                int endPos,
                                byte red, byte green, byte blue, byte bkRed, byte bkGreen, byte bkBlue)
        {
            if (startPos < endPos)
            {
                return;
            }

            text = text + " "; // to clear the text when scrolling
            int fullTextWidth = text.Length * font.Width;

            while (startPos >= endPos)
            {
                matrix.DrawText(startPos, 0, text, font, red, green, blue, bkRed, bkGreen, bkBlue);
                startPos--;
                Thread.Sleep(20);
            }
        }

        private struct CityData
        {
            public CityData(string city, string countryCode, string zoneId)
            {
                City = city;
                CountryCode = countryCode;
                ZoneId = zoneId;

            }

            public string City { get; }
            public string CountryCode { get; }
            public string ZoneId {get; }
        }

        private static readonly CityData [] s_citiesData = new CityData []
        {
            new CityData("New York", "US", "America/New_York"),
            new CityData("Redmond", "US", "America/Los_Angeles"),
            new CityData("Toronto", "CA", "America/Toronto"),
            new CityData("Mexico", "MX", "America/Mexico_City"),
            new CityData("Madrid", "ES", "Europe/Madrid"),
            new CityData("London", "UK", "Europe/London"),
            new CityData("Paris", "FR", "Europe/Paris"),
            new CityData("Rome", "IT", "Europe/Rome"),
            new CityData("Moscow", "RU", "Europe/Moscow"),
            new CityData("Casablanca", "MA", "Africa/Casablanca"),
            new CityData("Cairo", "EG", "Africa/Cairo"),
            new CityData("Riyadh", "SA", "Asia/Riyadh")
        } ;

        private static readonly string s_weatherKey = https://openweathermap.org;

        static void Demo3(RGBMatrix matrix)
        {
            play = true;

            byte blue = 0x10;
            matrix.Fill(0, 0, blue);

            TimeZoneInfo [] zones = new TimeZoneInfo [s_citiesData.Length] ;
            string [] weatherUrls = new string [s_citiesData.Length];
            for (int i = 0; i < s_citiesData.Length; i++)
            {
                weatherUrls[i] = String.Format("http://api.openweathermap.org/data/2.5/weather?q={0},{1}&mode=xml&units=imperial&APPID={2}", s_citiesData[i].City, s_citiesData[i].CountryCode, s_weatherKey);
                zones[i] = TimeZoneInfo.FindSystemTimeZoneById(s_citiesData[i].ZoneId);
            }

            using (WebClient client = new WebClient())
            {
                BdfFont font  = BdfFont.Load(@"fonts/6x12.bdf");
                BdfFont font1 = BdfFont.Load(@"fonts/5x7.bdf");
                BdfFont font2 = BdfFont.Load(@"fonts/4x6.bdf");

                int cityIndex = 0;

                try
                {
                while (play)
                {
                    string xml = client.DownloadString(weatherUrls[cityIndex]);
                    XDocument doc = XDocument.Parse(xml);
                    XElement element = doc.Root.Element("temperature");
                    string temperature = ((int) Math.Round(Double.Parse(element.Attribute("value").Value))).ToString(CultureInfo.InvariantCulture);

                    element = doc.Root.Element("weather");
                    string description = element.Attribute("value").Value + "                                           ";

                    element = doc.Root.Element("humidity");
                    string humidity = element.Attribute("value").Value + element.Attribute("unit").Value + "             ";
                    element = doc.Root.Element("city").Element("sun");
                    string sunRise = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(element.Attribute("rise").Value, CultureInfo.InvariantCulture), zones[cityIndex]).ToString("hh:mm tt");
                    string sunSet = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(element.Attribute("set").Value, CultureInfo.InvariantCulture), zones[cityIndex]).ToString("hh:mm tt");

                    int pos = Math.Max(0, (matrix.Width - (s_citiesData[cityIndex].City.Length * font.Width))) / 2;
                    ScrollText(
                        matrix,
                        s_citiesData[cityIndex].City,
                        font,
                        matrix.Width - 1,
                        pos,
                        128, 128, 128, 0, 0, blue);

                    int y = font.Height;
                    matrix.DrawText((matrix.Width - (temperature.Length + 1) * font1.Width) / 2, y, temperature + "\u00B0", font1, 255, 255, 0, 0, 0, blue);

                    y += font1.Height + 2;
                    matrix.DrawText(2, y, description, font2, 128, 128, 128, 0, 0, blue);

                    y += font2.Height + 2;
                    matrix.DrawText(2, y, "humidity: ", font2, 128, 128, 128, 0, 0, blue);
                    matrix.DrawText(font2.Width * "humidity: ".Length + 2, y, humidity, font2, 255, 255, 0, 0, 0, blue);

                    y += font2.Height;
                    string localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zones[cityIndex]).ToString("hh:mm tt");
                    matrix.DrawText((matrix.Width - localTime.Length * font.Width) / 2, y, localTime, font, 255, 255, 0, 0, 0, blue);

                    y += font.Height + 2;
                    matrix.DrawText(2, y, "Sun Rise: ", font2, 128, 128, 128, 0, 0, blue);
                    matrix.DrawText(2 + "Sun Rise: ".Length * font2.Width, y, sunRise, font2, 255, 255, 0, 0, 0, blue);

                    y += font2.Height + 2;
                    matrix.DrawText(2, y, "Sun Set:  ", font2, 128, 128, 128, 0, 0, blue);
                    matrix.DrawText(2 + "Sun Set:  ".Length * font2.Width, y, sunSet, font2, 255, 255, 0, 0, 0, blue);

                    Program.Sleep(4000);

                    ScrollText(
                        matrix,
                        s_citiesData[cityIndex].City,
                        font,
                        pos,
                        - (pos + s_citiesData[cityIndex].City.Length * font.Width),
                        128, 128, 128, 0, 0, blue);

                    y = font.Height;
                    matrix.DrawText((matrix.Width - (temperature.Length + 1) * font1.Width) / 2, y, temperature + "\u00B0", font1, 0, 0, blue, 0, 0, blue);

                    y += font1.Height + 2;
                    matrix.DrawText(2, y, description, font2, 0, 0, blue, 0, 0, blue);

                    y += font2.Height + 2;
                    matrix.DrawText(2, y, "humidity: ", font2, 0, 0, blue, 0, 0, blue);
                    matrix.DrawText(font2.Width * "humidity: ".Length + 2, y, humidity, font2, 0, 0, blue, 0, 0, blue);

                    y += font2.Height;
                    matrix.DrawText((matrix.Width - localTime.Length * font.Width) / 2, y, localTime, font, 0, 0, blue, 0, 0, blue);

                    y += font.Height + 2;
                    matrix.DrawText(2, y, "Sun Rise: ", font2, 0, 0, blue, 0, 0, blue);
                    matrix.DrawText(2 + "Sun Rise: ".Length * font2.Width, y, sunRise, font2, 0, 0, blue, 0, 0, blue);

                    y += font2.Height + 2;
                    matrix.DrawText(2, y, "Sun Set:  ", font2, 0, 0, blue, 0, 0, blue);
                    matrix.DrawText(2 + "Sun Set:  ".Length * font2.Width, y, sunSet, font2, 0, 0, blue, 0, 0, blue);

                    cityIndex = (cityIndex + 1) % s_citiesData.Length;
                }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
        static unsafe void Demo4(RGBMatrix matrix)
        {
            play = true;

            byte blue = 0x15;
            matrix.Fill(0, 0, blue);

            using (WebClient client = new WebClient())
            {
                int lastMinute          = -1;
                string temperature      = "";

                BdfFont font  = BdfFont.Load(@"fonts/6x12.bdf");
                BdfFont font1 = BdfFont.Load(@"fonts/5x7.bdf");

                Bitmap weatherIcon = null;
                string lastIcon = null;
                string description = "";

                while (play)
                {
                    DateTime time = DateTime.Now;
                    if (Math.Abs(time.Minute - lastMinute) > 4)
                    {
                        lastMinute = time.Minute;
                        string xml = client.DownloadString("http://api.openweathermap.org/data/2.5/weather?q=Redmond,US&mode=xml&units=imperial&APPID=" + s_weatherKey);

                        XDocument doc = XDocument.Parse(xml);
                        XElement element = doc.Root.Element("temperature");
                        temperature = ((int) Math.Round(Double.Parse(element.Attribute("value").Value))).ToString(CultureInfo.InvariantCulture);

                        element = doc.Root.Element("weather");
                        string icon = element.Attribute("icon").Value;
                        description = element.Attribute("value").Value;

                        if (lastIcon != icon)
                        {
                            weatherIcon = new Bitmap("bitmaps/" + icon + ".bmp");
                        }

                        matrix.DrawBitmap(20, 2, weatherIcon, 255, 255, 255, 0, 0, blue);
                        matrix.DrawText(Math.Max(0, matrix.Width - description.Length * font1.Width), 42, description, font1, 128, 128, 128, 0, 0, blue);
                        matrix.DrawText(2, 2 + font.Height, temperature + "\u00B0", font, 128, 128, 128, 0, 0, blue);
                    }

                    matrix.DrawText(2, 2, time.ToString("ddd"), font, 128, 128, 128, 0, 0, blue);
                    matrix.DrawText(2, matrix.Height - font.Height, time.ToString("hh:mm:sstt"), font, 128, 128, 128, 0, 0, blue);

                    Program.Sleep(200);
                }
            }
        }

        static void Demo5(RGBMatrix matrix)
        {
            play = true;

            try
            {
                var sw = Stopwatch.StartNew();
                while (play)
                {
                    float time = sw.ElapsedMilliseconds / 1000f;
                    for (int ix = 0; ix < matrix.Width; ix++)
                    {
                        for (int iy = 0; iy < matrix.Height; iy++)
                        {
                            Vector2 uv = new Vector2(ix / (float)(matrix.Width - 1), iy / (float)(matrix.Height - 1));
                            Vector3 cv = HSV(uv, time);
                            Color c = ColorFromVec3(cv);//ToSRGB(cv.X, cv.Y, cv.Z);//ColorFromVec3(cv);
                            matrix.SetPixel(ix, iy, c.R, c.G, c.B);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static unsafe void Demo6(RGBMatrix matrix)
        {
            play = true;

            try
            {
                matrix.Fill(0, 0, 0);

                Bitmap [] bitmaps = new Bitmap []
                {
                    new Bitmap(@"bitmaps/dotnet-bot-branded-32x32.bmp"),
                    new Bitmap(@"bitmaps/i-love-dotnet.bmp")
                };

                int x = matrix.Width - 1;
                int bitmapIndex = 0;
                while (play)
                {
                    matrix.DrawBitmap(x, 0, bitmaps[bitmapIndex]);

                    if (x + bitmaps[bitmapIndex].Width < matrix.Width)
                    {
                        DrawRect(matrix, x + bitmaps[bitmapIndex].Width, 0, matrix.Width - x - bitmaps[bitmapIndex].Width, matrix.Height, 0, 0, 0);
                    }

                    x--;

                    if (x == -bitmaps[bitmapIndex].Width)
                    {
                        bitmapIndex = (bitmapIndex + 1) % bitmaps.Length;
                        x = matrix.Width - 1;
                    }

                    Program.Sleep(25);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static unsafe void Demo7(RGBMatrix matrix)
        {
            play = true;

            try
            {
                matrix.Fill(0, 0, 0);

                while (play)
                {
                    matrix.SetPixel(matrix.Width / 2, matrix.Height /2, 255, 0, 0);
                    matrix.DrawCircle(matrix.Width / 2, matrix.Height /2, 14, 255, 0, 0);
                    matrix.DrawCircle(matrix.Width / 2, matrix.Height /2, 9, 0, 255, 0);
                    matrix.DrawCircle(matrix.Width / 2, matrix.Height /2, 6, 0, 0, 255);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static Vector3 clamp(Vector3 c, float a, float b)
        {
            return new Vector3(Math.Clamp(c.X, a, b), Math.Clamp(c.Y, a, b), Math.Clamp(c.Z, a, b));
        }
        static float mod(float a, float b)
        {
            float ret = a % b;
            if (ret < 0)
            {
                ret += b;
            }

            return ret;
        }

        static Vector3 mod(Vector3 a, float b)
        {
            return new Vector3(mod(a.X, b), mod(a.Y, b), mod(a.Z, b));
        }

        static Vector3 Add(Vector3 v, float s)
        {
            return new Vector3(v.X + s, v.Y + s, v.Z + s);
        }

        static Vector3 abs(Vector3 vector)
        {
            return new Vector3(Math.Abs(vector.X), Math.Abs(vector.Y), Math.Abs(vector.Z));
        }

        static Vector3 mix(Vector3 a, Vector3 b, float f)
        {
            return a * (1 - f) + b * f;
        }

        static Vector3 hsv2rgb_smooth(Vector3 c)
        {
            float c1 = c.X + 6.0f;

            Vector3 v1 = Add(new Vector3(0.0f, 4.0f, 2.0f), c.X * 6.0f);
            Vector3 rgb = clamp(Add(abs(Add(mod(v1, 6.0f), -3.0f)), -1.0f), 0.0f, 1.0f);

            rgb = rgb*rgb*(Add(-2.0f * rgb, 3.0f)); // cubic smoothing

            return c.Z * mix(new Vector3(1.0f, 1.0f, 1.0f), rgb, c.Y);
        }

        static float smoothstep(float edge0, float edge1, float x)
        {
            // Scale, bias and saturate x to 0..1 range
            x = Math.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            // Evaluate polynomial
            return x * x * (3 - 2 * x);
        }

        static Vector3 Star(Vector2 uv, float time)
        {
            Vector2 p = uv - new Vector2(0.5f, 0.5f);
            float a = (float)(Math.Atan2(p.Y, p.X) / 2f / Math.PI);

            a = mod(a + time / 10.0f, 1.0f);

            float r = p.Length();
            float n = 5.0f;

            float s1 = 0.2f;
            float s2 = 0.5f;
            float srange = s2 - s1;
            float x = Math.Abs(mod(a, 1.0f / n) * n - 0.5f) * srange / 0.5f + s1;

            float blur = 4.0f + 3.0f * (float)Math.Sin(time * 2f * Math.PI / 5f);
            float c = smoothstep(0.0f, 1.0f, (x - r) * blur);//x <= r ? 0.0 : 1.0;
            //// return hsv2rgb_smooth(new Vector3(mod(time / 11.0f, 1.0f), 1.0f, c));

            float ha = a;
            //ha = ha + time / 10f;
            float h = mod(ha, 1.0f);
            //h = (float)Math.Floor(h * colFac) / colFac;
            //h = mod(h + mod(time / 4.0f, 0.5f), 1.0f);

            float v = c;
            //v = (float)Math.Floor(v * colFac2) / colFac2;

            float s = 1.0f;
            //s = (float)Math.Pow(r * 5.0f, 0.5f);
            return hsv2rgb_smooth(new Vector3(h, s, v));
        }


        static Vector3 HSV(Vector2 uv, float time)
        {
            Vector2 p = uv - new Vector2(0.5f, 0.5f);
            float a = (float)(Math.Atan2(p.Y, p.X) / 2f / Math.PI);

            a = mod(a + time / 10.0f, 1.0f);

            float r = p.Length();

            float ha = a;
            //ha = ha + time / 10f;
            float h = mod(ha, 1.0f);
            //v = (float)Math.Floor(v * colFac2) / colFac2;

            float s = 1.0f;
            float v = r * 2.0f;

            //s = (float)Math.Pow(r * 5.0f, 0.5f);
            return hsv2rgb_smooth(new Vector3(a, s, v));
        }

        static Vector3 Circle(Vector2 uv)
        {
            Vector2 p = uv - new Vector2(0.5f, 0.5f);
            float c = p.Length() - 0.5f > 0.0f ? 0.0f : 1.0f;
            return new Vector3(c, 0.0f, 0.0f);
        }

        static byte Col(float x)
        {
            x *= 255f;
            x = Math.Clamp(x, 0f, 255f);
            return (byte)x;
        }

        static Color ColorFromVec3(Vector3 v)
        {
            return Color.FromArgb(Col(v.X), Col(v.Y), Col(v.Z));
        }

        static void Demo8(RGBMatrix matrix)
        {
            play = true;

            try
            {
                var sw = Stopwatch.StartNew();
                while (play)
                {
                    float time = sw.ElapsedMilliseconds / 1000f;
                    for (int ix = 0; ix < matrix.Width; ix++)
                    {
                        for (int iy = 0; iy < matrix.Height; iy++)
                        {
                            Vector2 uv = new Vector2(ix / (float)(matrix.Width - 1), iy / (float)(matrix.Height - 1));
                            Vector3 cv = Star(uv, time);
                            Color c = ToSRGB(cv.X, cv.Y, cv.Z);//ColorFromVec3(cv);
                            matrix.SetPixel(ix, iy, c.R, c.G, c.B);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine($"Hello Matrix World!");

            PinMapping mapping = PinMapping.MatrixBonnetMapping64;
            // PinMapping mapping = PinMapping.MatrixBonnetMapping32;
            // RGBMatrix matrix = new RGBMatrix(mapping, 32, 32);
            RGBMatrix matrix = new RGBMatrix(mapping, 64, 64);

            Task.Run(() =>
            {
                matrix.StartRendering();
                // System.Interop.ThreadHelper.SetCurrentThreadHighPriority();

                while (scenario != 0)
                {
                    matrix.Brightness = 255;
                    switch (scenario)
                    {
                        case 1: Demo1(matrix); break;
                        case 2: Demo2(matrix); break;
                        case 3: Demo3(matrix); break;
                        case 4: Demo4(matrix); break;
                        case 5: Demo5(matrix); break;
                        case 6: Demo6(matrix); break;
                        case 7: Demo7(matrix); break;
                        case 8: Demo8(matrix); break;
                        default:
                            scenario = 3;
                            break;
                    }
                }
            });

            ConsoleKeyInfo cki;
            Console.WriteLine($"Press q to exit.");
            System.Interop.ThreadHelper.SetCurrentThreadHighPriority();

            do
            {
                cki = Console.ReadKey();

                if (cki.KeyChar == '+')
                {
                    matrix.PWMDuration = matrix.PWMDuration + 100;
                    Console.WriteLine($"     ({matrix.PWMDuration})");
                }

                if (cki.KeyChar == '-')
                {
                    matrix.PWMDuration = matrix.PWMDuration - 100;
                    Console.WriteLine($"     ({matrix.PWMDuration})");
                }

                if (cki.KeyChar == 'a')
                {
                    dd += 0.1;
                    Console.WriteLine($"     ({dd})");
                }

                if (cki.KeyChar == 'z')
                {
                    dd -= 0.1;
                    Console.WriteLine($"     ({dd})");
                }

                if (cki.KeyChar == 's')
                {
                    ee += 0.05;
                    Console.WriteLine($"     ({ee})");
                }

                if (cki.KeyChar == 'x')
                {
                    ee -= 0.05;
                    Console.WriteLine($"     ({ee})");
                }

                if (cki.KeyChar == 'f')
                {
                    matrix.ShowFrameTime = true;
                }

                if (cki.KeyChar >= '1' && cki.KeyChar <= '9')
                {
                    play = false;
                    scenario = cki.KeyChar - '0';
                    Thread.Sleep(1000);
                }
            } while (cki.KeyChar != 'q');

            play = false;
            scenario = 0;
            Thread.Sleep(1000);
            matrix.Dispose();
        }
    }
}
