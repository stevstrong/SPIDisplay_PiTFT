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
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using DisplayFont;
using Adafruit_HX8357;

namespace SPIDisplay
{

    public sealed partial class MainPage
    {
        private readonly Tft _tft = new Tft( 0, 25, "SPI0");

        public MainPage()
        {
            InitializeComponent();

            InitAll();
        }

        private async void InitAll()
        {
            try
            {
                await _tft.Begin();
            }
            catch (Exception ex)
            {
                Debug.WriteLine( "Exception" + ex.Message );
                return;
            }

            while (true)
            {
                var sw = new Stopwatch();
                sw.Start();
                _tft.FillScreen(0x0000);
                sw.Stop();
                Debug.WriteLine( "FillScreen took " + sw.Elapsed.Seconds + " seconds." );

                //_tft.DrawColor(0xf800); //Red
                //_tft.DrawColor(0x07e0); //Green
                //_tft.DrawColor(0x001f); //Blue
                //_tft.DrawColor(0x0000); //Black
                //_tft.DrawColor(0xffe0); //Yellow
                //_tft.DrawColor(0xffff); //White

                _tft.FillRect(0, 0, 180, 180, 0xffe0); // Yellow

                //for (short w = 0; w < 200; w++)
                //{
                //    for (short h = 0; h < 300; h++)
                //    {
                //       _tft.DrawPixel( w, h, 0x001f);
                //    }
                //}

                _tft.FillRect(0, 0, 10, 10, 0xf800); // Red
                _tft.FillRect(100, 100, 100, 100, 0x07e0); // Green
                _tft.FillRect(100, 100, 100, 100, 0x001f); // Blue
                _tft.FillRect(100, 100, 100, 100, 0x0000); // Black
                _tft.FillRect(100, 100, 100, 100, 0xffe0); // Yellow
                _tft.FillRect(100, 100, 100, 100, 0xffff); // White
            }
        }
    }
}
