﻿////////////////////////////////////////////////////////////////

#region Header

//
// Copyright (c) 2007-2010 MetaGeek, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion Header


////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using WirelessFireless.Localization;
using WirelessFireless.Misc;
using WirelessFireless.Scanning;

using MetaGeek.WiFi;

namespace WirelessFireless.UI.Controls
{
    public partial class TimeGraph : UserControl
    {
        #region Fields

        //Label spacing
        private int _amplitudeLabelSpacing = 10;

        //Fonts
        private readonly Font _boldFont;
        private readonly ContextMenuStrip _cmsCopy;
        private int _graphHeight;

        //Graph size
        private int _graphWidth;

        //Time boundries
        private DateTime _maxTime = DateTime.Now;
        private DateTime _minTime = DateTime.MinValue;

        //Pixel multipilers
        private float _pixelsPerDbm = 1;
        private float _pixelsPerSecond = 1;
        private readonly System.Timers.Timer _rTimer = new System.Timers.Timer(1000) { AutoReset = true };
        internal ScanController _scanner;
        private TimeSpan _secPerTick = TimeSpan.FromSeconds(60);
        private TimeSpan _timeSpan = TimeSpan.FromMinutes(5);

        #endregion Fields

        #region Properties

        /// <summary>
        /// Pixels from bottom to place border
        /// </summary>
        [Category("Margins"),
        DefaultValue(20)]
        public int BottomMargin
        {
            get; set;
        }

        /// <summary>
        /// Pixels from left to place border
        /// </summary>
        [Category("Margins"),
        DefaultValue(55)]
        public int LeftMargin
        {
            get; set;
        }

        /// <summary>
        /// The maximum amplitude in dB
        /// </summary>
        [Category("Configuration"),
        DefaultValue(-20)]
        public float MaxAmplitude
        {
            get; set;
        }

        /// <summary>
        /// The maximum (right most) time visible in the graph
        /// </summary>
        [Category("Configuration")]
        public DateTime MaxTime
        {
            get { return _maxTime; }
            set
            {
                _maxTime = value;
                _minTime = value - TimeSpan;
            }
        }

        /// <summary>
        /// The minimum amplitude in dB
        /// </summary>
        [Category("Configuration"),
        DefaultValue(-100)]
        public float MinAmplitude
        {
            get; set;
        }

        //Properties
        /// <summary>
        /// Pixels from right to place border
        /// </summary>
        [Category("Margins"),
        DefaultValue(10)]
        public int RightMargin
        {
            get; set;
        }

        /// <summary>
        /// Determines if the SSIDs are shown on that graph.
        /// </summary>
        [Category("Configuration"),
        DefaultValue(false)]
        public bool ShowSSIDs
        {
            get; set;
        }

        /// <summary>
        /// The ammount of time to display in the graph
        /// </summary>
        [Category("Configuration")]
        public TimeSpan TimeSpan
        {
            get { return _timeSpan; }
            set
            {
                _timeSpan = value;
                _minTime = _maxTime - value;
            }
        }

        /// <summary>
        /// Pixels from top to place border
        /// </summary>
        [Category("Margins"),
        DefaultValue(10)]
        public int TopMargin
        {
            get; set;
        }

        #endregion Properties

        #region Constructors

        public TimeGraph()
        {
            _minTime = _maxTime - _timeSpan;
            MinAmplitude = -100;
            MaxAmplitude = -10;
            BottomMargin = 20;
            TopMargin = 10;
            LeftMargin = 55;
            RightMargin = 20;
            ShowSSIDs = false;
            InitializeComponent();

            _rTimer.Elapsed += RTimerElapsed;

            SetStyle(ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer, true);

            _boldFont = new Font(Font, FontStyle.Bold);

            var copyItem = new ToolStripMenuItem("Copy to clipboard");
            copyItem.Click += CopyItemClick;

            _cmsCopy = new ContextMenuStrip();

            _cmsCopy.Items.Add(copyItem);

            ContextMenuStrip = _cmsCopy;
        }

        #endregion Constructors

        #region Public Methods

        public void SetScanner(ref ScanController scanner)
        {
            _scanner = scanner;
        }

        public void Start()
        {
            _rTimer.Start();
        }

