using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIResponder : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
    }

    public void OnMute(bool muted)
    {
        AudioListener.volume = muted ? 0 : 1;
    }

    public void OnRestartClicked()
    {
        SceneManager.LoadScene("Battle");
    }
}
