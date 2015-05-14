using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.UI.Xaml.Controls;

namespace Adafruit_HX8357
{
    public class Tft
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
        private static readonly short HX8357_TFTWIDTH = 480;
        private static readonly short HX8357_TFTHEIGHT = 320;

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
        private static readonly byte[] HX8357_RAMRD = { 0x2E };

        // Color definitions
        //private static readonly byte[] HX8357_BLACK = { 0x0000 }; 
        //private static readonly byte[] HX8357_BLUE = { 0x001F }; 
        //private static readonly byte[] HX8357_RED = { 0xF800 }; 
        //private static readonly byte[] HX8357_GREEN = { 0x07E0 }; 
        //private static readonly byte[] HX8357_CYAN = { 0x07FF }; 
        //private static readonly byte[] HX8357_MAGENTA = { 0xF81F }; 
        //private static readonly byte[] HX8357_YELLOW = { 0xFFE0 };   
        //private static readonly byte[] HX8357_WHITE = { 0xFFFF }; 

        public async Task Begin()
        {
            try
            {
                // InitGpio();             // Initialize the GPIO controller and GPIO pins
                await InitSpi();        // Initialize the SPI controller
                await InitDisplay();    // Initialize the display
            }
            catch (Exception ex)
            {
                Debug.WriteLine( "Exception: " + ex.Message );
                if (ex.InnerException != null)
                {
                    Debug.WriteLine( "Inner Exception: " + ex.InnerException.Message );
                }
            }
        }

        public void DrawPixel(short x, short y, ushort color)
        {
            if ((x < 0) || (x >= HX8357_TFTWIDTH) || (y < 0) || (y >= HX8357_TFTHEIGHT)) return;

            SetAddrWindow((ushort)x, (ushort)y, (ushort)(x + 1), (ushort)(y + 1));

            DisplayWriteData((byte)(color >> 8));
            DisplayWriteData((byte)color);
        }

        public void DrawColor(ushort color)
        {
            Debug.WriteLine("->DrawColor()");
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
            for (var i = 0; i < 32000; i++)
            {
                data.Add(hcolor);
                data.Add(lcolor);
            }

            DisplayWriteData(data.ToArray());
            Debug.WriteLine("<-DrawColor()");
        }

        public void FillRect(short x, short y, short w, short h, ushort color)
        {
            // rudimentary clipping (drawChar w/big text requires this)
            //if ((x >= HX8357_TFTWIDTH) || (y >= HX8357_TFTHEIGHT)) return;
            //if ((x + w - 1) >= HX8357_TFTWIDTH) w =(HX8357_TFTWIDTH - x);
            //if ((y + h - 1) >= HX8357_TFTHEIGHT) h = HX8357_TFTHEIGHT - y;

            SetAddrWindow((ushort)x, (ushort)y, (ushort)(x + w - 1), (ushort)(y + h - 1));

            // byte hi = (color >> 8), lo = color;

            var hi = (byte)(color >> 8);
            var lo = (byte)(color);

            var data = new List<byte>();
            for (y = h; y > 0; y--)
            {
                for (x = w; x > 0; x--)
                {
                    // Debug.WriteLine("hi = " + hi );
                    DisplayWriteData(hi);
                    // Debug.WriteLine("lo = " + lo);
                    DisplayWriteData(lo);
                    // data.Add(hi);
                    // data.Add(lo);
                }
            }
            // DisplayWriteData( data.ToArray() );
        }

        public void FillScreen(ushort color)
        {
            FillRect(0, 0, HX8357_TFTWIDTH, HX8357_TFTHEIGHT, color);
        }

        // Send graphics data to the screen
        private void DisplayWriteData(byte[] data)
        {
            // Debug.WriteLine("Data = " + BitConverter.ToString(data));
            try
            {
                _dataCommandPin.Write(GpioPinValue.High);
                _spiDisplay.Write(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                throw;
            }
        }

        private void DisplayWriteData(byte data)
        {
            // Debug.WriteLine("Data = " + data );
            try
            {
                _dataCommandPin.Write(GpioPinValue.High);
                _spiDisplay.Write(new[] { data });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                throw;
            }
        }

        // Send commands to the screen
        private void DisplayWriteCommand(byte[] command)
        {
            Debug.WriteLine( "Command = 0x" + BitConverter.ToString(command).Replace("-", ", 0x") );
            try
            {
                _dataCommandPin.Write(GpioPinValue.Low);
                _spiDisplay.Write(command);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in DisplayWriteCommand(byte[] command): " + ex.Message);
                throw;
            }
        }

        private void SetAddrWindow(ushort x0, ushort y0, ushort x1, ushort y1)
        {
            Debug.WriteLine("->SetAddrWindow()");
            DisplayWriteCommand(HX8357_CASET); // Column addr set
            DisplayWriteData((byte)(x0 >> 8));
            DisplayWriteData((byte)(x0 >> 8));
            DisplayWriteData((byte)(x0 & 0xFF)); // XSTART 
            DisplayWriteData((byte)(x1 >> 8));
            DisplayWriteData((byte)(x1 & 0xFF)); // XEND

            DisplayWriteCommand(HX8357_PASET); // Row addr set
            DisplayWriteData((byte)(y0 >> 8));
            DisplayWriteData((byte)y0);     // YSTART
            DisplayWriteData((byte)(y1 >> 8));
            DisplayWriteData((byte)y1);     // YEND

            DisplayWriteCommand(HX8357_RAMWR); // write to RAM
            Debug.WriteLine("<-SetAddrWindow()");
        }

        // Send SPI commands to power up and initialize the display
        private async Task InitDisplay()
        {
            Debug.WriteLine("->InitDisplay()");
            try
            {
                DisplayWriteCommand(HX8357_SWRESET);
                await Task.Delay(300);

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
            Debug.WriteLine("<-InitDisplay()");
        }

        // Initialize the SPI bus
        private async Task InitSpi()
        {
            Debug.WriteLine("->InitSpi()");
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
                
                // Create SPI initialization settings
                var settings = new SpiConnectionSettings(SpiChipSelectLine)
                {
                    ClockFrequency = 30000000, // Datasheet specifies maximum SPI clock frequency of ???MHz
                    // DataBitLength = 32,
                    Mode = SpiMode.Mode0
                };
                
                var spiAqs = SpiDevice.GetDeviceSelector(SpiControllerName);   // Find the selector string for the SPI bus controller
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);  // Find the SPI bus controller device with our selector string
                _spiDisplay = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings); // Create an SpiDevice with our bus controller and SPI settings
            }
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
            Debug.WriteLine("<-InitSpi()");
        }

        // Initialize the GPIO
        private void InitGpio()
        {
            Debug.WriteLine("->InitGpio()");
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
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }
            Debug.WriteLine("<-InitGpio()");
        }


        public void Dispose()
        {
            // Cleanup
            _spiDisplay.Dispose();
            _resetPin.Dispose();
            _dataCommandPin.Dispose();
        }

    }
}
