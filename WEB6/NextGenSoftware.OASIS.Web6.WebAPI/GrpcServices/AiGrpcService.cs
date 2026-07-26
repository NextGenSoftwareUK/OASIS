using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class AiGrpcService : AiService.AiServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        public override async Task<CompleteReply> Complete(CompleteRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var completionRequest = BuildCompletionRequest(request, avatarId);
                var manager = new AIProviderManager(avatarId, DNA);
                var result = await manager.CompleteAsync(completionRequest);
                if (result.IsError)
                    return new CompleteReply { IsError = true, Message = result.Message };
                return new CompleteReply
                {
                    IsError = false,
                    Provider = result.Result?.Provider ?? "",
                    Model = result.Result?.Model ?? "",
                    Content = result.Result?.Content ?? "",
                    PromptTokens = result.Result?.PromptTokens ?? 0,
                    CompletionTokens = result.Result?.CompletionTokens ?? 0,
                    EstimatedCostUsd = result.Result?.EstimatedCostUSD ?? 0
                };
            }
            catch (Exception ex)
            {
                return new CompleteReply { IsError = true, Message = ex.Message };
            }
        }

        public override Task<CompleteReply> CompleteToolResult(CompleteRequest request, ServerCallContext context)
            => Complete(request, context);

        public override async Task<EmbedReply> Embed(EmbedRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var embeddingRequest = new EmbeddingRequest
                {
                    Texts = request.Inputs.ToList(),
                    Provider = request.Provider,
                    Model = request.Model
                };
                var manager = new EmbeddingManager(avatarId, DNA);
                var result = await manager.EmbedAsync(embeddingRequest);
                if (result.IsError)
                    return new EmbedReply { IsError = true, Message = result.Message };
                var reply = new EmbedReply { IsError = false };
                if (result.Result?.Embeddings != null)
                {
                    foreach (var vec in result.Result.Embeddings)
                    {
                        var v = new EmbeddingVector();
                        if (vec != null)
                            v.Values.AddRange(vec.Select(f => (float)f));
                        reply.Embeddings.Add(v);
                    }
                }
                return reply;
            }
            catch (Exception ex)
            {
                return new EmbedReply { IsError = true, Message = ex.Message };
            }
        }

        public override Task<OpenServModelsReply> ListOpenServModels(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var reply = new OpenServModelsReply { IsError = false };
                foreach (var m in OpenServCatalog.Models)
                    reply.Models.Add(m?.Id ?? m?.ToString() ?? "");
                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new OpenServModelsReply { IsError = true, Message = ex.Message });
            }
        }

        public override Task<MLModelsReply> ListMLModels(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var reply = new MLModelsReply { IsError = false };
                reply.Models.Add(new MLModelInfo { Name = "task_classifier", Type = "MulticlassClassification", Status = "heuristic_fallback", Description = "Classifies FAHRN task type" });
                reply.Models.Add(new MLModelInfo { Name = "loop_anomaly", Type = "AnomalyDetection", Status = "heuristic_fallback", Description = "Scores FAHRN loop anomaly probability" });
                reply.Models.Add(new MLModelInfo { Name = "sentiment_analysis", Type = "Classification", Status = "heuristic_fallback", Description = "Classifies text sentiment" });
                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new MLModelsReply { IsError = true, Message = ex.Message });
            }
        }

        public override Task<ClassifyTaskReply> ClassifyTask(TextRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                    return Task.FromResult(new ClassifyTaskReply { IsError = true, Message = "text is required" });
                var manager = new MLNetManager(Guid.Empty, DNA);
                string taskType = manager.ClassifyTaskType(request.Text);
                return Task.FromResult(new ClassifyTaskReply { IsError = false, TaskType = taskType });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ClassifyTaskReply { IsError = true, Message = ex.Message });
            }
        }

        public override Task<SentimentReply> AnalyseSentiment(TextRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                    return Task.FromResult(new SentimentReply { IsError = true, Message = "text is required" });
                var manager = new MLNetManager(Guid.Empty, DNA);
                string sentiment = manager.AnalyseSentiment(request.Text);
                return Task.FromResult(new SentimentReply { IsError = false, Sentiment = sentiment });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new SentimentReply { IsError = true, Message = ex.Message });
            }
        }

        public override Task<BaseReply> TrainTaskClassifier(TrainRequest request, ServerCallContext context)
        {
            try
            {
                if (request.Samples == null || request.Samples.Count < 10)
                    return Task.FromResult(new BaseReply { IsError = true, Message = "At least 10 samples required" });
                var manager = new MLNetManager(Guid.Empty, DNA);
                var samples = request.Samples.Select(s => (s.Problem, s.TaskType)).ToList();
                var result = manager.TrainTaskClassifier(samples);
                return Task.FromResult(result.IsError
                    ? new BaseReply { IsError = true, Message = result.Message }
                    : new BaseReply { IsError = false, Message = "Model trained and saved" });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new BaseReply { IsError = true, Message = ex.Message });
            }
        }

        public override async Task<ImageGenReply> GenerateImage(ImageGenRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (!System.Enum.TryParse<NextGenSoftware.OASIS.Web6.Core.Enums.AIProviderType>(request.Provider, true, out var imageProviderType))
                    imageProviderType = NextGenSoftware.OASIS.Web6.Core.Enums.AIProviderType.StabilityAI;
                var imgRequest = new ImageGenerationRequest
                {
                    Prompt = request.Prompt,
                    Provider = imageProviderType,
                    Model = request.Model,
                    Size = (request.Width > 0 && request.Height > 0) ? $"{request.Width}x{request.Height}" : "1024x1024"
                };
                var manager = new AIProviderManager(avatarId, DNA);
                var result = await manager.GenerateImageAsync(imgRequest);
                if (result.IsError)
                    return new ImageGenReply { IsError = true, Message = result.Message };
                return new ImageGenReply
                {
                    IsError = false,
                    Url = "",
                    Base64Image = result.Result?.ImageBase64 ?? ""
                };
            }
            catch (Exception ex)
            {
                return new ImageGenReply { IsError = true, Message = ex.Message };
            }
        }

        private static CompletionRequest BuildCompletionRequest(CompleteRequest request, Guid avatarId)
        {
            var cr = new CompletionRequest
            {
                AvatarId = avatarId,
                Provider = request.Provider,
                Model = request.Model,
                UseFAHRN = request.UseFahrn ? true : (bool?)null,
                UseHolonicBraid = request.UseHolonicBraid ? true : (bool?)null,
                FahrnTaskType = request.FahrnTaskType,
                InjectAvatarContext = request.InjectAvatarContext ? true : (bool?)null,
                Messages = request.Messages.Select(m => new ChatMessage { Role = m.Role, Content = m.Content }).ToList()
            };
            return cr;
        }
    }
}
