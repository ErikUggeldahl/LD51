using System.Collections;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    const float TIME_PERIOD = 10f;

    [SerializeField]
    AudioClip tech;

    [SerializeField]
    AudioClip classical;

    [SerializeField]
    AudioSource musicSource;

    [SerializeField]
    Transform timerBar;

    [SerializeField]
    TMP_Text timerText;
    RectTransform timerTextTransform;

    Coroutine activeSequence;

    bool active = false;
    bool forever = false;
    public bool Active
    {
        get { return active; }
        private set
        {
            active = value;
            Time.timeScale = value ? 1f : 0f;
        }
    }

    public void SetForever(bool forever)
    {
        this.forever = forever;
    }

    void Start()
    {
        timerTextTransform = timerText.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) || (forever && !Active))
        {
            if (activeSequence != null) StopCoroutine(activeSequence);
            activeSequence = StartCoroutine(StartSequence());
        }
    }

    public void SetClassical(bool isClassical)
    {
        var position = musicSource.timeSamples;
        var playing = musicSource.isPlaying;
        if (playing)
        {
            musicSource.timeSamples = position;
            musicSource.Stop();
        }

        musicSource.clip = isClassical ? classical : tech;

        if (playing)
        {
            musicSource.Play();
        }
    }

    IEnumerator StartSequence()
    {
        Active = true;

        musicSource.timeSamples = 0;
        musicSource.Play();
        var timer = TIME_PERIOD;
        while (timer > 0f)
        {
            yield return null;
            timer -= Time.deltaTime;

            var fraction = Mathf.Clamp01(timer / TIME_PERIOD);

            timerBar.localScale = new Vector3(fraction, 1f, 1f);

            var newTextPosition = timerTextTransform.anchoredPosition;
            newTextPosition.x = 500f * fraction - 250f;
            timerTextTransform.anchoredPosition = newTextPosition;

            timerText.text = Mathf.Ceil(fraction * TIME_PERIOD).ToString();
        }

        Active = false;
    }
}