        public void Stop()
        {
            _rTimer.Stop();
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawView(e.Graphics);
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        /// Adjusts the right margin to fit the SSID list
        /// </summary>
        /// <param name="minSize">The sise of the largest SSID</param>
        internal void AdjustRightMargin(SizeF minSize)
        {
            int oldm = RightMargin;
            //Minimum of 15 pixels from the right of the control to the list and 10 pixels from the list to the graph
            RightMargin = 15 + (int)minSize.Width + 10 + 15 + 18;
            if(oldm != RightMargin) Invalidate();
        }

        private static Color ColorFade(Color color, int age)
        {
            return Color.FromArgb(Math.Min(age*20 < 255 ? 255 - (age*20) : 0, 255), color);
        }

        private void CopyItemClick(object sender, EventArgs e)
        {
            try
            {
                //copy image to clipboard
                using (var bitClip = new Bitmap(Width, Height))
                {
                    using (Graphics g = Graphics.FromImage(bitClip))
                    {
                        //Fill the background with the back color so the graph looks right
                        g.Clear(BackColor);
                        //Draw the graph to the surface
                        DrawView(g);
                        //Then put it on the clipboard
                        Clipboard.SetImage(bitClip);
                    }
                }
            }
            catch
            {
                MessageBox.Show(Localizer.GetString("CopyGraphError"), Localizer.GetString("Error"),
                                MessageBoxButtons.OK);
            }
        }

        private int DbToY(int db)
        {
            return (int)(TopMargin + _graphHeight - ((db - MinAmplitude) * _pixelsPerDbm));
        }

        private void DrawGrid(Graphics graphics)
        {
            var pen = new Pen(DefaultColorScheme.GraphOutlineColor);
            var brush = new SolidBrush(DefaultColorScheme.GraphBackColor);

            graphics.FillRectangle(brush, LeftMargin - 1, TopMargin - 1, _graphWidth + 1, _graphHeight);

            graphics.DrawRectangle(pen, LeftMargin - 1, TopMargin - 1, _graphWidth + 1, _graphHeight + 1);

            brush.Color = DefaultColorScheme.GraphForeColor;

            //Draw rotated line and text
            var y = (_graphHeight / 2f) + graphics.MeasureString(Localizer.GetString("AmplitudedBm"), Font).Width / 2 + TopMargin;
            var rotationPoint = new PointF(8, y);
            var matrix = new Matrix();
            matrix.RotateAt(270, rotationPoint);
            graphics.Transform = matrix;
            graphics.DrawString(Localizer.GetString("AmplitudedBm"), Font, brush, 8, y);
            matrix.RotateAt(90, rotationPoint);
            graphics.Transform = matrix;

            // Y axis
            float maxAmpToLabel = MaxAmplitude - (_amplitudeLabelSpacing / 3f);
            int labelAmplitude = (int)(MinAmplitude - (MinAmplitude % _amplitudeLabelSpacing) + _amplitudeLabelSpacing);

            StringFormat sfAmp = new StringFormat { Alignment = StringAlignment.Far };

            brush.Color = DefaultColorScheme.GraphAxisLabelColor;
            while (labelAmplitude < maxAmpToLabel)
            {
                //Get the color
                //Left side
                sfAmp.Alignment = StringAlignment.Far;
                // amplitude label
                y = TopMargin + _graphHeight - ((labelAmplitude - MinAmplitude) * _pixelsPerDbm);
                graphics.DrawString(labelAmplitude.ToString(), Font, brush, LeftMargin - 5, y - 7,sfAmp);

                // Tick marks next to amplitude labels
                pen.Color = DefaultColorScheme.GraphTickColor;
                pen.DashStyle = DashStyle.Solid;
                graphics.DrawLine(pen, LeftMargin - 3, y, LeftMargin + 1, y);

                //Rgiht side

                sfAmp.Alignment = StringAlignment.Near;
                // amplitude label
                //y = TopMargin + _graphHeight - ((labelAmplitude - MinAmplitude) * _pixelsPerDbm);
                graphics.DrawString(labelAmplitude.ToString(), Font, brush, Width - RightMargin + 3, y - 7, sfAmp);

                // draw the horizontal graph lines
                pen.Color = DefaultColorScheme.GraphHorizontalDottedLineColor;
                pen.DashStyle = DashStyle.Dot;
                graphics.DrawLine(pen, LeftMargin, y, LeftMargin + _graphWidth, y);

                // Tick marks next to amplitude labels
                pen.Color = DefaultColorScheme.GraphTickColor;
                pen.DashStyle = DashStyle.Solid;
                graphics.DrawLine(pen, Width - RightMargin + 2, y, Width - RightMargin - 2, y);

                //Neutral


                labelAmplitude += _amplitudeLabelSpacing;
            }

            //Draw floor label and tick
            y = DbToY((int)MinAmplitude);
            pen.Color = DefaultColorScheme.GraphTickColor;
            pen.DashStyle = DashStyle.Solid;

            //Left
            sfAmp.Alignment = StringAlignment.Far;

            //Tick
            graphics.DrawLine(pen, LeftMargin - 3, y, LeftMargin + 1, y);
            //label
            graphics.DrawString(MinAmplitude.ToString(), Font, brush, LeftMargin - 5, y - 7, sfAmp);

            //Right
            sfAmp.Alignment = StringAlignment.Near;
            //Tick
            graphics.DrawLine(pen, Width - RightMargin + 2, y, Width - RightMargin - 2, y);
            pen.Color = DefaultColorScheme.GraphAxisLabelColor;
            //label
            graphics.DrawString(MinAmplitude.ToString(), Font, brush, Width - RightMargin + 3, y - 7, sfAmp);

            pen.Dispose();
        }

        //Draws ssid labels
        private void DrawLabels(Graphics graphics)
        {
            if(_scanner == null) return;
            if(_scanner.Cache.Count < 1) return;

            var brush = new SolidBrush(DefaultColorScheme.GraphForeColor);
            var pen = new Pen(DefaultColorScheme.GraphOutlineColor);

            var szString = new SizeF(0,0);
            var szBox = new Size(0,0);

            foreach (var ap in _scanner.Cache.GetAccessPoints())
            {
                if (!ap.Graph) continue;
                //Meassure the SSIDs to find the longest
                SizeF tempSz = graphics.MeasureString(ap.Ssid, ap.Highlight ? _boldFont : Font);
                if (tempSz.Width > szString.Width) szString.Width = tempSz.Width;
                if (tempSz.Height > szString.Height) szString.Height = tempSz.Height;
                szBox.Height += (int)tempSz.Height + 2;
            }

            szBox.Height += 5;

            if (szBox.Height > Height - BottomMargin - TopMargin) szBox.Height = Height - BottomMargin - TopMargin + 1;

            AdjustRightMargin(szString);
            UpdateGraphDimensions();

            //Background
            brush.Color = DefaultColorScheme.GraphLegendBackColor;
            graphics.FillRectangle(brush, Width - RightMargin + 30, TopMargin - 1, szString.Width + 15, szBox.Height);
            //Outline
            graphics.DrawRectangle(pen, Width - RightMargin + 30, TopMargin - 1, szString.Width + 15, szBox.Height);

            var b = new SolidBrush(DefaultColorScheme.GraphForeColor);
            var y = TopMargin + 5;
            var x = Width - RightMargin + 30;
            foreach (var ap in _scanner.Cache.GetAccessPoints())
            {
                if (!ap.Graph) continue;
                if (y + szString.Height > Height - BottomMargin - TopMargin)
                {
                    graphics.DrawString("...", Font, b, x + 10, y - (szString.Height /2));
                    break;
                }
                graphics.DrawString(ap.Ssid, ap.Highlight ? _boldFont : Font, b, x + 10, y);
                graphics.FillRectangle(new SolidBrush(ap.MyColor), x + 5, y + 5, 5, 3);
                y += (int)szString.Height + 2;
            }

            brush.Dispose();
            pen.Dispose();
        }

        private void DrawNetworks(Graphics graphics)
        {
            if (_scanner == null) return;
            if (_scanner.Cache.Count < 1) return;

            var pen = new Pen(Color.White);
            var tPoints = new List<Point>();
            NetworkData[] data;

                foreach (var ap in _scanner.Cache.GetAccessPoints())
                {
                    tPoints.Clear();
                    if (ap.MyNetworkDataCollection.Count < 2) continue;
                    try
                    {
                        if (!ap.Graph) continue;
                    }
                    catch (Exception)
                    {

                    }

                    data = ap.GetDataUntilTime(_minTime);

                    float[] pts = new float[data.Length];
                    Color[] cols = new Color[data.Length];

                    DateTime lastAp = data[0].MyTimestamp;

                    for (int i = 0; i < data.Length; i++)
                    {
                        double tot1 = (data[i].MyTimestamp - lastAp).TotalSeconds;
                        double tot2 = (data[data.Length - 1].MyTimestamp - lastAp).TotalSeconds;
                        int toti1 = (int)tot1;
                        int toti2 = (int)tot2;

                        pts[i] = (toti1 / (float)toti2);
                        cols[i] = ColorFade(ap.MyColor, data[i].Age);
                    }

                    //Fix min and max pos.
                    if (pts[0] > 0f) pts[0] = 0f;
                    if (pts[pts.Length - 1] < 1f) pts[pts.Length - 1] = 1f;

                    try
                    {
                        pen.Width = ap.Highlight ? 4 : 1;
                        var psl = new List<Point>();
                        foreach (var nd in data)
                        {
                            psl.Add(new Point(TimeToX(nd.MyTimestamp)+1, DbToY(nd.Rssi)));
                        }
                        var ps = psl.ToArray();

                        var lgb = new LinearGradientBrush(new Point(ps[0].X, 0), new Point(ps[ps.Length -1].X, 0), Color.White,
                                                                          Color.White)
                                                      {
                                                          InterpolationColors =
                                                              new ColorBlend {Colors = cols, Positions = pts}
                                                      };

                        pen.Brush = lgb;

                        graphics.DrawLines(pen, ps);

                    }
                    catch(Exception)
                    {

                    }
                }
        }

        private void DrawTimeLabels(Graphics graphics)
        {
            var pen = new Pen(DefaultColorScheme.GraphTickColor);
            Brush bLabel = new SolidBrush(DefaultColorScheme.GraphForeColor);

            SizeF szString;
            int x, y;
            string label;
            RectangleF rect;
            for (DateTime i = MaxTime - TimeSpan.FromTicks(MaxTime.Ticks % _secPerTick.Ticks); i > _minTime; i -= TimeSpan.FromMinutes(1))
            {
                x = TimeToX(i);
                y = Height - BottomMargin;
                //Draw the tick
                graphics.DrawLine(pen, x, y, x, y + 3);

                label = i.ToShortTimeString();
                label = label.Replace(" AM", "").Replace(" PM", "");
                szString = graphics.MeasureString(label, Font);
                rect = new RectangleF(x - (szString.Width/2), y + 4, szString.Width, szString.Height);

                graphics.DrawString(label, Font, bLabel, rect);
            }
        }

        /// <summary>
        /// Draws the whole graph to the specified drawing surface.
        /// </summary>
        /// <param name="graphics"></param>
        private void DrawView(Graphics graphics)
        {
            //graphics.SmoothingMode = SmoothingMode.HighQuality;
            //Draw the amplitude grid and labels
            if(_scanner != null && _scanner.Cache.NewestTimestamp != DateTime.MinValue)
            {
                MaxTime = _scanner.Cache.NewestTimestamp;
            }
               // Console.WriteLine("DrawGrid");
            DrawGrid(graphics);

            //Console.WriteLine("DrawLabels");
            DrawTimeLabels(graphics);

            // set cropping region
            graphics.Clip = new Region(new Rectangle(LeftMargin, TopMargin, _graphWidth, _graphHeight));

            //Console.WriteLine("DrawNetworks");
            //graphics.SmoothingMode = SmoothingMode.HighQuality;
            DrawNetworks(graphics);
            //graphics.SmoothingMode = SmoothingMode.None;

            // reset cropping region
            graphics.ResetClip();

            //Console.WriteLine("DrawLabels");
            if(ShowSSIDs) DrawLabels(graphics);
            //Console.WriteLine("==========");
        }

        private void RTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invalidate();
        }

