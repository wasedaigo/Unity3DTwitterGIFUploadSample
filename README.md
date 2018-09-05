# Unity3DTwitterGIFUploadSample
A sample code to upload GIF using chunked media/upload API.
Not polished, but I hope it helps everyone interested in knowing how to implement twitter GIF upload on Unity3D

## Usage
1. Download TwitterKit (https://github.com/twitter/twitter-kit-unity)
2. Attach TwitterKitInit and TwitterController to a GameObject
3. Call TwitterController::Login()
4. Call TwitterController::PostGIF()

### Note
```UTwitter/TwitterClient``` and ```UTwitter/TwitterRestClient``` are not dependent on TwitterKit, so if you can get authtoken by yourself, you can upload GIF without Twitterkit dependencies
