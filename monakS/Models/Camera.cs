using monakS.BackgroundServices;
using Newtonsoft.Json;

namespace monakS.Models
{
  public class Camera
  {
    public int Id { get; set; }
    public bool SetupMode { get; set; }
    public string Name { get; set; }
    public string StreamUrl { get; set; }
    public string User { get; set; }
    [JsonIgnore]
    public string Password { get; set; }
    public bool IsObjectDetectionEnabled { get; set; } = false;
  }
}