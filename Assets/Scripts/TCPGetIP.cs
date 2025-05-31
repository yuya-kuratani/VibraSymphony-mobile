using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.IO;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// TCP 通信を行うサーバ側のコンポーネント
/// </summary>
public class TCPGetIP : MonoBehaviour
{
    //================================================================================
    // 変数
    //================================================================================
    // この IP アドレスとポート番号はクライアント側と統一すること
    public string m_ipAddress = "127.0.0.1";
    public int m_port = 8000;
    private int sendedInt;
    private TcpListener m_tcpListener;
    private TcpClient m_tcpClient;
    private NetworkStream m_networkStream;
    private OSCListener osclistner;
    private bool hasRecieved;
    private bool adjustMode;
    private bool NormalCetecStart;
    private string VRIP;
    private int VRPort;
    private bool adjustStart, startDelay, adjustEnd, musicStart;
    private float delaySec, startMusicDelaySec;
    private int adjustSec;
    private float endAdjustSec;

    bool VibeCetecStart, musicStartCetec, musicEndtCetec;
    DateTime expectVibingTime, expectStartTime;

    private string m_message = string.Empty; // クライアントから受信した文字列

    //================================================================================
    // 関数
    //================================================================================
    /// <summary>
    /// 初期化する時に呼び出されます
    /// </summary>
    private void Awake()
    {
        // クライアントから文字列を受信する処理を非同期で実行します
        // 非同期で実行しないと接続が終了するまで受信した文字列を UI に表示できません
        Task.Run(() => OnProcess());
    }

