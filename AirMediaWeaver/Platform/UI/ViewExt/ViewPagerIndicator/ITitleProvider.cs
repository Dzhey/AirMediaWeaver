using System;

namespace ViewPagerIndicator
{
	/**
	 * A TitleProvider provides the title to display according to a view.
	 */
	public interface ITitleProvider
	{
		/**
	     * Returns the title of the view at position
	     * @param position
	     * @return
	     */
		String GetTitle (int position);
	}
}

