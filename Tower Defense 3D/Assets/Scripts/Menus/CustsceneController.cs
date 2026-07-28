using System.IO;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CustsceneController : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;

    void Start()
    {
        PlayVideo(PlayerSessionData.Instance.pendingCutsceneName.Value.ToString());
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
        NetworkManager.Singleton.SceneManager.LoadScene("Level1", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}