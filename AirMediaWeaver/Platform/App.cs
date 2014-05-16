using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Impl;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql;
using AirMedia.Platform.Logger;
using Android.App;
using Android.OS;
using System;
using Android.Runtime;

namespace AirMedia.Platform
{
	[Application(Theme = "@style/AppTheme")]
	public sealed class App : Application
	{
	    public static string LogTag = typeof (App).Name;

		public static App Instance { get; private set; }
		public static Handler MainHandler { get; private set; }
        public static UserPreferences Preferences { get; private set; }
        public static WorkerRequestManager WorkerRequestManager { get; private set; }
        public static DatabaseHelper DatabaseHelper { get; private set; }

		public App(IntPtr handle, JniHandleOwnership transfer)
			: base(handle,transfer)
		{
		}

		public override void OnCreate ()
		{
			base.OnCreate ();

            AmwLog.Init(new AndroidAmwLog());

			MainHandler = new Handler();
            Instance = this;
            Preferences = new UserPreferences(this);
		    WorkerRequestManager = new WorkerRequestManager(this);
		    DatabaseHelper = new AndroidDatabaseHelper();
            DatabaseHelper.Init(DatabaseHelper);

		    var rq = new InitDatabaseRequest();
		    rq.Execute();
		}
    }
}

