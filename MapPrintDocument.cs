using System;
using System.Drawing;
using System.Drawing.Printing;

using System.Text;

namespace printPoster
{
    class MapPrintDocument : PrintDocument
    {
        Bitmap image;
        int curPage;

        private Rectangle m_printArea;
        
        public void Load(string fileName, string title = null)
        {
            image = (Bitmap)Bitmap.FromFile(fileName);
            DocumentName = String.IsNullOrEmpty(title) ? fileName : title;
            PrintArea = Rectangle.Empty;
        }

        public void SetImage(Bitmap im)
        {
            image = im;
        }

        public Image Image { get { return image; } }

        public void SetDpi(float dpi)
        {
            image.SetResolution(dpi, dpi);
        }

        public Rectangle PrintArea
        {
            get { return m_printArea; }

            set
            {
                var allImage = new Rectangle(new Point(0, 0), image.Size);
                m_printArea = !value.IsEmpty ? Rectangle.Intersect(allImage, value) : allImage;
            }
        }

        public void Start()
        {
            curPage = 0;
            Print();
        }

        public bool PrintNextPage(Graphics grph, PageSettings sett)
        {
            var ps = sett.PrinterSettings;
            int shift = ps.PrintRange == PrintRange.SomePages ? ps.FromPage - ps.MinimumPage : 0;

            var sz = GetPageSize(sett);

            var srcRect = GetSrcRect(curPage + shift, sz);
            //grph.TranslateTransform(-sett.Margins.Left, -sett.Margins.Top);
            grph.DrawImage(image, 0, 0, srcRect, GraphicsUnit.Pixel);

#if DEBUG
            var s = String.Format("name: {0}, vdpi: {1}, range: {2}, page: {3}, total: {4}\n", DocumentName, image.VerticalResolution, ps.PrintRange, curPage + shift, GetNumPages(sett)) +
                    String.Format("compositing mode: {0}, CompQual: {1}, px offset mode: {2}, interpol mode: {3}", grph.CompositingMode, grph.CompositingQuality, grph.PixelOffsetMode, grph.InterpolationMode);

            grph.PageUnit = GraphicsUnit.Pixel;
            using (var font = new Font("Sans serif", 10))
            using (var brush = new SolidBrush(Color.Red))
            {
                // grph.DrawRectangle(new Pen(Color.Red, 1), 0, 0, InchHdthToPx(sz.Width, grph.DpiX) - 1, InchHdthToPx(sz.Height, grph.DpiY) -1);
                // grph.DrawRectangle(new Pen(Color.Green, 1), -1, -1, InchHdthToPx(sz.Width, grph.DpiX) + 1, InchHdthToPx(sz.Height, grph.DpiY) + 1);
                // grph.DrawRectangle(new Pen(Color.White, 1), -2, -2, InchHdthToPx(sz.Width, grph.DpiX) + 3, InchHdthToPx(sz.Height, grph.DpiY) + 3);
                // grph.DrawRectangle(new Pen(Color.Red, 1), 0, 0, sz.Width - 1, sz.Height);
                // grph.DrawString(s, font, brush, 0, 0);
            }
#endif

            curPage++;
            return HasMorePages(sett, sz);
        }

        public bool HasMorePages(PageSettings sett, Size pageSize)
        {
            switch (sett.PrinterSettings.PrintRange)
            {
                case PrintRange.AllPages:
                    return curPage < GetNumPages(pageSize);
                case PrintRange.SomePages:
                    return curPage + sett.PrinterSettings.FromPage <= sett.PrinterSettings.ToPage;
            }

            throw new NotImplementedException("Methods other than All pages and Some pages are not implemented yet");
        }

        private static float pxToInchHdth(int px, float dpi)
        {
            return 100.0f * px / dpi;
        }

        private static int InchHdthToPx(int iHdth, float dpi)
        {
            return (int)(iHdth * dpi / 100);
        }

        public static Size PhysicalMargins(PageSettings sett)
        {
            return new Size((int)Math.Ceiling(sett.HardMarginX), (int) Math.Ceiling(sett.HardMarginY));
/*            return new Size((int)Math.Ceiling(sett.Landscape ? sett.HardMarginY : sett.HardMarginX),
                            (int)Math.Ceiling(sett.Landscape ? sett.HardMarginX : sett.HardMarginY));
                            */
        }

        // почему-то тормозит, лишний раз не звать, кешируя значения
        public static Size GetPageSize(PageSettings sett) // hdth of inch
        {
            var bnds = sett.Bounds;
            var m = sett.Margins;

            var ps = PhysicalMargins(sett);
            var mrgns = new Margins(Math.Max(m.Left, ps.Width), Math.Max(m.Right, ps.Width), Math.Max(m.Top, ps.Height), Math.Max(m.Bottom, ps.Height));

            var pageWidth = bnds.Width - mrgns.Left - mrgns.Right;
            var pageHeight = bnds.Height - mrgns.Top - mrgns.Bottom;

            var pa = sett.PrintableArea;
            if (sett.Landscape)
                pa = new RectangleF(pa.Y, pa.X, pa.Height, pa.Width);            

            return new Size(Math.Min(pageWidth, (int)Math.Floor(pa.Width)), Math.Min(pageHeight, (int)Math.Floor(pa.Height)));
        }


        public int GetNumPages(PageSettings sett)
        {
            return sett != null ? GetNumPages(GetPageSize(sett)) : 0;
        }

        public int GetNumPages(Size pageSz)
        {
            if (pageSz.IsEmpty)
                return 0;

            float w = pxToInchHdth(PrintArea.Width, image.HorizontalResolution);
            float h = pxToInchHdth(PrintArea.Height, image.VerticalResolution);

            var cols = Math.Ceiling(w / pageSz.Width);
            var rows = Math.Ceiling(h / pageSz.Height);

            return (int)(cols * rows);
        }

        public Rectangle GetSrcRect(int page, Size pageSz)
        {
            Size pxSize = new Size(InchHdthToPx(pageSz.Width, image.VerticalResolution),
                                   InchHdthToPx(pageSz.Height, image.HorizontalResolution));

            int nCol = (PrintArea.Width + pxSize.Width - 1) / pxSize.Width;

            int row = page / nCol;
            int col = page % nCol;

            int x = col * pxSize.Width;
            int y = row * pxSize.Height;

            return new Rectangle(x + PrintArea.X, y + PrintArea.Y,
                x + pxSize.Width < PrintArea.Width ? pxSize.Width : PrintArea.Width - x,
                y + pxSize.Height < PrintArea.Height ? pxSize.Height : PrintArea.Height - y);
        }
    }

    public class TestMapPrintDocument
    {
        public StringBuilder sb = new StringBuilder();


        public void Test()
        {
            Bitmap bm = new Bitmap(2000, 1000);

            bm.SetResolution(150, 150);

            var pd = new MapPrintDocument();
            pd.SetImage(bm);
            pd.PrintArea = new Rectangle(300, 0, 1700, 1000);

            var pageSize = new Size(900, 1300);
            int n = pd.GetNumPages(pageSize);
            sb.AppendLine("pages: " + n.ToString());

            for (int i = 0; i < n; i++)
            {
                var r = pd.GetSrcRect(i, pageSize);
                sb.AppendLine("page " + i.ToString() + ": " + r.ToString());
            }
        }

        public string Log { get { return sb.ToString();  } }
        public void ClearLog()
        {
            sb = new StringBuilder();
        }
    }
}
