using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.AI.MachineLearning;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CelebWinml
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private celebrityModel modelGen = new celebrityModel();
        private celebrityInput celebInput = new celebrityInput();
        private celebrityOutput celebOutput = new celebrityOutput();

        public MainPage()
        {
            this.InitializeComponent();
            LoadModel();
        }

        private async void LoadModel()
        {
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/celebrity.onnx"));
            modelGen = await celebrityModel.CreateFromStreamAsync(modelFile as IRandomAccessStreamReference);
           
        }

        private LearningModelDeviceKind GetDeviceKind()
        {
            return LearningModelDeviceKind.Default;
        }

        public async Task<VideoFrame> GetCropedImage(VideoFrame inputVideoFrame)
        {
            bool useDX = inputVideoFrame.SoftwareBitmap == null;

            BitmapBounds cropBounds = new BitmapBounds();
            uint h = 227;
            uint w = 227;
            var frameHeight = useDX ? inputVideoFrame.Direct3DSurface.Description.Height : inputVideoFrame.SoftwareBitmap.PixelHeight;
            var frameWidth = useDX ? inputVideoFrame.Direct3DSurface.Description.Width : inputVideoFrame.SoftwareBitmap.PixelWidth;

            var requiredAR = ((float)227 / 227);
            w = Math.Min((uint)(requiredAR * frameHeight), (uint)frameWidth);
            h = Math.Min((uint)(frameWidth / requiredAR), (uint)frameHeight);
            cropBounds.X = (uint)((frameWidth - w) / 2);
            cropBounds.Y = 0;
            cropBounds.Width = w;
            cropBounds.Height = h;

            var cropped_vf = new VideoFrame(BitmapPixelFormat.Bgra8, 227, 227, BitmapAlphaMode.Premultiplied);

            await inputVideoFrame.CopyToAsync(cropped_vf, cropBounds, null);

            return cropped_vf;
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                txtFilePath.Text = file.Path;
                SoftwareBitmap bitmap;
                using (var s = await file.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(s);
                    bitmap = await decoder.GetSoftwareBitmapAsync();
                    bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                var images = new SoftwareBitmapSource();
                await images.SetBitmapAsync(bitmap);
                imgCeleb.Source = images;

                //VideoFrame
                VideoFrame inputimage = VideoFrame.CreateWithSoftwareBitmap(bitmap);
                inputimage = await GetCropedImage(inputimage);
                celebInput.data = ImageFeatureValue.CreateFromVideoFrame(inputimage);
                celebOutput = await modelGen.EvaluateAsync(celebInput);

                var resultVector = celebOutput.classLabel.GetAsVectorView();
                txtcelebName.Text = resultVector[0];
                sw.Stop();
                txtProcTime.Text = $"{sw.Elapsed}";
                Debug.WriteLine($"process time = {sw.Elapsed}");
            }
            else
            {
                txtFilePath.Text = "Operation cancelled.";
            }
        }
    }
}
