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
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.UI.Xaml.Controls;
using DisplayFont;

namespace SPIDisplay
{

    public sealed partial class MainPage : Page
    {
        private const string SpiControllerName = "SPI0";  // For Raspberry Pi 2, use SPI0
        private const int SpiChipSelectLine = 0;       // Line 0 maps to physical pin number 24 on the Rpi2
        private const int DataCommandPin = 25;         // We use GPIO 22 since it's conveniently near the SPI pins
        private const int ResetPin = 24;               // We use GPIO 23 since it's conveniently near the SPI pins

        // Definitions for SPI and GPIO
        private SpiDevice _spiDisplay;
        private GpioController _ioController;
        private GpioPin _dataCommandPin;
        private GpioPin _resetPin;

        // Display Properties
        private static readonly int HX8357_TFTWIDTH = 480;
        private static readonly int HX8357_TFTHEIGHT = 320;

        // Display commands.
        private static readonly byte[] CmdDisplayOn = { 0x29 }; // Turns the display on
        private static readonly byte[] CmdDisplayReset = { 0x01 };
        private static readonly byte[] CmdExitSleep = { 0x11 };
        private static readonly byte[] CmdResetcoladdr = { 0x2A }; // Reset the column address pointer
        private static readonly byte[] CmdResetpageaddr = { 0x2B }; // Reset the page address pointer

        private static readonly byte[] HX8357_CASET = {0x2A};
        private static readonly byte[] HX8357_PASET = {0x2B};
        private static readonly byte[] HX8357_RAMWR = { 0x2C };
        private static readonly byte[] HX8357_RAMRD = { 0x2E };

        public MainPage()
        {
            InitializeComponent();

            // Register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;

            // Initialize GPIO, SPI, and the display
            InitAll();
        }

        // Initialize GPIO, SPI, and the display
        private async void InitAll()
        {
            try
            {
                InitGpio();             // Initialize the GPIO controller and GPIO pins
                await InitSpi();        // Initialize the SPI controller
                await InitDisplay();    // Initialize the display
            }
            catch (Exception ex) // If initialization fails, display the exception and stop running
            {
                //Text_Status.Text = "Exception: " + ex.Message;
                if (ex.InnerException != null)
                {
                    //Text_Status.Text += "\nInner Exception: " + ex.InnerException.Message;
                }
                return;
            }

            DrawColor(0xf800); //Red
            DrawColor(0x07e0); //Green
            DrawColor(0x001f); //Blue
            DrawColor(0x0000); //Black
            DrawColor(0xffe0); //Yellow
            DrawColor(0xffff); //White

            // FillScreen(0x0000);

            FillRect( 0, 0, 320, 480, 0xffe0); // Yellow

            FillRect(100, 100, 100, 100, 0xf800); // Red
            FillRect(100, 100, 100, 100, 0x07e0); // Green
            FillRect(100, 100, 100, 100, 0x001f); // Blue
            FillRect(100, 100, 100, 100, 0x0000); // Black
            FillRect(100, 100, 100, 100, 0xffe0); // Yellow
            FillRect(100, 100, 100, 100, 0xffff); // White
        }

