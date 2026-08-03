using System.IO;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Collections;

public class CustsceneController : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;

    void Start()
    {
        var data = PlayerSessionData.Instance;
        string name = data.pendingCutsceneName.Value.ToString();
        if (!string.IsNullOrEmpty(name))
        {
            PlayVideo(name);
        }
        else
        {
            data.pendingCutsceneName.OnValueChanged += OnCutsceneNameReceived;
        }
    }

void OnCutsceneNameReceived(FixedString32Bytes oldVal, FixedString32Bytes newVal)
{
    PlayerSessionData.Instance.pendingCutsceneName.OnValueChanged -= OnCutsceneNameReceived;
    PlayVideo(newVal.ToString());
}


    public void PlayVideo(string videoName)
    {
        string videoPath = Path.Combine(Application.streamingAssetsPath, videoName);
        videoPlayer.url = videoPath;
        videoPlayer.loopPointReached += HandleFinished;
        videoPlayer.Play();
    }

    void HandleFinished(VideoPlayer source)
    {
        videoPlayer.loopPointReached -= HandleFinished;
        // 播放结束，进入游戏
        NetworkManager.Singleton.SceneManager.LoadScene("Level1 New", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}