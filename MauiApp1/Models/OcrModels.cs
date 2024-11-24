using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiApp1.Models
{
    public class TextExtractionModel
    {
        public TextExtractionModel()
        {
            LinesList = new List<LineWithXY>();
        }

        public float CenterY
        {
            get
            {
                float avg = 0;
                if (LinesList != null && LinesList.Count > 0)
                {
                    avg = LinesList.Sum(x => x.Y) / LinesList.Count;
                }
                return avg;
            }
        }
        public List<LineWithXY> LinesList { get; set; }
    }

    public class LineWithXY
    {
        public LineWithXY(int x, int y, string text)
        {
            X = x;
            Y = y;
            Text = text;
        }
        public int X { get; set; }
        public int Y { get; set; }
        public string Text { get; set; }
    }
}