        private void TimeGraphN_SizeChanged(object sender, EventArgs e)
        {
            UpdateGraphDimensions();
            Invalidate();
        }

        private int TimeToX(DateTime time)
        {
            TimeSpan tsp = time - _minTime;
            return (int)((tsp.TotalSeconds * _pixelsPerSecond) + LeftMargin);
        }

        private void UpdateGraphDimensions()
        {
            if ((Height > 0) && (Width > 0))
            {

                _graphWidth = (Width - LeftMargin - RightMargin);
                _graphHeight = (Height - TopMargin - BottomMargin);

                float viewableRange = MaxAmplitude - MinAmplitude + 1;
                _pixelsPerDbm = _graphHeight / viewableRange;

                if (_pixelsPerDbm < 1.1)
                {
                    _amplitudeLabelSpacing = 20;
                }
                else if (_pixelsPerDbm < 3.3)
                {
                    _amplitudeLabelSpacing = 10;
                }
                else if (_pixelsPerDbm < 6.5)
                {
                    _amplitudeLabelSpacing = 5;
                }
                else if (_pixelsPerDbm >= 6.5)
                {
                    _amplitudeLabelSpacing = 2;
                }

                viewableRange = (float)TimeSpan.TotalSeconds;
                _pixelsPerSecond = _graphWidth / viewableRange;
            }
        }

        #endregion Private Methods
    }
}