        // Initialize the SPI bus
        private async Task InitSpi()
        {
            try
            {
                var settings = new SpiConnectionSettings(SpiChipSelectLine)
                {
                    ClockFrequency = 16000000,
                    Mode = SpiMode.Mode0
                }; //  Create SPI initialization settings
                // Datasheet specifies maximum SPI clock frequency of 10MHz

                var spiAqs = SpiDevice.GetDeviceSelector(SpiControllerName);   // Find the selector string for the SPI bus controller
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);  // Find the SPI bus controller device with our selector string
                _spiDisplay = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings); // Create an SpiDevice with our bus controller and SPI settings

            }
            catch (Exception ex) // If initialization fails, display the exception and stop running
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        // Send SPI commands to power up and initialize the display
        private async Task InitDisplay()
        {
            // Initialize the display
            try
            {
                DisplayWriteCommand( CmdDisplayReset ); // HX8357_SWRESET 0x01

                DisplayWriteCommand( new byte[] { 0xB9 } ); // HX8357D_SETC 0xB9
                DisplayWriteData(new byte[] { 0xFF, 0x83, 0x57 });

                await Task.Delay(300);

                DisplayWriteCommand( new byte[] { 0xB3 } ); // HX8357_SETRGB 0xB3, enable SDO pin!
                DisplayWriteData(new byte[] { 0x80, 0x0, 0x06, 0x06 });

                DisplayWriteCommand( new byte[] { 0xB3 } ); // HX8357D_SETCOM  0xB6, -1.52V
                DisplayWriteData(new byte[] { 0x25 });

                DisplayWriteCommand( new byte[] { 0xB3 } ); // HX8357_SETOSC 0xB0, Normal mode 70Hz, Idle mode 55 Hz
                DisplayWriteData(new byte[] { 0x68 });

                DisplayWriteCommand( new byte[] { 0xCC } ); // HX8357_SETPANEL 0xCC, BGR, Gate direction swapped
                DisplayWriteData(new byte[] { 0x05 });

                DisplayWriteCommand( new byte[] { 0xB1 } ); // HX8357_SETPWR1 0xB1, BT, VSPR, VSNR, AP, FS
                DisplayWriteData(new byte[] { 0x00, 0x15, 0x1C, 0x1C, 0x83, 0xAA });

                DisplayWriteCommand( new byte[] { 0xC0 } ); // HX8357D_SETSTBA 0xC0, OPON normal, OPON idle, STBA, STBA, STBA, GEN
                DisplayWriteData(new byte[] { 0x50, 0x50, 0x01, 0x3C, 0x1E, 0x08 });

                DisplayWriteCommand( new byte[] { 0xB4 } ); // HX8357D_SETCYC 0xB4, NW 0x02, RTN, DIV, DUM, DUM, GDON, GDOFF
                DisplayWriteData(new byte[] { 0x02, 0x40, 0x00, 0x2A, 0x2A, 0x0D, 0x78 });

                DisplayWriteCommand( new byte[] { 0xE0 } ); // HX8357D_SETGAMMA 0xE0
                DisplayWriteData(new byte[] { 0x02, 0x0A, 0x11, 0x1d, 0x23, 0x35, 0x41, 0x4b, 0x4b, 0x42, 0x3A, 0x27, 0x1B, 0x08, 0x09, 0x03, 0x02, 0x0A, 0x11, 0x1d, 0x23, 0x35, 0x41, 0x4b, 0x4b, 0x42, 0x3A, 0x27, 0x1B, 0x08, 0x09, 0x03, 0x00, 0x01 });

                DisplayWriteCommand( new byte[] { 0x3A  } ); // HX8357_COLMOD 0x3A, 16 bit
                DisplayWriteData(new byte[] { 0x55 });

                DisplayWriteCommand( new byte[] { 0x36 } ); // HX8357_MADCTL 0x36, 
                DisplayWriteData(new byte[] { 0xC0 });

                DisplayWriteCommand( new byte[] { 0xB3 } ); // HX8357_TEON 0x35, TE off
                DisplayWriteData(new byte[] { 0x00 });

                DisplayWriteCommand( new byte[] { 0x44 } ); // HX8357_TEARLINE 0x44 
                DisplayWriteData(new byte[] { 0x00, 0x02 });

                DisplayWriteCommand( CmdExitSleep ); // HX8357_SLPOUT 0x11
                await Task.Delay(150);

                DisplayWriteCommand( CmdDisplayOn ); // HX8357_DISPON 0x29, Turns the display on
                await Task.Delay(50);

                // DisplaySendCommand( new byte[] { 0x36 } ); // Rotation HX8357_MADCTL
                // DisplaySendData(new byte[] { 0x40, 0x80, 0x0 }); // MADCTL_MX 0x40, MADCTL_MY 0x80, MADCTL_RGB 0x00
                // DisplaySendData(new byte[] { 0x20, 0x80, 0x0 }); // MADCTL_MV 0x20, MADCTL_MY 0x80, MADCTL_RGB 0x00
                // DisplaySendData(new byte[] { 0x0 }); // MADCTL_RGB 0x00
                // DisplaySendData( new byte[] { 0x40, 0x80, 0x0 } ); // MADCTL_MX 0x40, MADCTL_MV 0x20, MADCTL_RGB 0x00

            }
            catch (Exception ex)
            {
                throw new Exception("Display Initialization Failed", ex);
            }
        }

        private void SetAddrWindow( int x0, int y0, int x1, int y1)
        {
            DisplayWriteCommand(HX8357_CASET); // Column addr set
            DisplayWriteData((byte)(x0 >> 8) );
            DisplayWriteData((byte)(x0 & 0xFF));     // XSTART 
            DisplayWriteData((byte)(x1 >> 8) );
            DisplayWriteData((byte)(x1 & 0xFF));     // XEND

            DisplayWriteCommand(HX8357_PASET); // Row addr set
            DisplayWriteData((byte)(y0 >> 8));
            DisplayWriteData((byte)y0 );     // YSTART
            DisplayWriteData((byte)(y1 >> 8));
            DisplayWriteData((byte)y1 );     // YEND

            DisplayWriteCommand(HX8357_RAMWR); // write to RAM
        }

