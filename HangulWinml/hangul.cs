using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
namespace HangulWinml
{
    
    public sealed class hangulInput
    {
        public TensorFloat input00; // shape(1,4096)
    }
    
    public sealed class hangulOutput
    {
        public TensorFloat output00; // shape(1,2350)
    }
    
    public sealed class hangulModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;
        public static async Task<hangulModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            hangulModel learningModel = new hangulModel();
            learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);
            return learningModel;
        }
        public async Task<hangulOutput> EvaluateAsync(hangulInput input)
        {
            binding.Bind("input:0", input.input00);
            var result = await session.EvaluateAsync(binding, "0");
            var output = new hangulOutput();
            output.output00 = result.Outputs["output:0"] as TensorFloat;
            return output;
        }
    }
}
