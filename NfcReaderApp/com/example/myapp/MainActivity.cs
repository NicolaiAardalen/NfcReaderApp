using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Content;
using com.example.myapp;
using Android.Widget;
using Java.Lang;
using NfcReaderApp;

namespace com.example.myapp
{
    [Activity(Label = "NFC Chip", MainLauncher = true)]

    public class MainActivity : Activity
    {
        private NfcReader _nfcReader;
        public TextView _textViewUri;
        public TextView _textViewText;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            _nfcReader = new NfcReader(this);
            _textViewUri = FindViewById<TextView>(Resource.Id.textViewUri);
            _textViewText = FindViewById<TextView>(Resource.Id.textViewText);
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Enable NFC foreground dispatch
            _nfcReader.EnableNfcForegroundDispatch(this);
        }

        protected override void OnPause()
        {
            base.OnPause();

            // Disable NFC foreground dispatch when the app goes to the background
            //_nfcReader.DisableNfcForegroundDispatch(this);
        }

        [System.Obsolete]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        protected override void OnNewIntent(Intent intent)
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        {
            base.OnNewIntent(intent);

            // Handle NFC intent
            _nfcReader.HandleNfcIntent(intent);
        }

        private void DisplayUriInUI(string uri)
        {
            RunOnUiThread(() =>
            {
                _textViewUri.Text = uri;
            });
        }

        private void DisplayTextInUI(string text)
        {
            RunOnUiThread(() =>
            {
                _textViewText.Text = text;
            });
        }

        // You can add other lifecycle methods and UI event handlers here
    }
}