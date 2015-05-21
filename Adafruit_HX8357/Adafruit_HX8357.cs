/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Adafruit_HX8357
{
    public class Tft
    {
        // Display Properties
        private static ushort HX8357_TFTWIDTH = 320;
        private static ushort HX8357_TFTHEIGHT = 480;
        private static ushort _width = HX8357_TFTWIDTH;
        private static ushort _height = HX8357_TFTWIDTH;
        private static uint ROTATION = 0;

        private string SpiControllerName; // For Raspberry Pi 2, use SPI0
        private int SpiChipSelectLine; // Line 0 maps to physical pin number 24 on the Rpi2
        private int DataCommandPin;  // We use GPIO 22 since it's conveniently near the SPI pins

        // Definitions for SPI and GPIO
        private SpiDevice _spiDisplay;
        private GpioController _ioController;
        private GpioPin _dataCommandPin;

        // Display commands.
        private static readonly byte[] HX8357_SWRESET = { 0x01 };
        private static readonly byte[] HX8357D_SETC = { 0xB9 };
        private static readonly byte[] HX8357_SETRGB = { 0xB3 };
        private static readonly byte[] HX8357D_SETCOM = { 0xB6 };
        private static readonly byte[] HX8357_SETOSC = { 0xB0 };
        private static readonly byte[] HX8357_SETPANEL = { 0xCC };
        private static readonly byte[] HX8357_SETPWR1 = { 0xB1 };
        private static readonly byte[] HX8357D_SETSTBA = { 0xC0 };
        private static readonly byte[] HX8357D_SETCYC = { 0xB4 };
        private static readonly byte[] HX8357D_SETGAMMA = { 0xE0 };
        private static readonly byte[] HX8357_COLMOD = { 0x3A };
        private static readonly byte[] HX8357_MADCTL = { 0x36 };
        private static readonly byte[] HX8357_TEON = { 0x35 };
        private static readonly byte[] HX8357_TEARLINE = { 0x44 };
        private static readonly byte[] HX8357_SLPOUT = { 0x11 };
        private static readonly byte[] HX8357_DISPON = { 0x29 }; // Turns the display on

        private static readonly byte[] HX8357_CASET = { 0x2A }; // Reset the column address pointer
        private static readonly byte[] HX8357_PASET = { 0x2B }; // Reset the page address pointer
        private static readonly byte[] HX8357_RAMWR = { 0x2C };

        private static readonly byte MADCTL_MY = 0x80;
        private static readonly byte MADCTL_MX = 0x40;
        private static readonly byte MADCTL_MV = 0x20;
        private static readonly byte MADCTL_ML = 0x10;
        private static readonly byte MADCTL_RGB = 0x00;
        private static readonly byte MADCTL_BGR = 0x08;
        private static readonly byte MADCTL_MH = 0x04;

        public Tft( int cs, int dc, string sclk )
        {
            SpiControllerName = sclk;
            SpiChipSelectLine = cs;
            DataCommandPin = dc;
        }

        public async Task Begin()
        {
            _ioController = GpioController.GetDefault(); // Get the default GPIO controller on the system

            // Initialize a pin as output for the Data/Command line on the display
            _dataCommandPin = _ioController.OpenPin(DataCommandPin);
            _dataCommandPin.Write(GpioPinValue.High);
            _dataCommandPin.SetDriveMode(GpioPinDriveMode.Output);

            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(SpiChipSelectLine)
            {
                ClockFrequency = 16000000, // Datasheet specifies maximum SPI clock frequency of ???MHz
                Mode = SpiMode.Mode0
            };

            var spiAqs = SpiDevice.GetDeviceSelector(SpiControllerName);   // Find the selector string for the SPI bus controller
            var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);  // Find the SPI bus controller device with our selector string
            _spiDisplay = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings); // Create an SpiDevice with our bus controller and SPI settings

            DisplayWriteCommand(HX8357_SWRESET);

            DisplayWriteCommand(HX8357D_SETC);
            DisplayWriteData(new byte[] { 0xFF, 0x83, 0x57 });

            await Task.Delay(300);

            DisplayWriteCommand(HX8357_SETRGB); // setRGB which also enables SDO
            DisplayWriteData(new byte[] { 0x80, 0x0, 0x06, 0x06 });

            DisplayWriteCommand(HX8357D_SETCOM);
            DisplayWriteData(new byte[] { 0x25 });// -1.52V

            DisplayWriteCommand(HX8357_SETOSC);
            DisplayWriteData(new byte[] { 0x68 }); // Normal mode 70Hz, Idle mode 55 Hz

            DisplayWriteCommand(HX8357_SETPANEL);
            DisplayWriteData(new byte[] { 0x05 }); // BGR, Gate direction swapped

            DisplayWriteCommand(HX8357_SETPWR1);
            DisplayWriteData(new byte[] { 0x00, 0x15, 0x1C, 0x1C, 0x83, 0xAA }); // BT, VSPR, VSNR, AP, FS

            DisplayWriteCommand(HX8357D_SETSTBA);
            DisplayWriteData(new byte[] { 0x50, 0x50, 0x01, 0x3C, 0x1E, 0x08 }); // OPON normal, OPON idle, STBA, STBA, STBA, GEN

            DisplayWriteCommand(HX8357D_SETCYC);
            DisplayWriteData(new byte[] { 0x02, 0x40, 0x00, 0x2A, 0x2A, 0x0D, 0x78 }); //NW 0x02, RTN, DIV, DUM, DUM, GDON, GDOFF

            DisplayWriteCommand(HX8357D_SETGAMMA);
            DisplayWriteData(new byte[] { 0x02, 0x0A, 0x11, 0x1d, 0x23, 0x35, 0x41, 0x4b, 0x4b, 0x42, 0x3A, 0x27, 0x1B, 0x08, 0x09, 0x03, 0x02, 0x0A, 0x11, 0x1d, 0x23, 0x35, 0x41, 0x4b, 0x4b, 0x42, 0x3A, 0x27, 0x1B, 0x08, 0x09, 0x03, 0x00, 0x01 });

            DisplayWriteCommand(HX8357_COLMOD);
            DisplayWriteData(new byte[] { 0x55 }); // 16 bit

            DisplayWriteCommand(HX8357_MADCTL);
            DisplayWriteData(new byte[] { 0xC0 });

            DisplayWriteCommand(HX8357_TEON);
            DisplayWriteData(new byte[] { 0x00 }); // TE off

            DisplayWriteCommand(HX8357_TEARLINE);  // tear line
            DisplayWriteData(new byte[] { 0x00, 0x02 });

            DisplayWriteCommand(HX8357_SLPOUT); // Exit Sleep
            await Task.Delay(150);

            DisplayWriteCommand(HX8357_DISPON); //display on
            await Task.Delay(50);
        }

        public void DrawPixel(short x, short y, ushort color)
        {
            if ((x < 0) || (x >= HX8357_TFTWIDTH) || (y < 0) || (y >= HX8357_TFTHEIGHT)) return;

            SetAddrWindow((ushort)x, (ushort)y, (ushort)(x + 1), (ushort)(y + 1));

            _dataCommandPin.Write(GpioPinValue.High);
            _spiDisplay.Write(new[] { (byte)(color >> 8), (byte)color });
        }

        public void DrawColor(ushort color)
        {
            var hcolor = (byte)(color >> 8);
            var lcolor = (byte)(color & 0xff);

            var data = new List<byte>();
            for (var i = 0; i < 32000; i++)
            {
                data.Add(hcolor);
                data.Add(lcolor);
            }

            DisplayWriteCommand(HX8357_RAMWR); // write to RAM
            DisplayWriteData(data.ToArray());
        }

        public void FillRect(ushort x, ushort y, ushort w, ushort h, ushort color)
        {
            if ((x >= HX8357_TFTWIDTH) || (y >= HX8357_TFTHEIGHT)) return;

            SetAddrWindow( x, y, (ushort)(x + w - 1), (ushort)(y + h - 1));

            var data = new[] { (byte)(color >> 8), (byte)(color) };

            _dataCommandPin.Write(GpioPinValue.High);
            for (y = h; y > 0; y--)
                for (x = w; x > 0; x--)
                    _spiDisplay.Write( data );
        }

        public void FillScreen(ushort color)
        {
            FillRect(0, 0, HX8357_TFTWIDTH, HX8357_TFTHEIGHT, color);
        }

        //private void SpiWrite(ushort c)
        //{
        //    // Fast SPI bitbang swiped from LPD8806 library
        //    for (ushort bit = 0x80; bit; bit >>= 1 )
        //    {
        //        if (c & bit)
        //        {
        //            //digitalWrite(_mosi, HIGH); 
        //            *mosiport |= mosipinmask;
        //        }
        //        else
        //        {
        //            //digitalWrite(_mosi, LOW); 
        //            *mosiport &= ~mosipinmask;
        //        }
        //        //digitalWrite(_sclk, HIGH);
        //        *clkport |= clkpinmask;
        //        //digitalWrite(_sclk, LOW);
        //        *clkport &= ~clkpinmask;
        //    }
        //}

        // Send graphics data to the screen
        private void DisplayWriteData(byte[] data)
        {
            _dataCommandPin.Write(GpioPinValue.High);
            _spiDisplay.Write(data);
        }

        // Send commands to the screen
        private void DisplayWriteCommand(byte[] command)
        {
            _dataCommandPin.Write(GpioPinValue.Low);
            _spiDisplay.Write(command);
        }

        private void SetAddrWindow(ushort x0, ushort y0, ushort x1, ushort y1)
        {
            DisplayWriteCommand(HX8357_CASET); // Column addr set
            DisplayWriteData(new [] { (byte)(x0 >> 8), (byte)(x0 & 0xFF), (byte)(x1 >> 8), (byte)(x1 & 0xFF) });

            DisplayWriteCommand(HX8357_PASET); // Row addr set
            DisplayWriteData(new[] { (byte)(y0 >> 8), (byte)y0, (byte)(y1 >> 8), (byte)y1 });

            DisplayWriteCommand(HX8357_RAMWR); // write to RAM
        }

        public void SetRotation( uint m )
        {
            DisplayWriteCommand(HX8357_MADCTL);

            // DisplaySendData(new byte[] { 0x40, 0x80, 0x0 }); // MADCTL_MX 0x40, MADCTL_MY 0x80, MADCTL_RGB 0x00
            // DisplaySendData(new byte[] { 0x20, 0x80, 0x0 }); // MADCTL_MV 0x20, MADCTL_MY 0x80, MADCTL_RGB 0x00
            // DisplaySendData(new byte[] { 0x0 }); // MADCTL_RGB 0x00
            // DisplaySendData( new byte[] { 0x40, 0x80, 0x0 } ); // MADCTL_MX 0x40, MADCTL_MV 0x20, MADCTL_RGB 0x00

            ROTATION = m % 4; // can't be higher than 3
            switch (ROTATION)
            {
                case 0:
                    DisplayWriteData( new[] { MADCTL_MX, MADCTL_MY, MADCTL_RGB } );
                    _width = HX8357_TFTWIDTH;
                    _height = HX8357_TFTHEIGHT;
                    break;
                case 1:
                    DisplayWriteData(new[] { MADCTL_MV, MADCTL_MY, MADCTL_RGB });
                    _width = HX8357_TFTHEIGHT;
                    _height = HX8357_TFTWIDTH;
                    break;
                case 2:
                    DisplayWriteData(new [] { MADCTL_RGB });
                    _width = HX8357_TFTWIDTH;
                    _height = HX8357_TFTHEIGHT;
                    break;
                case 3:
                    DisplayWriteData( new[] { MADCTL_MX, MADCTL_MV, MADCTL_RGB });
                    _width = HX8357_TFTHEIGHT;
                    _height = HX8357_TFTWIDTH;
                    break;
            }
        }
    }
}
