using AirMedia.Platform.UI.ViewExt.QuickAction;
using Android.Graphics;
using Android.Views;

namespace AirMedia.Platform.UI.ViewUtils.QuickActionHelper
{
    public class PopupActionHelper
    {
        public enum Action
        {
            Play,
            PlayNext,
            AddToQueue,
            AddToPlaylist,
            Send
        }

        public void ShowAlbumItemMenu(View anchorView)
        {
            var res = App.Instance.Resources;
            var menu = new QuickAction(App.Instance, QuickActionLayout.Vertical);
            var icActionPlay = res.GetDrawable(Resource.Drawable.ic_action_play_light);
            var icActionPlayAfter = res.GetDrawable(Resource.Drawable.ic_action_play_after_light);
            var icActionQueue = res.GetDrawable(Resource.Drawable.ic_action_add_to_queue_light);
            var icActionAddToPlaylist = res.GetDrawable(Resource.Drawable.ic_action_add_to_playlist);
            var icActionSend = res.GetDrawable(Resource.Drawable.ic_action_share);

            menu.AddActionItem(new ActionItem((int) Action.Play,
                res.GetString(Resource.String.album_menu_action_play), icActionPlay));
            menu.AddActionItem(new ActionItem((int)Action.PlayNext,
                res.GetString(Resource.String.album_menu_action_play_after), icActionPlayAfter));
            menu.AddActionItem(new ActionItem((int) Action.AddToQueue,
                res.GetString(Resource.String.album_menu_action_add_to_queue), icActionQueue));
            menu.AddActionItem(new ActionItem((int)Action.AddToPlaylist,
                res.GetString(Resource.String.album_menu_action_add_to_playlist), icActionAddToPlaylist));
            menu.AddActionItem(new ActionItem((int)Action.Send,
                res.GetString(Resource.String.album_menu_action_send), icActionSend));

            menu.ActionItemClicked += OnMenuActionItemClicked;

            menu.Show(anchorView);
        }

        private void OnMenuActionItemClicked(object sender, ActionItemClickEventArgs args)
        {
            ((View) sender).SetBackgroundResource(Resource.Drawable.quickaction_action_item_selected);
            args.Source.ActionItemClicked -= OnMenuActionItemClicked;
        }
    }
}