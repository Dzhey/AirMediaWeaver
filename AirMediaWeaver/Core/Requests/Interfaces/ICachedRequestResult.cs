
namespace AirMedia.Core.Requests.Interfaces
{
    public interface ICachedRequestResult
    {
        byte[] Serialize();
        void Deserialize(byte[] data);
    }
}