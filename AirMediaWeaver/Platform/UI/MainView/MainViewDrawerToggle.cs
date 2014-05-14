
using System;
using Android.App;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;

namespace AirMedia.Platform.UI.MainView
{
    public class MainViewDrawerToggle : ActionBarDrawerToggle
    {
        public event EventHandler<EventArgs> DrawerOpened;
        public event EventHandler<EventArgs> DrawerClosed;

        public MainViewDrawerToggle(Activity activity, DrawerLayout drawerLayout, 
            int drawerImageRes, int openDrawerContentDescRes, int closeDrawerContentDescRes) 
            : base(activity, drawerLayout, drawerImageRes, 
            openDrawerContentDescRes, closeDrawerContentDescRes)
        {
        }

        public override void OnDrawerOpened(View drawerView)
        {
            base.OnDrawerOpened(drawerView);

            if (DrawerOpened != null) DrawerOpened(this, EventArgs.Empty);
        }

        public override void OnDrawerClosed(View drawerView)
        {
            base.OnDrawerClosed(drawerView);

            if (DrawerClosed != null) DrawerClosed(this, EventArgs.Empty);
        }
    }
}