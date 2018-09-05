using System;
using System.IO;
using TwitterKit.Unity.Settings;
using TwitterKit.Unity;
using UnityEngine;
using UTwitter;
 
public class TwitterController : MonoBehaviour
{
    private TwitterClient _twitterClient;
    void Start() {
        _twitterClient = new TwitterClient(this, TwitterSettings.ConsumerKey, TwitterSettings.ConsumerSecret);
    }

    public void Login(System.Action<Exception> callback)
    {
        TwitterKit.Unity.Twitter.LogIn (
            (TwitterSession session) =>
            {
                _twitterClient.SetAuthToken(session.authToken.token, session.authToken.secret);
            },
            (ApiError error) =>
            {
                callback(new Exception(error.message));
            }
        );
    }

    public void PostGIF(string message, string gifPath, System.Action<Exception> callback)
    {
        var data = File.ReadAllBytes(gifPath);
        _twitterClient.PostGIF(message, data, (e)=>{
            if (e != null) {
                Debug.LogError(e);
            }
        });
    }

    public void PostTweet(string message)
    {
        _twitterClient.PostTweet(message, 0, (e, r) => {
            if (e != null) {
                Debug.LogError(e);
                return;
            }
            Debug.Log("Tweet Successful!");
        });
    }

    public void PostGIF() {
        var bytes = File.ReadAllBytes(Const.GifFilepath);
        _twitterClient.PostGIF(bytes, (e)=> {
            if (e != null) {
                Debug.LogError(e);
            }
        });
    }
}