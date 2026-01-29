using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.IO;
using System;
using DG.Tweening; 

public enum CloudBrainButton
{
    None,
    ButtonSouth, // A / Cross
    ButtonEast,  // B / Circle
    ButtonWest,  // X / Square
    ButtonNorth, // Y / Triangle
    LeftShoulder,
    RightShoulder
}

public class BobCloudBrain : MonoBehaviour
{
    [Header("--- OpenAI 配置 ---")]
    public string apiKey = "sk-xxxxxxxxxxxxxxxxxxxxxxxx"; 
    [TextArea] public string prompt = "Bob, Weather, Map, Music, Home, Cancel, Setting, 打开地图, 天气, 音乐, 设置";

    [Header("--- 调试 ---")]
    public bool saveDebugWav = false; 

    [Header("--- 引用 ---")]
    public BobController bob;
    public GlobalDirector globalDirector; 
    
    [Header("--- 🎮 输入控制 ---")]
    [Tooltip("键盘触发键 (默认 F)")]
    public Key keyboardKey = Key.F;
    
    [Tooltip("手柄触发键 (默认 ButtonSouth = A键)")]
    public CloudBrainButton gamepadButton = CloudBrainButton.ButtonSouth;

    [Tooltip("需要长按多久才开始录音？")]
    public float holdDuration = 2.0f;

    [Header("--- 💡 视觉反馈微调 ---")]
    [Tooltip("预热时的最大亮度/转速 (0-1)。\n觉得转太快就把这个调小，比如 0.4")]
    [Range(0, 1)] public float warmupIntensity = 0.4f; 

    // 内部状态
    private string _defaultDevice; 
    private AudioClip _recordingClip;
    private bool _isRecording = false; 
    private bool _isPressing = false;  
    private float _pressTimer = 0f;    

