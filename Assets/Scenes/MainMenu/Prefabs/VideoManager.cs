using UnityEngine;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private GameObject _endGameCanvas;

    void Start()
    {
        if (_endGameCanvas != null)
            _endGameCanvas.SetActive(false);

        if (_videoPlayer != null)
            _videoPlayer.loopPointReached += OnVideoEnd;
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        if (_endGameCanvas != null)
        {
            _endGameCanvas.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void Update()
    {
        if (Input.anyKeyDown && _videoPlayer.isPlaying)
        {
            _videoPlayer.Stop();
            OnVideoEnd(_videoPlayer);
        }
    }

    void OnDestroy()
    {
        if (_videoPlayer != null)
            _videoPlayer.loopPointReached -= OnVideoEnd;
    }
}