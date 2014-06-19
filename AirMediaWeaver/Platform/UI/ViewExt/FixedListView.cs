using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace AirMedia.Platform.UI.ViewExt
{
    public class FixedListView : ListView
    {
        public FixedListView(Context context) : base(context)
        {
        }

        public FixedListView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public FixedListView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            // Do not use the highest two bits of Integer.MAX_VALUE because they are
            // reserved for the MeasureSpec mode
            var heightSpec = MeasureSpec.MakeMeasureSpec(int.MaxValue >> 2, MeasureSpecMode.AtMost);
            base.OnMeasure(widthMeasureSpec, heightSpec);
            LayoutParameters.Height = MeasuredHeight;
        }
    }
}