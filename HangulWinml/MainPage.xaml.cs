using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.AI.MachineLearning;
using Windows.Media;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;

namespace HangulWinml
{
    public sealed partial class MainPage : Page
    {
        private hangulModel charModel = null;
        private hangulInput charInput = new hangulInput();
        private hangulOutput charOuput = new hangulOutput();

        private IList<string> charLabel;

        //private LearningModelSession    session;
        private Helper helper = new Helper();
        RenderTargetBitmap renderBitmap = new RenderTargetBitmap();

        public MainPage()
        {
            this.InitializeComponent();

            // Set supported inking device types.
            inkCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
            inkCanvas.InkPresenter.UpdateDefaultDrawingAttributes(
                new Windows.UI.Input.Inking.InkDrawingAttributes()
                {
                    Color = Windows.UI.Colors.White,
                    Size = new Size(16, 16),
                    IgnorePressure = true,
                    IgnoreTilt = true,
                }
            );
            LoadModel();
        }

        private async void LoadModel()
        {
            //Load a machine learning model
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/hangul.onnx"));
            charModel = await hangulModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);

            StorageFile lableFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/2350-common-hangul.txt"));
            charLabel = await FileIO.ReadLinesAsync(lableFile);
        }

        private async void recognizeButton_Click(object sender, RoutedEventArgs e)
        {
            //Bind model input with contents from InkCanvas
            VideoFrame inputimage = await helper.GetHandWrittenImage(inkGrid);

            var source = new SoftwareBitmapSource();
            await source.SetBitmapAsync(inputimage.SoftwareBitmap);
            imgChar.Source = source;

            PredictHangul(inputimage);
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.InkPresenter.StrokeContainer.Clear();
            numberLabel.Text = "";
            topLabel.Text = "";
        }

        private async void PredictHangul(VideoFrame inputimage)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // convert to bgr8
            SoftwareBitmap bitgray8 = SoftwareBitmap.Convert(inputimage.SoftwareBitmap, BitmapPixelFormat.Gray8);
            var buff = new byte[64 * 64];
            bitgray8.CopyToBuffer(buff.AsBuffer());
            var fbuff = new float[4096];
            for (int i = 0; i < 4096; i++)
                fbuff[i] = (float)buff[i] / 255;

            long[] shape = { 1, 4096 };
            charInput.input00 = TensorFloat.CreateFromArray(shape, fbuff);

            var dummy = new float[1];
            long[] dummy_shape = { };
            charInput.keep_prob = TensorFloat.CreateFromArray(dummy_shape, dummy);

            //Evaluate the model
            charOuput = await charModel.EvaluateAsync(charInput);

            //Convert output to datatype
            IReadOnlyList<float> VectorImage = charOuput.output00.GetAsVectorView();
            IList<float> ImageList = VectorImage.ToList();

            //Display top results
            var topPred = ImageList.Select((value, index) => new { index, value })
                .ToDictionary(pair => pair.index, pair => pair.value)
                .OrderByDescending(key => key.Value)
                .ToArray();

            string topLabeltxt = "";
            for (int i = 1; i < 6; i++)
            {
                var item = topPred[i];
                Debug.WriteLine($"{item.Key}, {item.Value}, {charLabel[item.Key]}");
                topLabeltxt += $"{charLabel[item.Key]} ";
            }

            numberLabel.Text = charLabel[topPred[0].Key];
            topLabel.Text = topLabeltxt;

            Debug.WriteLine($"process time = {sw.Elapsed}");
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("Jpg", new List<string>() { ".jpg", ".jpeg" });
            picker.DefaultFileExtension = ".jpg";
            picker.SuggestedFileName = "test";

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                VideoFrame inputimage = await helper.GetHandWrittenImage(inkGrid);
                SoftwareBitmapSaveToFile(inputimage.SoftwareBitmap, file);
            }
        }

        public async void SoftwareBitmapSaveToFile(SoftwareBitmap softwareBitmap, StorageFile file)
        {
            if (softwareBitmap != null && file != null)
            {
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    encoder.SetSoftwareBitmap(softwareBitmap);

                    await encoder.FlushAsync();
                }
            }
        }

        private async void loadButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                SoftwareBitmap bitmap;
                using (var s = await file.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(s);
                    bitmap = await decoder.GetSoftwareBitmapAsync();
                    bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var images = new SoftwareBitmapSource();
                await images.SetBitmapAsync(bitmap);
                imgChar.Source = images;

                //VideoFrame
                VideoFrame inputimage = VideoFrame.CreateWithSoftwareBitmap(bitmap);

                PredictHangul(inputimage);
            }
        }

        private async void modelButton_Click(object sender, RoutedEventArgs e)
        {
            // access `PictureLibrary` only
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".onnx");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                //Load a machine learning model
                StorageFile modelFile = await StorageFile.GetFileFromPathAsync(file.Path);
                charModel = await hangulModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
            }

        }
    }
}
