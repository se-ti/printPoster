using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

using System.Text;

using R = printPoster.Properties.Resources;
using System.Drawing.Printing;

namespace printPoster
{
    public partial class CMainForm : Form
    {
        MapPrintDocument printDocument;
        int scale;

        protected Size PageSize { get; private set; }
        protected int NumPages { get; private set; }

        Rectangle selection;
        Point start;

        public CMainForm()
        {
            InitializeComponent();

            panel1.ZoomEvent += panel1_OnZoom; 
            printDocument = new MapPrintDocument();
            printDocument.PrintPage += printDocument_PrintPage;

            const int defMargins = (int) (10 * 100 / 25.4m) + 1; // 10mm in 1/100 of the inch
            printDocument.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(defMargins, defMargins, defMargins, defMargins);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
                LoadImage(openFileDialog1.FileName, openFileDialog1.SafeFileName);
        }

        public void LoadImage(string path, string title)
        {
            try
            {
                printDocument.Load(path, title);

                Image im = printDocument.Image;
                ZoomToFit(im.Size);

                this.Text = String.Format("{0}, {1} x {2} px", title, im.Width, im.Height);
                SetupResolutions(im.VerticalResolution);
                TuneMenu(true);
            }
            catch (Exception ex)
            {
                TuneMenu(false);
                ShowError(R.ErrorOpening, String.Format(R.FileCorruptFmt, title));
            }

#if DEBUG
            GetMetadata(printDocument.Image);

            var test = new TestMapPrintDocument();
            test.Test();
            var s = test.Log;
#endif
        }

        private void GetMetadata(Image image)
        {
            List<KeyValuePair<int, string>> knownIds = new List<KeyValuePair<int, string>>();
            knownIds.Add(new KeyValuePair<int, string>(0x0320, "im title"));
            knownIds.Add(new KeyValuePair<int, string>(0x010F, "eq manuf"));
            knownIds.Add(new KeyValuePair<int, string>(0x0110, "eq model"));
            knownIds.Add(new KeyValuePair<int, string>(0x9003, "exif orig"));
            knownIds.Add(new KeyValuePair<int, string>(0x829A, ""));
            knownIds.Add(new KeyValuePair<int, string>(0x5090, ""));
            knownIds.Add(new KeyValuePair<int, string>(0x5091, ""));


            int i = 0;
            foreach (var prop in image.PropertyItems)
                if (knownIds.Any(k => k.Key == prop.Id))
                    i = 1;
                else
                    i = 2;

            String s = ((ImageFlags)image.Flags).ToString();
        }

        private void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        #region scale-related 

        private static decimal[] m_scales = new decimal[] { 0.25m, 1m / 3, .5m, 1, /*1.33m,*/ 1.5m, /*1.66m,*/ 2, 2.5m, 3, 3.5m, 4 }; // а дальше через 1
        private decimal ScaleKoeff(int scale)
        {
            return scale < m_scales.Length ? m_scales[scale] : ((scale - m_scales.Length) + 5);
        }

        private decimal ScaleK { get { return ScaleKoeff(scale); } }
        private void Scale(int scl, Point? zoomCenter = null)
        {
            var kOld = ScaleK;
//            var m0 = panel1.HorizontalScroll.Maximum;

            if (zoomCenter == null)
            {
                var rect = panel1.ClientRectangle;
                zoomCenter = pictureBox1.PointToClient(panel1.PointToScreen(new Point(rect.Width / 2, rect.Height / 2)));
            }

            var image = printDocument.Image;

            scale = scl < 0 ? 0 : scl;

            const int tooMuchRAM = 800000000; // 800 Mpx * 32 bpp = 3.2 GB
            var k3 = scale > 0  ? ScaleKoeff(scale - 1) : 1;
            zoomInButton.Enabled = scale > 0 && image.Height * image.Width / k3 / k3 < tooMuchRAM;
            zoomInToolStripMenuItem.Enabled = zoomInButton.Enabled;

            var k = ScaleK;

            pictureBox1.Width = (int) Math.Round(image.Width / k);
            pictureBox1.Height = (int) Math.Round(image.Height / k);
            

            /*if (k < 0.5m)
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = printDocument.Image;
            }
            else*/
            {
                pictureBox1.SizeMode = PictureBoxSizeMode.Normal;
                pictureBox1.Image = new Bitmap(printDocument.Image, pictureBox1.Size);
            }

            ScaleScroll(panel1.VerticalScroll, kOld, k, zoomCenter.Value.Y);
            ScaleScroll(panel1.HorizontalScroll, kOld, k, zoomCenter.Value.X);

//            var m1 = panel1.HorizontalScroll.Maximum;
  //          var h = panel1.HorizontalScroll.Value;

            zoomStatusLabel.Text = String.Format(R.ZoomFmt, Math.Round(100m/k, 2));
        }

