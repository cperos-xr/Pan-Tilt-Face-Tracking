/* ======================================================================
   MANUAL WEBRTC PAIRING CHECKLIST (Unity ↔ Unity, No Signaling)

   Run TWO Unity apps:
     - One = Sender
     - One = Receiver

   --------------------------------------------------
   SENDER (Create Offer)
   --------------------------------------------------
   1) Click  : Create (Send)
   2) Copy   : Local SDP Output   -> OFFER
   3) Copy   : Local ICE Output   -> save for later

   --------------------------------------------------
   RECEIVER (Accept Offer → Create Answer)
   --------------------------------------------------
   4) Click  : Join (Receive)
   5) Paste  : Sender OFFER  -> Remote SDP
   6) Click  : Set Remote SDP
   7) Copy   : Local SDP Output   -> ANSWER
   8) Paste  : Sender ICE    -> Remote ICE
   9) Click  : Add ICE

   --------------------------------------------------
   SENDER (Accept Answer)
   --------------------------------------------------
   10) Paste : Receiver ANSWER -> Remote SDP
   11) Click : Set Remote SDP
   12) Paste : Receiver ICE    -> Remote ICE
   13) Click : Add ICE

   --------------------------------------------------
   SUCCESS INDICATORS
   --------------------------------------------------
   - Logs show:
       "Peer connection state: Connected"
       "ICE connection state: Connected / Completed"
   - Receiver displays live video in Remote RawImage

   --------------------------------------------------
   COMMON MISTAKES
   --------------------------------------------------
   - ❌ Copy ICE from Console (use Local ICE Output UI)
   - ❌ Forget ICE in one direction (ICE must go both ways)
   - ❌ Paste SDP but forget to click Set Remote SDP

   --------------------------------------------------
   MENTAL MODEL
   --------------------------------------------------
     Offer  -> Receiver
     Answer -> Sender
     ICE    -> Both directions
   ====================================================================== */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleWebRTCPair : MonoBehaviour
{
    public enum Role { None, Sender, Receiver }

    [Header("UI - Inputs")]
    [Tooltip("Future signaling connect code (NOT used in this version).")]
    public TMP_InputField connectCodeInput;

    [Tooltip("Paste the remote SDP here (offer or answer). Prefer JSON produced by this script.")]
    public TMP_InputField remoteSdpInput; // multiline
    [Tooltip("Paste one or more ICE candidates here (one per line). JSON per line recommended.")]
    public TMP_InputField remoteIceInput; // multiline

    [Header("UI - Outputs")]
    [Tooltip("Local SDP to copy/paste to the other peer (offer/answer).")]
    public TMP_InputField localSdpOutput; // multiline
    [Tooltip("Local ICE candidates gathered (one per line) to copy/paste.")]
    public TMP_InputField localIceOutput; // multiline
    private string allLocalIceOutput = "";

    [Header("UI - Video")]
    public RawImage localVideoImage;
    public RawImage remoteVideoImage;

    [Header("UI - Buttons (assign in Inspector; code wires onClick)")]
    public Button createSendButton;
    public Button joinReceiveButton;
    public Button setRemoteSdpButton;
    public Button addIceButton;
    public Button cleanupButton;

    [Header("WebRTC Settings")]
    public bool useStun = true;
    public string stunUrl = "stun:stun.l.google.com:19302";
    public bool verboseLogging = true;

    public Role CurrentRole { get; private set; } = Role.None;

    private RTCPeerConnection _pc;

    // Sender capture
    private WebCamTexture _webcamTex;
    private VideoStreamTrack _localVideoTrack;

    // Receiver display
    private VideoStreamTrack _remoteVideoTrack;
    private Texture _remoteTexture;
    private int _remoteFrameCount;

    private Coroutine _webrtcUpdateCoroutine;

    // Static Event/Delegate instance to manage subscription/unsubscription
    public delegate void OnSDPCreated(string desc);
    public static event OnSDPCreated SdpCreated;

    public delegate void OnICECreated(string desc);
    public static event OnICECreated IceCreated;

    public delegate void OnRead();
    public static event OnRead ReadSdp;
    


    // IMPORTANT: Use Unity.WebRTC.OnVideoReceived (NOT Action<Texture>) in your package line
    private OnVideoReceived _onRemoteVideoReceivedHandler;

    [Serializable]
    private struct SdpJson
    {
        public string type; // "offer" or "answer"
        public string sdp;
    }

    [Serializable]
    private struct IceJson
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public string usernameFragment; // optional
    }

    private void Awake()
    {
        UnityMainThreadDispatcher.EnsureExists();
    }

    void OnEnable()
    {
        ConnectionManager.ConnectionDataCompleted += OnConnectionDataCompleted;
    }

    void OnDisable()
    {
        ConnectionManager.ConnectionDataCompleted -= OnConnectionDataCompleted;
    }

    private void OnConnectionDataCompleted(ConnectionDataSet connectionData)
    {
        SetRemoteSdpRoutine(connectionData.SdpData);
        SetRemoteIceCandidates(connectionData.IceData);
    }

    private void Start()
    {
        // Optional on older package lines; newer ones removed these APIs.
        TryInvokeWebRTCStatic("Initialize");

        // WebRTC event pump
        _webrtcUpdateCoroutine = StartCoroutine(WebRTC.Update());
        Log("WebRTC update coroutine started.");

        WireButtons();
        ClearUiOutputs();
    }

    private void OnDestroy()
    {
        try { CleanupPeer(); }
        catch (Exception e) { Debug.LogWarning($"[SimpleWebRTCPair] Cleanup exception: {e}"); }

        if (_webrtcUpdateCoroutine != null)
        {
            StopCoroutine(_webrtcUpdateCoroutine);
            _webrtcUpdateCoroutine = null;
        }

        TryInvokeWebRTCStatic("Dispose");
        Log("Destroyed.");
    }

    // =========================
    // Button wiring
    // =========================

    private void WireButtons()
    {
        // Clear existing listeners (avoid duplicates on domain reload / play)
        if (createSendButton != null)
        {
            createSendButton.onClick.RemoveAllListeners();
            createSendButton.onClick.AddListener(CreateSendConnection);
        }

        if (joinReceiveButton != null)
        {
            joinReceiveButton.onClick.RemoveAllListeners();
            joinReceiveButton.onClick.AddListener(JoinReceiveConnection);
        }

        if (setRemoteSdpButton != null)
        {
            setRemoteSdpButton.onClick.RemoveAllListeners();
            setRemoteSdpButton.onClick.AddListener(SetRemoteSdp);
        }

        if (addIceButton != null)
        {
            addIceButton.onClick.RemoveAllListeners();
            addIceButton.onClick.AddListener(AddRemoteIceCandidates);
        }

        if (cleanupButton != null)
        {
            cleanupButton.onClick.RemoveAllListeners();
            cleanupButton.onClick.AddListener(CleanupPeer);
        }

        Log("Buttons wired via code (onClick).");
    }

    // =========================
    // Button handlers
    // =========================

    public void CreateSendConnection()
    {
        if (CurrentRole != Role.None)
        {
            LogWarning("Already initialized. CleanupPeer() first if you want to restart.");
            return;
        }

        CurrentRole = Role.Sender;
        Log("Role set to Sender.");
        ClearUiOutputs();

        StartCoroutine(SenderRoutine_CreateOffer());
    }

    public void JoinReceiveConnection()
    {
        if (CurrentRole != Role.None)
        {
            LogWarning("Already initialized. CleanupPeer() first if you want to restart.");
            return;
        }

        CurrentRole = Role.Receiver;
        Log("Role set to Receiver.");
        ClearUiOutputs();

        EnsurePeerConnection();
        Log("Receiver ready. Paste remote OFFER into Remote SDP and click Set Remote SDP.");
        ReadSdp?.Invoke();
    }

    public void SetRemoteSdp()
    {
        if (_pc == null)
        {
            LogWarning("PeerConnection not created. Click Create (Send) or Join (Receive) first.");
            return;
        }

        string text = remoteSdpInput != null ? remoteSdpInput.text : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            LogWarning("Remote SDP input is empty.");
            return;
        }

        StartCoroutine(SetRemoteSdpRoutine(text));
    }

    public void AddRemoteIceCandidates()
    {
        if (_pc == null)
        {
            LogWarning("PeerConnection not created. Click Create (Send) or Join (Receive) first.");
            return;
        }

        string text = remoteIceInput != null ? remoteIceInput.text : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            LogWarning("Remote ICE input is empty.");
            return;
        }
        SetRemoteIceCandidates(text);
    }

    private void SetRemoteIceCandidates(string iceCandidates)
    {
        int added = 0, failed = 0;

        foreach (var line in SplitNonEmptyLines(iceCandidates))
        {
            if (!TryParseIce(line, out var ice))
            {
                failed++;
                LogWarning($"Failed to parse ICE: {Truncate(line, 140)}");
                continue;
            }

            var init = new RTCIceCandidateInit
            {
                candidate = ice.candidate,
                sdpMid = ice.sdpMid,
                sdpMLineIndex = ice.sdpMLineIndex
            };

            _pc.AddIceCandidate(new RTCIceCandidate(init));
            added++;

            if (verboseLogging)
                Log($"Added remote ICE: {Truncate(line, 140)}");
        }

        Log($"AddRemoteIceCandidates done. Added={added}, Failed={failed}");
    }

    // =========================
    // Sender / Receiver routines
    // =========================

    private IEnumerator SenderRoutine_CreateOffer()
    {
        EnsurePeerConnection();
        StartLocalWebcam();

        if (_webcamTex == null)
        {
            LogError("Cannot create offer: webcam failed to start.");
            yield break;
        }

        _localVideoTrack = new VideoStreamTrack(_webcamTex);
        _pc.AddTrack(_localVideoTrack);
        Log("Local webcam track added.");

        var offerOp = _pc.CreateOffer();
        yield return offerOp;

        if (offerOp.IsError)
        {
            LogError($"CreateOffer failed: {offerOp.Error.message}");
            yield break;
        }

        RTCSessionDescription offerDesc = offerOp.Desc;
        Log($"SDP offer created. type={offerDesc.type}");

        var setLocalOp = _pc.SetLocalDescription(ref offerDesc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            LogError($"SetLocalDescription(offer) failed: {setLocalOp.Error.message}");
            yield break;
        }

        Log("LocalDescription set (offer).");
        string offerDescString = SerializeSdpJson(offerDesc);
        SetInputFieldText(localSdpOutput, offerDescString);
        // Sender SDP created event invocation
        SdpCreated?.Invoke(offerDescString);

        Log("Local SDP (offer) ready for copy/paste.");
    }

    private IEnumerator SetRemoteSdpRoutine(string remoteText)
    {
        if (!TryParseSdp(remoteText, out var desc))
        {
            LogWarning("Remote SDP parse failed. Prefer JSON {type,sdp} output by this script.");
            yield break;
        }

        Log($"Setting remote SDP. type={desc.type}");

        var setRemoteOp = _pc.SetRemoteDescription(ref desc);
        yield return setRemoteOp;

        if (setRemoteOp.IsError)
        {
            LogError($"SetRemoteDescription failed: {setRemoteOp.Error.message}");
            yield break;
        }

        Log($"RemoteDescription set. SignalingState={_pc.SignalingState}");

        if (CurrentRole == Role.Receiver && desc.type == RTCSdpType.Offer)
        {
            Log("Receiver received OFFER. Creating ANSWER...");

            var answerOp = _pc.CreateAnswer();
            yield return answerOp;

            if (answerOp.IsError)
            {
                LogError($"CreateAnswer failed: {answerOp.Error.message}");
                yield break;
            }

            var answerDesc = answerOp.Desc;

            var setLocalOp = _pc.SetLocalDescription(ref answerDesc);
            yield return setLocalOp;

            if (setLocalOp.IsError)
            {
                LogError($"SetLocalDescription(answer) failed: {setLocalOp.Error.message}");
                yield break;
            }

            Log($"LocalDescription set (answer). SignalingState={_pc.SignalingState}");
            string answerDescString = SerializeSdpJson(answerDesc);
            SetInputFieldText(localSdpOutput, answerDescString);
            //Receiver SDP created event invocation
            SdpCreated?.Invoke(answerDescString);
            Log("Local SDP (answer) ready for copy/paste.");
        }
    }

    // =========================
    // PeerConnection setup / events
    // =========================

    private void EnsurePeerConnection()
    {
        if (_pc != null) return;

        var config = GetConfig();
        _pc = new RTCPeerConnection(ref config);

        _pc.OnIceCandidate = OnIceCandidate;
        _pc.OnIceConnectionChange = state => Log($"ICE connection state: {state}");
        _pc.OnIceGatheringStateChange = state =>
        {
            Log($"ICE gathering state: {state}");
            if (state == RTCIceGatheringState.Complete)
            {
                IceCreated?.Invoke(allLocalIceOutput);
            }
        };
        _pc.OnConnectionStateChange = state => Log($"Peer connection state: {state}");

        _pc.OnTrack = e =>
        {
            Log($"OnTrack fired. kind={e.Track.Kind}");

            if (e.Track is VideoStreamTrack v)
            {
                // Unsubscribe old
                if (_remoteVideoTrack != null && _onRemoteVideoReceivedHandler != null)
                {
                    try { _remoteVideoTrack.OnVideoReceived -= _onRemoteVideoReceivedHandler; }
                    catch { /* ignore */ }
                }

                _remoteVideoTrack = v;
                _remoteFrameCount = 0;

                // Create a handler of EXACT delegate type
                _onRemoteVideoReceivedHandler = OnRemoteVideoReceived;

                // Subscribe
                _remoteVideoTrack.OnVideoReceived += _onRemoteVideoReceivedHandler;

                Log("Remote VideoStreamTrack connected; waiting for frames...");
            }
        };

        Log("PeerConnection created.");
    }

    private void OnRemoteVideoReceived(Texture tex)
    {
        _remoteFrameCount++;

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            _remoteTexture = tex;
            if (remoteVideoImage != null)
            {
                remoteVideoImage.texture = _remoteTexture;
                remoteVideoImage.SetNativeSize();
            }
        });

        if (verboseLogging && (_remoteFrameCount == 1 || (_remoteFrameCount % 60 == 0)))
        {
            Log($"Remote video frame received. count={_remoteFrameCount}, tex={tex?.width}x{tex?.height}");
        }
    }

    private RTCConfiguration GetConfig()
    {
        var config = default(RTCConfiguration);

        if (useStun && !string.IsNullOrWhiteSpace(stunUrl))
        {
            config.iceServers = new[]
            {
                new RTCIceServer { urls = new[] { stunUrl } }
            };
            Log($"RTCConfiguration: STUN enabled ({stunUrl})");
        }
        else
        {
            config.iceServers = Array.Empty<RTCIceServer>();
            Log("RTCConfiguration: STUN disabled.");
        }

        return config;
    }

    private void OnIceCandidate(RTCIceCandidate cand)
    {
        if (cand == null) return;

        var json = SerializeIceJson(cand);

        if (verboseLogging)
            Log($"Local ICE gathered: {Truncate(json, 180)}");

        AppendInputFieldLine(localIceOutput, json);
        allLocalIceOutput += json + "\n";
    }

    // =========================
    // Webcam / local video
    // =========================

    private void StartLocalWebcam()
    {
        if (_webcamTex != null) return;

        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            LogError("No webcam devices found.");
            return;
        }

        var deviceName = devices[0].name;
        _webcamTex = new WebCamTexture(deviceName);
        _webcamTex.Play();

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            if (localVideoImage != null)
            {
                localVideoImage.texture = _webcamTex;
                localVideoImage.SetNativeSize();
            }
        });

        Log($"Webcam started: {deviceName}");
    }

    // =========================
    // Cleanup
    // =========================

    public void CleanupPeer()
    {
        Log("Cleaning up PeerConnection and media...");

        if (_remoteVideoTrack != null && _onRemoteVideoReceivedHandler != null)
        {
            try { _remoteVideoTrack.OnVideoReceived -= _onRemoteVideoReceivedHandler; }
            catch { /* ignore */ }
            _onRemoteVideoReceivedHandler = null;
        }

        if (_remoteVideoTrack != null)
        {
            _remoteVideoTrack.Dispose();
            _remoteVideoTrack = null;
        }

        if (_localVideoTrack != null)
        {
            _localVideoTrack.Dispose();
            _localVideoTrack = null;
        }

        if (_webcamTex != null)
        {
            if (_webcamTex.isPlaying) _webcamTex.Stop();
            _webcamTex = null;
        }

        if (_pc != null)
        {
            try { _pc.Close(); } catch { /* ignore */ }
            _pc.Dispose();
            _pc = null;
        }

        UnityMainThreadDispatcher.Enqueue(() =>
        {
            if (localVideoImage != null) localVideoImage.texture = null;
            if (remoteVideoImage != null) remoteVideoImage.texture = null;
        });

        ClearUiOutputs();
        CurrentRole = Role.None;
        Log("Cleanup complete. Role reset to None.");
    }

    // =========================
    // WebRTC version compatibility helpers
    // =========================

    private void TryInvokeWebRTCStatic(string methodName)
    {
        try
        {
            var t = typeof(WebRTC);
            var m0 = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (m0 != null)
            {
                m0.Invoke(null, null);
                Log($"Invoked WebRTC.{methodName}()");
                return;
            }

            if (verboseLogging)
                Log($"WebRTC.{methodName} not present in this package version (OK).");
        }
        catch (Exception e)
        {
            LogWarning($"TryInvokeWebRTCStatic({methodName}) failed: {e.Message}");
        }
    }

    // =========================
    // Parsing / serialization
    // =========================

    private static string SerializeSdpJson(RTCSessionDescription desc)
    {
        var s = new SdpJson
        {
            type = desc.type == RTCSdpType.Offer ? "offer" : "answer",
            sdp = desc.sdp
        };
        return JsonUtility.ToJson(s);
    }

    private static bool TryParseSdp(string text, out RTCSessionDescription desc)
    {
        text = (text ?? "").Trim();

        if (text.StartsWith("{") && text.EndsWith("}"))
        {
            try
            {
                var s = JsonUtility.FromJson<SdpJson>(text);
                if (string.IsNullOrWhiteSpace(s.type) || string.IsNullOrWhiteSpace(s.sdp))
                {
                    desc = default;
                    return false;
                }

                desc = new RTCSessionDescription
                {
                    type = ParseSdpType(s.type),
                    sdp = s.sdp
                };
                return true;
            }
            catch { /* fallthrough */ }
        }

        // Raw SDP heuristic
        if (text.Contains("v=0") && text.Contains("a=group:BUNDLE"))
        {
            bool isOffer = text.Contains("a=setup:actpass");
            desc = new RTCSessionDescription
            {
                type = isOffer ? RTCSdpType.Offer : RTCSdpType.Answer,
                sdp = text
            };
            return true;
        }

        desc = default;
        return false;
    }

    private static RTCSdpType ParseSdpType(string type)
    {
        type = (type ?? "").Trim().ToLowerInvariant();
        return type == "offer" ? RTCSdpType.Offer : RTCSdpType.Answer;
    }

    private static string SerializeIceJson(RTCIceCandidate cand)
    {
        var ice = new IceJson
        {
            candidate = cand.Candidate,
            sdpMid = cand.SdpMid,
            sdpMLineIndex = cand.SdpMLineIndex ?? 0
        };
        return JsonUtility.ToJson(ice);
    }

    private static bool TryParseIce(string line, out IceJson ice)
    {
        line = (line ?? "").Trim();

        if (line.StartsWith("{") && line.EndsWith("}"))
        {
            try
            {
                ice = JsonUtility.FromJson<IceJson>(line);
                return !string.IsNullOrWhiteSpace(ice.candidate);
            }
            catch
            {
                ice = default;
                return false;
            }
        }

        var parts = line.Split('|');
        if (parts.Length >= 3 && int.TryParse(parts[2], out var idx))
        {
            ice = new IceJson
            {
                candidate = parts[0],
                sdpMid = parts[1],
                sdpMLineIndex = idx
            };
            return !string.IsNullOrWhiteSpace(ice.candidate);
        }

        ice = default;
        return false;
    }

    private static IEnumerable<string> SplitNonEmptyLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        foreach (var l in lines)
        {
            var t = l.Trim();
            if (!string.IsNullOrWhiteSpace(t))
                yield return t;
        }
    }

    private static string Truncate(string s, int max)
        => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "...";

    // =========================
    // UI helpers
    // =========================

    private void ClearUiOutputs()
    {
        SetInputFieldText(localSdpOutput, "");
        SetInputFieldText(localIceOutput, "");
    }

    private static void SetInputFieldText(TMP_InputField field, string text)
    {
        if (field == null) return;
        field.text = text ?? "";
    }

    private static void AppendInputFieldLine(TMP_InputField field, string line)
    {
        if (field == null) return;
        field.text = string.IsNullOrEmpty(field.text) ? line : (field.text + "\n" + line);
    }

    // =========================
    // Logging
    // =========================

    private void Log(string msg) => Debug.Log($"[SimpleWebRTCPair][{CurrentRole}] {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"[SimpleWebRTCPair][{CurrentRole}] {msg}");
    private void LogError(string msg) => Debug.LogError($"[SimpleWebRTCPair][{CurrentRole}] {msg}");
}

/// <summary>
/// UnityMainThreadDispatcher - minimal main-thread action queue.
/// </summary>
public sealed class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _queue = new Queue<Action>();
    private static readonly object _lock = new object();

    public static void EnsureExists()
    {
        if (_instance != null) return;

        var go = GameObject.Find("UnityMainThreadDispatcher");
        if (go == null) go = new GameObject("UnityMainThreadDispatcher");

        DontDestroyOnLoad(go);

        _instance = go.GetComponent<UnityMainThreadDispatcher>();
        if (_instance == null) _instance = go.AddComponent<UnityMainThreadDispatcher>();
    }

    public static void Enqueue(Action action)
    {
        if (action == null) return;
        EnsureExists();
        lock (_lock) _queue.Enqueue(action);
    }

    private void Update()
    {
        while (true)
        {
            Action a = null;
            lock (_lock)
            {
                if (_queue.Count == 0) break;
                a = _queue.Dequeue();
            }

            try { a?.Invoke(); }
            catch (Exception e) { Debug.LogWarning($"[UnityMainThreadDispatcher] Action exception: {e}"); }
        }
    }
}