    private void Update()
    {
        if (hasRecieved)
        {
            hasRecieved = false;
            SendMusic(VRIP, VRPort);

        }
        if (adjustStart)
        {
            adjustStart = false;
            FindObjectOfType<MusicManager>().StartMusicForAdjust(adjustSec);

        }
        if (startDelay)
        {
            startDelay = false;
            FindObjectOfType<MusicManager>().StartMusicDelay(delaySec);
        }
        if (adjustEnd)
        {
            adjustEnd = false;
            FindObjectOfType<MusicManager>().EndAdjust(endAdjustSec);

        }
        if (musicStart)
        {
            musicStart = false;
            FindObjectOfType<MusicManager>().StartMusic(endAdjustSec);

        }

        //cetec--------------

        if (NormalCetecStart)
        {
            GameObject.Find("StartManager").GetComponent<StartManager>().StartButtonPressed();
            FindObjectOfType<MusicManager>().SetAudioCetec();
            NormalCetecStart = false;
        }
        if (VibeCetecStart)
        {
            print("kita!");
            VibeCetecStart = false;
            WaitUntilDateTime(expectVibingTime, "vibe").Forget();
        }
        if (musicStartCetec)
        {
            musicStartCetec = false;
            WaitUntilDateTime(expectStartTime, "start").Forget();
        }
        if (musicEndtCetec)
        {
            musicStartCetec = false;
            OnDestroy();
            FindObjectOfType<EXITManager>().EXITGame();

        }
    }
    /// <summary>
    /// クライアント側から通信を監視し続けます
    /// </summary>
    /// <summary>
    /// クライアント側から通信を監視し続けます
    /// </summary>
    private async void OnProcess()
    {
        m_tcpListener = new TcpListener(IPAddress.Any, m_port);
        m_tcpListener.Start();

        UnityEngine.Debug.Log("待機中");

        while (true) // ループして常に新しい接続を待機する
        {
            try
            {
                // クライアントの接続を待機
                m_tcpClient = await m_tcpListener.AcceptTcpClientAsync();
                UnityEngine.Debug.Log("接続完了");

                m_networkStream = m_tcpClient.GetStream();

                StringBuilder messageBuffer = new StringBuilder(); // メッセージを蓄積するバッファ

                // 接続が維持されている間、データを受信し続ける
                while (m_tcpClient.Connected)
                {
                    // 受信データのバッファ
                    var buffer = new byte[256];
                    int bytesRead = await m_networkStream.ReadAsync(buffer, 0, buffer.Length); // 非同期で読み込む

                    if (bytesRead == 0)
                    {
                        UnityEngine.Debug.Log("クライアント切断");

                        // 通信に使用したインスタンスを破棄して再接続待機
                        OnDestroy();
                        break; // 切断されたらループを抜ける
                    }

                    // 受信したデータを文字列に変換
                    var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(receivedData);

                    // 改行でメッセージを区切り処理
                    while (messageBuffer.ToString().Contains("\n"))
                    {
                        int newlineIndex = messageBuffer.ToString().IndexOf("\n");
                        var fullMessage = messageBuffer.ToString().Substring(0, newlineIndex);

                        // 残りのメッセージをバッファに残す
                        messageBuffer.Remove(0, newlineIndex + 1);

                        // 受信したメッセージを処理
                        ProcessMessage(fullMessage);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"接続エラー: {e.Message}");
            }
        }
    }
    private void ProcessMessage(string message)
    {
        print("aa");
        var splitSlash = message.Split("/");
        UnityEngine.Debug.Log("Received message: " + message);

        switch (splitSlash[0])
        {
            case "Normal":
                hasRecieved = true;
                var newSplit = splitSlash[1].Split("B");
                VRIP = newSplit[0];
                VRPort = int.Parse(newSplit[1]);
                break;

            case "Adjust":
                hasRecieved = true;
                adjustMode = true;
                UnityEngine.Debug.Log("Have Sent the Adjustment info");
                var AdjustSplit = splitSlash[1].Split("B");
                VRIP = AdjustSplit[0];
                VRPort = int.Parse(AdjustSplit[1]);
                break;

            case "Time":
                adjustStart = true;
                UnityEngine.Debug.Log("Have Sent the time Info: " + splitSlash[1]);
                adjustSec = int.Parse(splitSlash[1]);
                break;

            case "DelayTime":
                startDelay = true;
                delaySec = float.Parse(splitSlash[1]);
                break;

            case "EndAdjust":
                adjustEnd = true;
                endAdjustSec = float.Parse(splitSlash[1]);
                break;

            case "StartMusic":
                musicStart = true;
                startMusicDelaySec = float.Parse(splitSlash[1]);
                break;

            case "NormalCetec":
                NormalCetecStart = true;
                break;

            case "VibeCetec":
                VibeCetecStart = true;
                expectVibingTime = DateTime.Parse(splitSlash[1]);
                break;

            case "StartMusicCetec":
                musicStartCetec = true;
                expectStartTime = DateTime.Parse(splitSlash[1]);
                break;
            case "EndMusicCetec":
                musicEndtCetec = true;
                break;

        }
    }

    /// <summary>
    /// 破棄する時に呼び出されます
    /// </summary>
    private void OnDestroy()
    {
        // 通信に使用したインスタンスを破棄します
        m_networkStream?.Dispose();
        m_tcpClient?.Dispose();
        m_tcpListener?.Stop();
    }

    private void SendMusic(string VRIP, int VRPort)
    {
        this.GetComponent<TCPSender>().ConnectToVR(VRIP, VRPort, adjustMode);
    }


    //cetec ---------

    private async UniTask WaitUntilDateTime(DateTime targetTime, string kind)
    {
        // 現在の時刻を取得
        DateTime currentTime = DateTime.Now;

        // 指定時刻が現在時刻を過ぎているかどうかを確認
        if (currentTime >= targetTime)
        {
            return;
        }

        // 残りの待機時間を計算
        TimeSpan waitTime = targetTime - currentTime;

        // 指定された時刻まで待機
        await UniTask.Delay(waitTime);
        switch (kind)
        {
            case "vibe":
                osclistner.VibeCetec();
                break;
            case "start":
                FindObjectOfType<MusicManager>().StartMusic(1);
                break;
        }
    }

    public void Start()
    {
        osclistner = FindObjectOfType<OSCListener>();
    }
}