        private void ScaleScroll(ScrollProperties sp, decimal kOld, decimal kNew, int picPos)
        {
            if (!sp.Visible || true)
                return;

            decimal val = (picPos) * kOld / kNew - (picPos - sp.Value);
            sp.Value = Math.Min(Math.Max((int) val, sp.Minimum), sp.Maximum);
        }

        private void ZoomIn()
        {
            Scale(scale - 1);
        }

        private void ZoomOut()
        {
            Scale(scale + 1);
        }

        private void ResetZoom()
        {
            Scale(3);
        }

        private void ZoomToFit(Size sz)
        {
            panel1.VerticalScroll.Visible = false;
            panel1.HorizontalScroll.Visible = false;
            var main = panel1.ClientSize;
            decimal k = Math.Max(1m * sz.Width / main.Width, 1m * sz.Height / main.Height);

            scale = 3; // indexOf(m_scales == 1)
            while (ScaleK < k)
                scale++;

            Scale(scale);
        }

        private void zoomOutButton_Click(object sender, EventArgs e)
        {
            ZoomOut();
        }

        private void zoomInButton_Click(object sender, EventArgs e)
        {
            ZoomIn();
        }

        private void zoomToWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ZoomToFit(printDocument.Image.Size);
        }

        private void panel1_OnZoom(object sender, ScrollPanel.ZoomEventArgs e)
        {
            if (printDocument.Image != null && (e.Delta <= 0 || zoomInButton.Enabled))
                Scale(scale - e.Delta, e.Location);
        }

        private static RectangleF DocCoord2ViewCoord(Rectangle r, decimal k)
        {
            //return new Rectangle( (int) (r.X/ k), (int)(r.Y / k), (int)(r.Width / k), (int)(r.Height / k));
            return new RectangleF(((float)r.X) / (float)k, ((float)r.Y) / (float)k, ((float)r.Width) / ((float)k), ((float)r.Height) / (float)k);
        }

        private static Rectangle ViewCoord2DocCoord(Rectangle r, decimal k)
        {
            return new Rectangle((int)(r.X * k), (int)(r.Y * k), (int)(r.Width * k), (int)(r.Height * k));
        }

