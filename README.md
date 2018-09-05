# Unity3DTwitterGIFUploadSample
Unity3D sample code for GIF upload using chunked media/upload API of Twitter
Not polished, but I hope it helps everyone interested in knowing how to implement twitter GIF upload on Unity3D

## Usage
1. Download TwitterKit (https://github.com/twitter/twitter-kit-unity)
2. Attach TwitterKitInit(Included in TwitterKit SDK) and TwitterController to a GameObject
3. Call TwitterController::Login()
4. Call TwitterController::PostGIF()

### Note
```UTwitter/TwitterClient``` and ```UTwitter/TwitterRestClient``` are not dependent on TwitterKit, so if you can get authtoken by yourself, you can upload GIF without Twitterkit dependencies. If what all you need is Auth, you can pick the logic from TwitterRestClient (Learned a lot from LetsTweet on UnityAsset Store)
