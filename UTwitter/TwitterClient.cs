using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace UTwitter
{
    public class TwitterClient
    {
        public enum MediaType
        {
            GIF
        }
        const string PostTweetURL = "https://api.twitter.com/1.1/statuses/update.json";
        const string UploadMediaURL = "https://upload.twitter.com/1.1/media/upload.json";
        public static Dictionary<MediaType, string> MediaTypePaths = new Dictionary<MediaType, string>() {
            {MediaType.GIF, "image/gif"}
        };

        public struct MediaUploadImage
        {
            [SerializeField]
            public string image_type;
            [SerializeField]
            public int w;
            [SerializeField]
            public int h;
        }

        public class MediaUploadInitResponse
        {
            [SerializeField]
            public long media_id;
            [SerializeField]
            public int size;
            [SerializeField]
            public int expires_after_secs;
            [SerializeField]
            public MediaUploadImage image_type;
        }

        public class MediaUploadAppendResponse
        {
            // Empty per spec
        }

        public struct MediaUploadProcessingInfo
        {
            [SerializeField]
            public string state;
            [SerializeField]
            public int check_after_secs; // check after 5 seconds for update using STATUS command
        }
        public class MediaUploadFinalizeResponse
        {
            [SerializeField]
            public long media_id;
            [SerializeField]
            public string media_id_string;
            [SerializeField]
            public int size;
            [SerializeField]
            public int expires_after_secs;
            [SerializeField]
            public MediaUploadImage image_type;
        }

        public class PostTweetResponse
        {
            // TODO
        }
        private TwitterRestClient _restClient;
        private MonoBehaviour _monobehaviour;

        public TwitterClient(MonoBehaviour monobehaviour, string consumerKey, string consumerSecret) {
            _monobehaviour = monobehaviour;
            _restClient = new TwitterRestClient(monobehaviour, consumerKey, consumerSecret);
        }

        public bool Authenticated
        {
            get
            {
                return _restClient.Authenticated;
            }
        }

        public void SetAuthToken(string authToken, string authSecret)
        {
            _restClient.SetAuthToken(authToken, authSecret);
        }

        public void PostGIF(string message, byte[] gifData, System.Action<Exception> callback)
        {
            PostMediaUploadInit(gifData, MediaType.GIF, (e1, r1) =>
            {
                if (e1 != null)
                {
                    Debug.LogError(string.Format("PostMediaUploadInit {0}", e1));
                    callback(e1);
                    return;
                }
                PostMediaUploadAppend(r1.media_id, gifData, (e2, r2) =>
                {
                    if (e2 != null)
                    {
                        Debug.LogError(string.Format("PostMediaUploadAppend {0}", e2));
                        callback(e2);
                        return;
                    }
                    PostMediaUploadFinalize(r1.media_id, (e3, r3) =>
                    {
                        if (e3 != null)
                        {
                            Debug.LogError(string.Format("PostMediaUploadFinalize {0}", e3));
                            callback(e3);
                            return;
                        }
                        PostTweet(message, r1.media_id, (e4, r4) =>
                        {
                            if (e4 != null)
                            {
                                Debug.LogError(string.Format("PostTweet {0}", e4));
                                callback(e4);
                                return;
                            }
                            callback(null);
                        });
                    });
                });
            });
        }

        public void PostMediaUploadInit(byte[] data, MediaType mediaType, System.Action<Exception, MediaUploadInitResponse> callback)
        {
            Dictionary<string, string> authParameters = new Dictionary<string, string>();
            authParameters.Add("command", "INIT");
            authParameters.Add("total_bytes", data.Length.ToString());
            authParameters.Add("media_type", MediaTypePaths[mediaType]);

            // Add data to the form to post.
            WWWForm form = new WWWForm();
            form.AddField("command", "INIT");
            form.AddField("total_bytes", data.Length.ToString());
            form.AddField("media_type", MediaTypePaths[mediaType]);

            Debug.Log(string.Format("PostMediaUploadInit command={0} total_bytes={1} media_type={2}", authParameters["command"], authParameters["total_bytes"], authParameters["media_type"]));

            UnityWebRequest request = UnityWebRequest.Post(UploadMediaURL, form);
            request.SetRequestHeader("Authorization", _restClient.GetHeader("POST", UploadMediaURL, authParameters));
            _restClient.Post(request, (err, responseText) =>
            {
                MediaUploadInitResponse res = null;
                if (err == null)
                {
                    res = JsonUtility.FromJson<MediaUploadInitResponse>(responseText);
                }
                callback(err, res);
            });
        }

        public void PostMediaUploadAppend(long mediaID, byte[] data, System.Action<Exception, MediaUploadAppendResponse> callback)
        {
            // For multi-part request, no need to add auth parameters
            Dictionary<string, string> authParameters = new Dictionary<string, string>();

            var form = new WWWForm();
            form.AddField("command", "APPEND");
            form.AddField("media_id", mediaID.ToString());
            form.AddField("segment_index", 0);
            form.AddBinaryData("media", data);

            Debug.Log(string.Format("PostMediaUploadAppend command=APPEND media_id={0} total_bytes={1}", mediaID, data.Length));

            UnityWebRequest request = UnityWebRequest.Post(UploadMediaURL, form);
            request.SetRequestHeader("Authorization", _restClient.GetHeader("POST", UploadMediaURL, authParameters));
            request.SetRequestHeader("ContentType", "multipart/form-data");
            _restClient.Post(request, (err, responseText) => 
            {
                MediaUploadAppendResponse res = null;
                if (err == null)
                {
                    res = JsonUtility.FromJson<MediaUploadAppendResponse>(responseText);
                }
                callback(err, res);
            });
        }

        public void PostMediaUploadFinalize(long mediaID, System.Action<Exception, MediaUploadFinalizeResponse> callback)
        {
            Dictionary<string, string> authParameters = new Dictionary<string, string>();
            authParameters.Add("command", "FINALIZE");
            authParameters.Add("media_id", mediaID.ToString());

            var form = new WWWForm();
            form.AddField("command", "FINALIZE");
            form.AddField("media_id", mediaID.ToString());

            UnityWebRequest request = UnityWebRequest.Post(UploadMediaURL, form);
            request.SetRequestHeader("Authorization", _restClient.GetHeader("POST", UploadMediaURL, authParameters));
            request.SetRequestHeader("ContentType", "application/x-www-form-urlencoded");
            _restClient.Post(request, (err, responseText) =>
            {
                MediaUploadFinalizeResponse res = null;
                if (err == null)
                {
                    res = JsonUtility.FromJson<MediaUploadFinalizeResponse>(responseText);
                }
                callback(err, res);
            });
        }

        public void PostTweet(string message, long mediaID, System.Action<Exception, PostTweetResponse> callback)
        {
            Dictionary<string, string> authParameters = new Dictionary<string, string>();
            authParameters.Add("status", message);
            if (mediaID > 0) {
                authParameters.Add("media_ids", mediaID.ToString());
            }

            // Add data to the form to post.
            WWWForm form = new WWWForm();
            form.AddField("status", message);
            if (mediaID > 0)
            {
                form.AddField("media_ids", mediaID.ToString());
            }

            UnityWebRequest request = UnityWebRequest.Post(PostTweetURL, form);
            request.SetRequestHeader("Authorization", _restClient.GetHeader("POST", PostTweetURL, authParameters));
            _restClient.Post(request, (err, responseText) =>
            {
                PostTweetResponse res = null;
                if (err == null)
                {
                    res = JsonUtility.FromJson<PostTweetResponse>(responseText);
                }
                callback(err, res);
            });
        }

    }
}