        private static Rectangle RectFromPoints(Point a, Point b)
        {
            return new Rectangle(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        #endregion

        private void TuneMenu(bool enable)
        {
            pageSetupToolStripMenuItem.Enabled = enable;
            printToolStripMenuItem1.Enabled = enable;
            printPreviewToolStripMenuItem.Enabled = enable;
            printToolStripButton.Enabled = enable;

            zoomInButton.Enabled = enable && scale > 0;
            zoomOutButton.Enabled = enable;

            zoomInToolStripMenuItem.Enabled = enable && scale > 0;
            zoomOutToolStripMenuItem.Enabled = enable;
            zoomToWindowToolStripMenuItem.Enabled = enable;

            dpiSelect.Enabled = enable;
        }

        private static string SizeText(Image im)
        {
            return SizeText(im.Size, im.HorizontalResolution, im.VerticalResolution);
        }

        private static string SizeText(Size sz, float horResolution, float verResolution)
        {
            return String.Format(R.SizeFmt, Math.Round(sz.Width / horResolution * 2.54, 2), Math.Round(sz.Height / verResolution * 2.54, 2));
        }

        #region print
        private void OnPrintClick(object sender, EventArgs e)
        {
            PrePageSetup();
            if (pageSetupDlg.ShowDialog() == DialogResult.OK)
            {
                SetPrintAreaText();

                var ps = pageSetupDlg.PrinterSettings;

                NumPages = printDocument.GetNumPages(pageSetupDlg.PageSettings);
                ps.MinimumPage = 1;
                ps.MaximumPage = NumPages;
                if (ps.FromPage < 1)
                    ps.FromPage = 1;
                if (ps.FromPage > NumPages)
                    ps.FromPage = NumPages;

                if (ps.ToPage < 1 || ps.ToPage > NumPages) 
                    ps.ToPage = NumPages;
                if (ps.ToPage < ps.FromPage)
                    ps.ToPage = ps.FromPage;

                printDlg.PrinterSettings = ps;
                printDlg.AllowCurrentPage = false;
                printDlg.AllowSomePages = true;
                
                var dialogRes = this.printDlg.ShowDialog();
                if (dialogRes == DialogResult.OK || dialogRes != DialogResult.Cancel)
                {
                    ps = printDlg.PrinterSettings; // todo а если сменился принтер и т.п.?
                    printDocument.PrinterSettings = ps;
                    printDocument.DefaultPageSettings = ps.PrinterName == pageSetupDlg.PrinterSettings.PrinterName ? pageSetupDlg.PageSettings : ps.DefaultPageSettings;
                    if (dialogRes == DialogResult.OK)
                        printDocument.Start();
                    else
                        SetPrintAreaText();
                }
            }

        }

        private void OnPageSetupClick(object sender, EventArgs e)
        {
            PrePageSetup();
            if (pageSetupDlg.ShowDialog() == DialogResult.OK)
            {
                printDocument.PrinterSettings = pageSetupDlg.PrinterSettings;
                printDocument.DefaultPageSettings = pageSetupDlg.PageSettings;

                SetPrintAreaText();
            }
        }

        private void PrePageSetup()
        {
            printDocument.OriginAtMargins = true;
            pageSetupDlg.Document = printDocument;

            var pm = MapPrintDocument.PhysicalMargins(pageSetupDlg.PageSettings);
            pageSetupDlg.MinMargins = new Margins(pm.Width, pm.Width, pm.Height, pm.Height);
        }

        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            bool SimulatePrint = false;
            if (SimulatePrint)
                e.Cancel = true;

            var grph = e.Graphics;

            grph.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            grph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
            grph.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            grph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;

            //grph.PageUnit = GraphicsUnit.Pixel;

            e.HasMorePages = printDocument.PrintNextPage(grph, e.PageSettings);
        }

        #endregion

        #region dpi choose
        private void SetupResolutions(float dpi)
        {
            var list = new List<string>(new[] { "100", "150", "200", "300", "600", "1200" });

            string s = DpiText(dpi);
            int i = list.IndexOf(s);
            if (i >= 0)
                list[i] = list[i] + R.NativeMark;
            else
                list.Insert(0, s + R.NativeMark);

            dpiSelect.Items.Clear();
            dpiSelect.Items.AddRange(list.ToArray());

            //dpiSelect.ComboBox.FormatString = "G:4.2";

            SetDpiText(dpi);
        }

        private void dpiSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDpi();
            panel1.Focus();
        }

