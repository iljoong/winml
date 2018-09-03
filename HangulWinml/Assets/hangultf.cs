using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
namespace MNIST_Demo
{
    
    public sealed class hangultfInput
    {
        public float input00;
    }
    
    public sealed class hangultfOutput
    {
        public TensorFloat output00; // shape(1,2350)
    }
    
    public sealed class hangultfModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<hangultfModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            hangultfModel learningModel = new hangultfModel();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<hangultfOutput> EvaluateAsync(hangultfInput input)
        {
            binding.Bind("input:0", input.input00);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new hangultfOutput();
            output.output00 = result.Outputs["output00"] as TensorFloat;
            return output;
        }
    }
}
