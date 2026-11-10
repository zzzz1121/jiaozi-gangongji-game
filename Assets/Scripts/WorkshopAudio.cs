using System;
using UnityEngine;

public sealed class WorkshopAudio : MonoBehaviour
{
    Graybox game;
    AudioSource music, effects;
    AudioClip tone;
    public void Initialize(Graybox owner)
    {
        game = owner;
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        effects.playOnAwake = false;
        const int rate = 22050;
        float[] bed = new float[rate * 8];
        int[] notes =
        {
            0,
            4,
            7,
            12,
            7,
            4,
            2,
            7
        };
        for (int i = 0; i < bed.Length; i++)
        {
            float t = (float)i / rate;
            float local = t % 1;
            float hz = 220 * Mathf.Pow(2, notes[(int)t] / 12f);
            bed[i] = Mathf.Sin(t * hz * Mathf.PI * 2) * .065f * Mathf.Sin(local * Mathf.PI) * Mathf.Exp(-local * 2);
        }

        var clip = AudioClip.Create("占位音乐", bed.Length, 1, rate, false);
        clip.SetData(bed, 0);
        music.clip = clip;
        music.Play();
        float[] ping = new float[rate / 12];
        for (int i = 0; i < ping.Length; i++)
            ping[i] = Mathf.Sin(i * 880f / rate * Mathf.PI * 2) * .2f * (1f - (float)i / ping.Length);
        tone = AudioClip.Create("占位操作音", ping.Length, 1, rate, false);
        tone.SetData(ping, 0);
        game.Rules.Recorded += OnRecord;
    }

    void OnRecord(string message)
    {
        if (message.StartsWith("take ") || message.StartsWith("place ") || message.StartsWith("delivered") || message.StartsWith("process_complete"))
            effects.PlayOneShot(tone);
    }

    public void Apply(Jiaozi.GameSettings settings)
    {
        if (music != null)
            music.volume = settings.music;
        if (effects != null)
            effects.volume = settings.effects;
    }

    void Update()
    {
        if (game.Flow != null && (game.Flow.Screen == GameScreen.Pause || game.Flow.Screen == GameScreen.Settings && game.Rules.Phase == Jiaozi.RoundPhase.Paused))
        {
            music.Pause();
            effects.Pause();
        }
        else
        {
            music.UnPause();
            effects.UnPause();
        }
    }

    void OnDestroy()
    {
        if (game != null)
            game.Rules.Recorded -= OnRecord;
    }
}