    [System.Serializable] class OpenAIResponse { public string text; }

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            _defaultDevice = Microphone.devices[0];
            Debug.Log($"🎤 Unity 已连接系统默认麦克风: [{_defaultDevice}]");
        }
        else
        {
            Debug.LogError("❌ 严重错误：Windows 没有找到任何录音设备！");
        }
    }

    void Update()
    {
        bool kPressed = Keyboard.current != null && Keyboard.current[keyboardKey].isPressed;
        bool gPressed = CheckGamepadInput(gamepadButton);
        
        bool isInputActive = kPressed || gPressed;

        if (isInputActive)
        {
            if (!_isPressing)
            {
                _isPressing = true;
                _pressTimer = 0f;
                if(bob) bob.masterVolume = 0.1f; // 起始亮度
            }

            if (!_isRecording)
            {
                _pressTimer += Time.deltaTime;
                
                // 控制旋转速度：从 0.1 过渡到 warmupIntensity
                float progress = Mathf.Clamp01(_pressTimer / holdDuration);
                if(bob) bob.masterVolume = Mathf.Lerp(0.1f, warmupIntensity, progress);

                if (_pressTimer >= holdDuration)
                {
                    StartRecording();
                }
            }
        }
        else
        {
            if (_isPressing)
            {
                _isPressing = false;
                
                if (_isRecording)
                {
                    StopAndSend();
                }
                else
                {
                    Debug.Log("⚠️ 按键时间太短，取消录音");
                    if(bob) bob.masterVolume = 0f;
                }
                _pressTimer = 0f;
            }
        }
    }

    bool CheckGamepadInput(CloudBrainButton btn)
    {
        var gp = Gamepad.current;
        if (gp == null || btn == CloudBrainButton.None) return false;
        
        switch (btn)
        {
            case CloudBrainButton.ButtonSouth: return gp.buttonSouth.isPressed;
            case CloudBrainButton.ButtonEast: return gp.buttonEast.isPressed;
            case CloudBrainButton.ButtonWest: return gp.buttonWest.isPressed;
            case CloudBrainButton.ButtonNorth: return gp.buttonNorth.isPressed;
            case CloudBrainButton.LeftShoulder: return gp.leftShoulder.isPressed;
            case CloudBrainButton.RightShoulder: return gp.rightShoulder.isPressed;
            default: return false;
        }
    }

    void StartRecording()
    {
        if (_defaultDevice == null) return;

        _isRecording = true;
        
        // 录音开始瞬间，稍微亮一点
        if(bob) 
        {
            bob.masterVolume = Mathf.Min(warmupIntensity + 0.2f, 1.0f);
            bob.TriggerAction(BobController.BobActionType.PulseLight); 
        }
        
        _recordingClip = Microphone.Start(_defaultDevice, false, 10, 44100);
        Debug.Log("🔴 长按达成！录音开始...");
    }

    void StopAndSend()
    {
        _isRecording = false;
        
        int position = Microphone.GetPosition(_defaultDevice);
        Microphone.End(_defaultDevice);
        
        Debug.Log($"✋ 录音结束 (采样长度: {position})");
        
        if(bob) bob.masterVolume = warmupIntensity; // 保持预热亮度等待

        if (position < 1000) 
        {
            if(bob) bob.masterVolume = 0f;
            return;
        }

        StartCoroutine(SendToOpenAI(_recordingClip, position));
    }

    IEnumerator SendToOpenAI(AudioClip clip, int endPos)
    {
        // 🔥 这里调用了下面的 WavUtilityNative
        byte[] wavData = WavUtilityNative.ConvertToWavAndBoost(clip, endPos, saveDebugWav);

        if (wavData == null)
        {
            Debug.LogWarning("🔇 录音无效，忽略");
            if(bob) bob.masterVolume = 0f;
            yield break;
        }

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormFileSection("file", wavData, "audio.wav", "audio/wav"));
        formData.Add(new MultipartFormDataSection("model", "whisper-1"));
        formData.Add(new MultipartFormDataSection("prompt", prompt));

        string url = "https://api.openai.com/v1/audio/transcriptions";
        UnityWebRequest request = UnityWebRequest.Post(url, formData);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        Debug.Log("📤 发送给 OpenAI...");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            if (json.Contains("text")) 
            {
                OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(json);
                string cleanText = response.text.ToLower().Trim().Replace("。", "").Replace(",", "");

                if (cleanText.Length < 2)
                {
                    Debug.LogWarning($"⚠️ 忽略无效语音: {response.text}");
                }
                else
                {
                    Debug.Log($"<color=green>🧠 识别成功: [{cleanText}]</color>");
                    if(globalDirector) globalDirector.ProcessVoiceCommand(cleanText);
                    
                    if(bob) bob.TriggerAction(BobController.BobActionType.JumpHigh);
                }
            }
        }
        else 
        { 
            Debug.LogError($"❌ API 请求失败: {request.error}"); 
        }
        
        if(bob) bob.masterVolume = 0f; 
    }
}

// ==========================================
// 🔥 必须包含这个工具类，否则会报错
// ==========================================
public static class WavUtilityNative
{
    public static byte[] ConvertToWavAndBoost(AudioClip clip, int sampleCount, bool saveDebug)
    {
        if (clip == null || sampleCount <= 0) return null;
        float[] samples = new float[sampleCount * clip.channels];
        clip.GetData(samples, 0);
        float maxVol = 0f;
        foreach (var s in samples) if (Mathf.Abs(s) > maxVol) maxVol = Mathf.Abs(s);
        if (maxVol < 0.02f) return null;
        float multiplier = 0.95f / maxVol;
        float gateThreshold = 0.05f; 
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((ushort)(clip.channels * 2));
            writer.Write((ushort)16);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            writer.Write(samples.Length * 2);
            foreach (var sample in samples)
            {
                float amplified = sample * multiplier;
                if (Mathf.Abs(amplified) < gateThreshold) amplified = 0;
                short intData = (short)(Mathf.Clamp(amplified, -1f, 1f) * 32767);
                writer.Write(intData);
            }
            return stream.ToArray();
        }
    }
}