        private void dpiSelect_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            UpdateDpi();
            panel1.Focus();
        }

        private void dpiSelect_TextChanged(object sender, EventArgs e)
        {
            int i = 6;
        }

        private void dpiSelect_Leave(object sender, EventArgs e)
        {
            float dpi;
            if (TryGetDpi(out dpi))
                UpdateDpi();
        }

        private void dpiSelect_Validating(object sender, CancelEventArgs e)
        {
            float dpi;
            e.Cancel = !TryGetDpi(out dpi);
        }

        private void dpiSelect_Validated(object sender, EventArgs e)
        {
            UpdateDpi();
        }

        private bool TryGetDpi(out float res)
        {
            res = -1;
            var s = dpiSelect.Text;
            if (s != null && s.EndsWith(R.NativeMark))
                s = s.Replace(R.NativeMark, "");
            
            return !String.IsNullOrEmpty(s) && (float.TryParse(s, out res) || float.TryParse(s.Replace('.', ','), out res)) && res > 0;
        }
        private void UpdateDpi()
        {
            float dpi;
            if (!TryGetDpi(out dpi))
            {
                ShowError(R.ErrorParsingDpi, String.Format(R.NotAFloatFmt, dpiSelect.Text));
                return;
            }

            printDocument.SetDpi(dpi);
            SetDpiText(dpi);
        }

        private void SetDpiText(float dpi)
        {
            var txt = DpiText(dpi);
            if (txt != dpiSelect.Text)
                dpiSelect.Text = txt;

            sizeLabel.Text = String.Format("dpi, {0}", SizeText(printDocument.Image));
            SetPrintAreaText();
        }

        #endregion

        private static string DpiText(float dpi)
        {
            return String.Format("{0}", Math.Round(dpi, 2));
        }

        private void SetPrintAreaText()
        {
            var im = printDocument.Image;
            PageSize = MapPrintDocument.GetPageSize(pageSetupDlg.PageSettings ?? printDocument.DefaultPageSettings);
            NumPages = printDocument.GetNumPages(PageSize);

            var pa = printDocument.PrintArea;
            printAreaLabel.Text = String.Format(R.PrintAreaFmt, pa.X, pa.Y, pa.Width, pa.Height, NumPages);

            pictureBox1.Invalidate();
        }

        private void SelectionText()
        {
            var dr = ViewCoord2DocCoord(selection, ScaleK);
            var im = printDocument.Image;

            printAreaLabel.Text = String.Format(R.SelectionFmt, dr.X, dr.Y, dr.Width, dr.Height, SizeText(dr.Size, im.HorizontalResolution, im.VerticalResolution));
        } 

        private void SetPositionText(Point viewPos)
        {
            var k = ScaleK;
            positionLabel.Text = String.Format("X: {0}, Y:{1}", (int)(viewPos.X * k), (int)(viewPos.Y * k));
        }

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            panel1.Focus();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            var im = printDocument.Image;
            if (im == null)
                return;

            var k = ScaleK;
            var pen = new Pen(Color.Blue, 2);
            // pages

            var clipRec = new RectangleF(e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height);
            var bRect = new RectangleF(0, 0, pictureBox1.Width, pictureBox1.Height);
            Dictionary<int, RectangleF> rects = new Dictionary<int, RectangleF>();
            for (int i = 0, n = NumPages; i < n; i++)
            {
                var rf = DocCoord2ViewCoord(printDocument.GetSrcRect(i, PageSize), k);
                if (rf.IntersectsWith(clipRec))
                    rects.Add(i, RectangleF.Intersect(bRect, rf));
            }

            if (rects.Any())
            {
                e.Graphics.DrawRectangles(pen, rects.Values.ToArray());

                int ln;
                int chr;
                var sf = StringFormat.GenericDefault;

                using (var f = new Font("Sans serif", 15))
                    using (var brush = new SolidBrush(Color.Blue))
                        foreach (var key in rects.Keys)
                        {
                            var s = String.Format(R.PageFmt, key + 1);

                            e.Graphics.MeasureString(s, f, rects[key].Size, sf, out chr, out ln);
                            e.Graphics.DrawString(ln > 1 || chr < s.Length ? String.Format("{0}", key + 1) : s, f, brush, rects[key], sf);
                        }
            }
            

            // selection
            pen.Width = 1;
            pen.Color = Color.Black;
            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            if (selection != null && !selection.IsEmpty)
                e.Graphics.DrawRectangle(pen, selection);

            pen.Dispose();
        }

        #region select print Area
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            selection = Rectangle.Empty;
            start = e.Location;

            SelectionText();
            pictureBox1.Invalidate();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            SetPositionText(e.Location);
            if (start.IsEmpty)
                return;

            var old = selection;
            selection = RectFromPoints(start, e.Location);
                
            if (ScrollIfNeeded(e.Location))
            {
                SelectionText();
                pictureBox1.Invalidate();
                return;
            }

            Rectangle inv = Rectangle.Union(selection, old);
            inv.Inflate(2, 2);
            inv.Offset(-1, -1);

            SelectionText();
            pictureBox1.Invalidate(inv);
        }

        /// <returns>true if scroll happened, false otherwise</returns>
        private bool ScrollIfNeeded(Point location)
        {
            var mainSz = panel1.ClientSize;
            var loc = panel1.PointToClient(pictureBox1.PointToScreen(location));

            return SideAutoScroll(panel1.HorizontalScroll, start.X, mainSz.Width, loc.X, location.X) ||
                   SideAutoScroll(panel1.VerticalScroll, start.Y, mainSz.Height, loc.Y, location.Y);
        }

        private bool SideAutoScroll(ScrollProperties sp, int stPos, int size, int mousePos, int mousePosPic)
        {
            if (!sp.Visible)
                return false;

            const int limit = 20; 
            int d2 = size - mousePos; // расстояние до второй границы

            if (mousePos < limit && d2 > mousePos && (mousePosPic < stPos || mousePosPic - stPos > limit ))
                sp.Value = Math.Max(sp.Minimum, sp.Value - limit);
            else if (d2 < limit && d2 < mousePos && (mousePosPic > stPos  || stPos - mousePosPic > limit))
                sp.Value = Math.Min(sp.Maximum, sp.Value + limit);
            else
                return false;

            return true;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {            
            if (start.IsEmpty)
                return;

            selection = RectFromPoints(start, e.Location);
            // deselect on small
            printDocument.PrintArea = (selection.Height * selection.Width < 25) ? Rectangle.Empty : ViewCoord2DocCoord(selection, ScaleK);

            start = Point.Empty;
            selection = Rectangle.Empty;

            SetPrintAreaText(); // hidden invalidate
        }
        #endregion

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutBox())
                dlg.ShowDialog();
        }

    }
}
