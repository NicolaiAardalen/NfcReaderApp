using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Nfc;
using Java.Util;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Android.Util;

namespace com.example.myapp
{
    public class NfcReader
    {
        private MainActivity _activity;
        private NfcAdapter _nfcAdapter;

        public NfcReader(MainActivity activity)
        {
            _activity = activity;
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(_activity);
        }

        public void EnableNfcForegroundDispatch(Context context)
        {
            PendingIntent pendingIntent = PendingIntent.GetActivity(
            context,
            0,
            new Intent(context, typeof(MainActivity)).AddFlags(ActivityFlags.SingleTop),
            PendingIntentFlags.Immutable
        );

            _nfcAdapter.EnableForegroundDispatch(
                _activity,
                pendingIntent,
                null,
                null
            );
        }

        [Obsolete]
        public void HandleNfcIntent(Intent intent)
        {
            if (NfcAdapter.ActionNdefDiscovered.Equals(intent.Action))
            {
                Android.OS.IParcelable[] rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
                if (rawMessages == null || rawMessages.Length == 0)
                {
                    return;
                }

                NdefMessage[] messages = new NdefMessage[rawMessages.Length];
                for (int i = 0; i < rawMessages.Length; i++)
                {
                    if (rawMessages[i] is NdefMessage ndefMessage)
                    {
                        messages[i] = ndefMessage;
                    }
                    else
                    {
                        Log.Error("NFC", "Failed to cast raw message to NdefMessage");
                    }
                }

                ProcessNdefMessages(messages);
            }
        }

        static byte[] SerializeType(Type type)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(memoryStream, type);
                return memoryStream.ToArray();
            }
        }

        private void ProcessNdefMessages(NdefMessage[] messages)
        {
            foreach (NdefMessage message in messages)
            {
                NdefRecord[] records = message.GetRecords();
                foreach (NdefRecord record in records)
                {
                    byte[] type = SerializeType(record.GetType());
                    short tnf = record.Tnf;

                    if (tnf == NdefRecord.TnfWellKnown)
                    {
                        if (Arrays.Equals(type, NdefRecord.RtdText))
                        {
                            string textPayload = TextRecordHelper.DecodeTextRecord(record);
                            DisplayTextInUI(textPayload);
                        }
                        else if (Arrays.Equals(type, NdefRecord.RtdUri))
                        {
                            Uri uri = UriRecordHelper.DecodeUriRecord(record);
                            DisplayUriInUI(uri.ToString());
                        }
                    }
                    else if (tnf == NdefRecord.TnfMimeMedia)
                    {
                        string mimeType = Encoding.UTF8.GetString(type);
                        if (mimeType == "application/vnd.example.customtype")
                        {
                            byte[] payload = record.GetPayload();
                        }
                    }
                    else
                    {
                        Log.Warn("NFC", $"Unknown TNF value: {tnf}");
                    }
                }
            }
        }

        private void ProcessNdefRecord(NdefRecord record)
        {
            short tnf = record.Tnf;
            byte[] type = SerializeType(record.GetType());

            if (tnf == NdefRecord.TnfWellKnown)
            {
                if (Arrays.Equals(type, NdefRecord.RtdText))
                {
                    string textPayload = TextRecordHelper.DecodeTextRecord(record);
                    DisplayTextInUI(textPayload);
                }
                else if (Arrays.Equals(type, NdefRecord.RtdUri))
                {
                    Uri uri = UriRecordHelper.DecodeUriRecord(record);
                    DisplayUriInUI(uri.ToString());
                }
            }
            else if (tnf == NdefRecord.TnfMimeMedia)
            {
                string mimeType = Encoding.UTF8.GetString(type);
                if (mimeType == "application/vnd.example.customtype")
                {
                    byte[] payload = record.GetPayload();
                }
            }
            else
            {
                Log.Warn("NFC", $"Unknown TNF value: {tnf}");
            }
        }

        private string DecodeUriRecord(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            byte uriIdentifierCode = payload[0];
            string decodedUri = UriRecordHelper.DecodeUriPayload(uriIdentifierCode, payload);
            return decodedUri;
        }

        private string DecodeTextRecord(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            string decodedText = TextRecordHelper.DecodeTextPayload(payload);
            return decodedText;
        }

        private void DisplayUriInUI(string uri)
        {
            _activity.RunOnUiThread(() =>
            {
                _activity._textViewUri.Text = uri;
            });
        }

        private void DisplayTextInUI(string text)
        {
            _activity.RunOnUiThread(() =>
            {
                _activity._textViewText.Text = text;
            });
        }
    }
    public static class TextRecordHelper
    {
        public static string DecodeTextRecord(NdefRecord record)
        {
            byte[] payload = record.GetPayload();
            byte statusByte = (byte)(payload[0] & 0x80);
            int languageCodeLength = payload[0] & 0x3F;

            string text = Encoding.UTF8.GetString(payload, 1 + languageCodeLength, payload.Length - 1 - languageCodeLength);
            return text;
        }
        public static string DecodeTextPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            byte statusByte = payload[0];
            int languageCodeLength = statusByte & 0x3F;
            int textOffset = languageCodeLength + 1;

            string languageCode = Encoding.ASCII.GetString(payload, 1, languageCodeLength);
            string decodedText = Encoding.UTF8.GetString(payload, textOffset, payload.Length - textOffset);

            return decodedText;
        }
    }

    public static class UriRecordHelper
    {
        public static Uri DecodeUriRecord(NdefRecord record)
        {
            byte[] payload = record.GetPayload();
            byte uriIdentifierCode = (byte)(payload[0] & 0xFF);

            string prefix;
            switch (uriIdentifierCode)
            {
                case 0x00:
                    prefix = "http://www.";
                    break;
                case 0x01:
                    prefix = "https://www.";
                    break;
                case 0x02:
                    prefix = "http://";
                    break;
                case 0x03:
                    prefix = "https://";
                    break;
                default:
                    prefix = "Unknown URI: ";
                    break;
            }

            string uri = prefix + Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
            return new Uri(uri);
        }
        public static string DecodeUriPayload(byte identifierCode, byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            string uriPrefix = GetUriPrefix(identifierCode);
            string uriBody = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);

            string decodedUri = uriPrefix + uriBody;
            return decodedUri;
        }

        private static string GetUriPrefix(byte identifierCode)
        {
            switch (identifierCode)
            {
                case 0x00: return string.Empty; // No prefix, the URI is in its raw form
                case 0x01: return "http://www.";
                case 0x02: return "https://www.";
                case 0x03: return "http://";
                case 0x04: return "https://";
                // Add more cases for other identifier codes as needed
                default: return string.Empty;
            }
        }
    }
}