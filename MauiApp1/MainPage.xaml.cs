using MauiApp1.Models;
using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.OCR;
using System.Text;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        #region consts
        private const int PLUS_MINUS_VALUE_FOR_ROW = 15;
        private const int NEAREST_ROW_MERGE_VALUE = 20;
        private const int NEAREST_ROW_MERGE_VALUE_FOR_NARROW_LINES = 10;
        #endregion consts

        #region ctor
        private readonly IOcrService _ocrService;
        public MainPage(IOcrService ocrService)
        {
            InitializeComponent();
            _ocrService = ocrService;
        }
        #endregion ctor

        #region handlers
        private async void OnCapturePhotoClicked(object sender, EventArgs e)
        {
            var result = await MediaPicker.Default.CapturePhotoAsync();
            if (result != null)
            {
                await ExtractTextAsync(result);
            }
        }

        private async void OnPickPhotoClicked(object sender, EventArgs e)
        {
            var result = await MediaPicker.Default.PickPhotoAsync();
            if (result != null)
            {
                await ExtractTextAsync(result);
            }
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            //count++;

            //if (count == 1)
            //    CounterBtn.Text = $"Clicked {count} time";
            //else
            //    CounterBtn.Text = $"Clicked {count} times";

            //SemanticScreenReader.Announce(CounterBtn.Text);
        }
        #endregion handlers

        private async Task ExtractTextAsync(FileResult photo)
        {
            // Open a stream to the photo
            using var sourceStream = await photo.OpenReadAsync();

            // Create a byte array to hold the image data
            var imageData = new byte[sourceStream.Length];

            // Read the stream into the byte array
            await sourceStream.ReadAsync(imageData);

            // Process the image data using the OCR service
            var ocrResult = await _ocrService.RecognizeTextAsync(imageData);
            ProcessOcrResults(ocrResult);
        }

        private void ProcessOcrResults(OcrResult ocrResult)
        {
            lblOcrResults.Text = ProcessText(ocrResult, narrowLinesSwitch.IsToggled);
            StringBuilder sb = new StringBuilder();
            foreach (var line in ocrResult.Lines)
            {
                sb.Append(line.ToString());
                sb.AppendLine();
            }
            lblOcrResultLines.Text = sb.ToString();
        }

        #region helpers methods
        private string ProcessText(OcrResult ocrResult, bool containNarrowLines = false)
        {
            List<TextExtractionModel> textExtractions = new List<TextExtractionModel>();
            foreach (var element in ocrResult.Elements)
            {
                var line = new LineWithXY(element.X, element.Y, element.Text);
                if (textExtractions.Count == 0)
                {
                    textExtractions.Add(new TextExtractionModel
                    {
                        LinesList = new List<LineWithXY> { line }
                    });
                }
                else
                {
                    var nearest = GetNearest(textExtractions, element.Y);
                    if (element.Y >= nearest.CenterY - PLUS_MINUS_VALUE_FOR_ROW && element.Y <= nearest.CenterY + PLUS_MINUS_VALUE_FOR_ROW)
                    {
                        textExtractions.FirstOrDefault(x => x.CenterY == nearest.CenterY).LinesList.Add(line);
                    }
                    else
                    {
                        if (textExtractions.Any(x => x.CenterY == element.Y))
                        {
                            textExtractions.FirstOrDefault(x => x.CenterY == element.Y).LinesList.Add(line);
                        }
                        else
                        {
                            textExtractions.Add(new TextExtractionModel
                            {
                                LinesList = new List<LineWithXY> { line }
                            });
                        }
                    }
                }
            }
            textExtractions = MergeNearestRows(textExtractions, containNarrowLines);
            StringBuilder finalLines = new StringBuilder();
            foreach (var item in textExtractions.OrderBy(x => x.CenterY))
            {
                finalLines.Append($"{string.Join(' ', item.LinesList.OrderBy(x => x.X).Select(x => x.Text).ToList())}↩\n");
            }
            return finalLines.ToString();
        }

        private TextExtractionModel GetNearest(List<TextExtractionModel> textExtractions, int currentKey)
        {
            var sorted = textExtractions.OrderBy(x => x.CenterY).ToList();
            TextExtractionModel last = null;
            foreach (var item in sorted)
            {
                var less = currentKey < item.CenterY;
                if (less)
                {
                    last = item;
                }
                else
                {
                    if (last == null)
                        return item;
                    var lessDiff = currentKey - last.CenterY;
                    var greaterDiff = item.CenterY - currentKey;
                    if (lessDiff < greaterDiff)
                        return last;
                    else
                        return item;
                }
            }
            return last;
        }

        private List<TextExtractionModel> MergeNearestRows(List<TextExtractionModel> textExtractions, bool containNarrowLines)
        {
            textExtractions = textExtractions.OrderBy(x => x.CenterY).ToList();
            List<TextExtractionModel> toRemove = new List<TextExtractionModel>();
            TextExtractionModel last = null;
            foreach (var current in textExtractions)
            {
                if (last == null)
                {
                    last = current;
                    continue;
                }
                var diff = current.CenterY - last.CenterY;
                if (diff <= (containNarrowLines ? NEAREST_ROW_MERGE_VALUE_FOR_NARROW_LINES : NEAREST_ROW_MERGE_VALUE))
                {
                    current.LinesList.AddRange(last.LinesList);
                    toRemove.Add(last);
                }
                last = current;
            }
            foreach (var item in toRemove)
            {
                textExtractions.Remove(item);
            }
            return textExtractions;
        }
        #endregion helpers methods
    }

}
