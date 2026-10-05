
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IRuntime : ISTARNETHolon
    {
        public bool IsRunning { get; set; }
        public string StartCommand { get; set; }
        public string StopCommand { get; set; }
        public string BuildCommand { get; set; }
        public string PublishCommand { get; set; }
        public string InstallCommand { get; set; }
        public string DeployCommand { get; set; }
    }
}