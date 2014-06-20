using AirMedia.Core.Data.Sql;
using AirMedia.Core.Log;
using AirMedia.Core.Requests.Controller;
using AirMedia.Core.Requests.Impl;
using AirMedia.Core.Requests.Model;
using AirMedia.Platform.Controller;
using AirMedia.Platform.Controller.WebService;
using AirMedia.Platform.Data;
using AirMedia.Platform.Data.Sql;
using AirMedia.Platform.Logger;
using Android.App;
using Android.Content;
using Android.OS;
using System;
using Android.Runtime;
using AndroidDownloadManager = Android.App.DownloadManager;

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
        public static MemoryRequestResultCache MemoryRequestResultCache { get; private set; }

        private RequestResultListener _requestResultListener;

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
            RequestManager.Init(WorkerRequestManager);

            DatabaseHelper = new AndroidDatabaseHelper();
            DatabaseHelper.Init(DatabaseHelper);

            MemoryRequestResultCache = new MemoryRequestResultCache();

            _requestResultListener = new RequestResultListener("application_request_listener");
            _requestResultListener.RegisterResultHandler(typeof(InitDatabaseRequest), OnDatabaseInitialized);

            _requestResultListener.SubmitRequest(new InitDatabaseRequest());
        }

        private void OnDatabaseInitialized(object sender, ResultEventArgs args)
        {
            if (args.Request.Status != RequestStatus.Ok)
            {
                AmwLog.Error(LogTag, "Error inializing database!");
                return;
            }
            AmwLog.Verbose(LogTag, "database initialized successfully");

            var intent = new Intent(this, typeof (AirStreamerService));
            intent.SetAction(AirStreamerService.ActionStartHttp);
            StartService(intent);
        }
    }
}

