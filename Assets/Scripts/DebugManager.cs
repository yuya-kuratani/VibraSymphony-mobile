using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    private const int MaxLogLines = 10; // 表示するログの最大行数
    private string logText = "";
    private GUIStyle guiStyle = new GUIStyle();
    private bool showLogInGame = true;

    private float lastLogTime = 0f;

    private void Start()
    {
        // ログのテキストをスタイルに設定
        guiStyle.fontSize = 20;
        guiStyle.normal.textColor = Color.white;

        // エディタで実行していない場合のみ、ゲーム画面内のログを表示
        showLogInGame = !Application.isEditor;
    }

    private void OnGUI()
    {

        if (showLogInGame)
        {
            GUI.Label(new Rect(10, 10, Screen.width, Screen.height), logText, guiStyle);
        }
    }

    private void OnEnable()
    {
        // デバッグログを表示するためのイベントハンドラを登録
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        // イベントハンドラを解除
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        // ゲーム画面内のログ表示が有効な場合のみ、3秒ごとにログのテキストをクリア
        if (showLogInGame && Time.time - lastLogTime > 3f)
        {
            logText = "";
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Debug.Log()のテキストをlogTextに追加
        logText += logString + "\n";

        // 表示するログの行数がMaxLogLinesを超えたら、古いログを削除
        string[] logLines = logText.Split('\n');
        if (logLines.Length > MaxLogLines)
        {
            logText = string.Join("\n", logLines, logLines.Length - MaxLogLines, MaxLogLines);
        }

        // 最後にログを表示した時刻を更新
        lastLogTime = Time.time;
    }
}
