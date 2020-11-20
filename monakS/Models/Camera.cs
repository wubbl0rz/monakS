namespace monakS.Models
{
  public class Camera
  {
    public int Id { get; set; }
    public string Name { get; set; }
    public string StreamUrl { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public bool IsObjectDetectionEnabled { get; set; } = true;
  }
}