        private void DrawPixel( int x, int y, uint color)
        {

            if ((x < 0) || (x >= HX8357_TFTWIDTH) || (y < 0) || (y >= HX8357_TFTHEIGHT)) return;

            SetAddrWindow( x, y, x + 1, y + 1 );

            DisplayWriteData((byte)(color >> 8));
            DisplayWriteData((byte)color);
        }

        private void FillRect( int x, int y, int w, int h, uint color)
        {

            // rudimentary clipping (drawChar w/big text requires this)
            if ((x >= HX8357_TFTWIDTH) || (y >= HX8357_TFTHEIGHT)) return;
            if ((x + w - 1) >= HX8357_TFTWIDTH) w = HX8357_TFTWIDTH - x;
            if ((y + h - 1) >= HX8357_TFTHEIGHT) h = HX8357_TFTHEIGHT - y;

            SetAddrWindow(x, y, x + w - 1, y + h - 1);

            // uint hi = color >> 8, lo = color;
            var hi = (byte)(color >> 8);
            var lo = (byte)(color);

            // var data = new List<byte>();
            for (y = h; y > 0; y--)
            {
                for (x = w; x > 0; x--)
                {
                    DisplayWriteData( hi );
                    DisplayWriteData( lo );
                    // data.Add(hi);
                    // data.Add(lo);
                }
            }
            // DisplayWriteData(data.ToArray());

        }

        private void FillScreen( uint color )
        {
            FillRect(0, 0, HX8357_TFTWIDTH, HX8357_TFTHEIGHT, color);
        }

        private void DrawColor(ushort color)
        {
            //This part of code does not seem to work. 

            //ushort startX = 100;
            //ushort endX = 200;

            //ushort startY = 100;
            //ushort endY = 200;

            //DisplaySendCommand(new byte[] { 0x2A });
            //DisplaySendData(new byte[] { (byte)(startX >> 8), (byte)(startX >> 0xff), (byte)(endX >> 8), (byte)(endX >> 0xff) });

            //DisplaySendCommand(new byte[] { 0x2B });
            //DisplaySendData(new byte[] { (byte)(startY >> 8), (byte)(startY >> 0xff), (byte)(endY >> 8), (byte)(endY >> 0xff) });

            DisplayWriteCommand(new byte[] { 0x2C }); // HX8357_RAMWR   0x2C

            var hcolor = (byte)(color >> 8);
            var lcolor = (byte)(color & 0xff);

            var data = new List<byte>();
            for (int i = 0; i < 32400; i++)
            {
                data.Add(hcolor);
                data.Add(lcolor);
            }

            DisplayWriteData( data.ToArray() );
        }

        // Initialize the GPIO
        private void InitGpio()
        {
            try
            {
                _ioController = GpioController.GetDefault(); // Get the default GPIO controller on the system

                // Initialize a pin as output for the Data/Command line on the display
                _dataCommandPin = _ioController.OpenPin(DataCommandPin);
                _dataCommandPin.Write(GpioPinValue.High);
                _dataCommandPin.SetDriveMode(GpioPinDriveMode.Output);

                // Initialize a pin as output for the hardware Reset line on the display
                _resetPin = _ioController.OpenPin(ResetPin);
                _resetPin.Write(GpioPinValue.High);
                _resetPin.SetDriveMode(GpioPinDriveMode.Output);
            }
            // If initialization fails, throw an exception
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }
        }

        // Send graphics data to the screen
        private void DisplayWriteData( byte[] data )
        {
            _dataCommandPin.Write(GpioPinValue.High);
            _spiDisplay.Write(data);
        }

        private void DisplayWriteData( byte data )
        {
            _dataCommandPin.Write(GpioPinValue.High);
            _spiDisplay.Write( new[] { data } );
        }

        // Send commands to the screen
        private void DisplayWriteCommand( byte[] command )
        {
            _dataCommandPin.Write( GpioPinValue.Low );
            _spiDisplay.Write( command );
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            _spiDisplay.Dispose();
            _resetPin.Dispose();
            _dataCommandPin.Dispose();
        }
    }
}
