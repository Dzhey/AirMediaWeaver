using System;
using System.Collections.Generic;
using System.Linq;
using AirMedia.Core.Controller.PlaybackSource;
using AirMedia.Core.Data;
using AirMedia.Core.Log;

namespace AirMedia.Platform.Controller.PlaybackSource
{
    public class QueuePlaybackSource : IBasicPlaybackSource
    {
        private static readonly string LogTag = typeof (QueuePlaybackSource).Name;

        private readonly LinkedList<IBasicPlaybackSource> _queue;
        private int _currentQueuePosition;

        public QueuePlaybackSource()
        {
            _queue = new LinkedList<IBasicPlaybackSource>();
        }

        public void Reset()
        {
            _currentQueuePosition = 0;
            _queue.Clear();
        }

        public void EnqueueSource(IBasicPlaybackSource source)
        {
            _queue.AddLast(source);
        }

        public bool HasCurrent()
        {
            if (HasPlaybackSource(_currentQueuePosition))
            {
                var src = GetPlaybackSource(_currentQueuePosition);

                return src.HasCurrent();
            }

            return false;
        }

        public bool HasNext()
        {
            int i = _currentQueuePosition + 1;

            for (; HasPlaybackSource(i); i++)
            {
                var source = GetPlaybackSource(i);
                if (source.HasNext())
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasPrevious()
        {
            int i = _currentQueuePosition - 1;

            for (; HasPlaybackSource(i); i--)
            {
                var source = GetPlaybackSource(i);
                if (source.HasPrevious())
                {
                    return true;
                }
            }

            return false;
        }

        public bool MoveNext()
        {
            int i = _currentQueuePosition;
            for (; HasPlaybackSource(i); i++)
            {
                var source = GetPlaybackSource(i);
                if (source.HasNext())
                {
                    source.MoveNext();
                    _currentQueuePosition = i;
                    AmwLog.Verbose(LogTag, string.Format("queue position is set to {0}", _currentQueuePosition));

                    return true;
                }
            }

            return false;
        }

        public bool MovePrevious()
        {
            int i = _currentQueuePosition;
            for (; HasPlaybackSource(i); i--)
            {
                var source = GetPlaybackSource(i);
                if (source.HasPrevious())
                {
                    source.MovePrevious();
                    _currentQueuePosition = i;
                    AmwLog.Verbose(LogTag, string.Format("queue position is set to {0}", _currentQueuePosition));

                    return true;
                }
            }

            return false;
        }

        public IBasicPlaybackSource GetCurrentPlaybackSource()
        {
            CheckQueuePositionBounds();

            return _queue.ElementAt(_currentQueuePosition);
        }

        public ResourceDescriptor? GetCurrentResource()
        {
            CheckQueuePositionBounds();

            var source = GetCurrentPlaybackSource();

            return source.GetCurrentResource();
        }

        private IBasicPlaybackSource GetPlaybackSource(int position)
        {
            return _queue.ElementAt(position);
        }

        private bool HasPlaybackSource(int position)
        {
            return _queue.Count > 0 && position < _queue.Count && position >= 0;
        }

        private void CheckQueuePositionBounds()
        {
            if (_currentQueuePosition < 0)
            {
                throw new ApplicationException(string.Format(
                    "unexpected queue position value ({0})", _currentQueuePosition));
            }

            if (_currentQueuePosition >= _queue.Count)
            {
                throw new ApplicationException("current queue position is beyond bounds");
            }
        }
    }
}