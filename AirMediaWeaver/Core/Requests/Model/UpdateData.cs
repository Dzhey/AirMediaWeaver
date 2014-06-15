using System;

namespace AirMedia.Core.Requests.Model
{
    public class UpdateData
    {
        public const int UpdateCodeIntermediateResultObtained = 1000;
        public const int UpdateCodeCachedResultRetrieved = 1100;

        public int UpdateCode
        {
            get
            {
                if (_updateCode.HasValue == false)
                {
                    throw new InvalidOperationException("update code is undefined");
                }

                return _updateCode.Value;
            }

            private set
            {
                if (_updateCode.HasValue)
                {
                    throw new InvalidOperationException("update code is already defined");
                }

                _updateCode = value;
            }
        }

        private int? _updateCode;

        public UpdateData()
        {
        }

        public UpdateData(int updateCode)
        {
            UpdateCode = updateCode;
        }
    }
}
