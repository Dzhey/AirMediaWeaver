

namespace AirMedia.Core.Requests.Model
{
    public class IntermediateResultUpdateData : UpdateData
    {
        public RequestResult RequestResult { get; set; }

        public IntermediateResultUpdateData()
            : base(UpdateData.UpdateCodeIntermediateResultObtained)
        {
        }

        public IntermediateResultUpdateData(int updateCode)
            : base(updateCode)
        {
        }
    }
}