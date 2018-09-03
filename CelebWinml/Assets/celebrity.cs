using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
namespace CelebWin
{
    
    public sealed class celebrityInput
    {
        public ImageFeatureValue data; // BitmapPixelFormat: Bgra8, BitmapAlphaMode: Premultiplied, width: 227, height: 227
    }
    
    public sealed class celebrityOutput
    {
        public TensorString classLabel; // shape(-1,1)
        public IList<Dictionary<string,float>> loss;
    }
    
    public sealed class celebrityModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<celebrityModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            celebrityModel learningModel = new celebrityModel();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<celebrityOutput> EvaluateAsync(celebrityInput input)
        {
            binding.Bind("data", input.data);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new celebrityOutput();
            output.classLabel = result.Outputs["classLabel"] as TensorString;
            output.loss = result.Outputs["loss"] as IList<Dictionary<string,float>>;
            return output;
        }
    }
}
